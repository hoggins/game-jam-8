using Balance;
using System.Collections.Generic;
using Destruction;
using Movement;
using UnityEngine;
using Unity.Profiling;

namespace Map
{
  public class MapEnvironmentSpawner : System.IDisposable
  {
    private static readonly ProfilerMarker TryArrangeSpecialFenceMarker =
      new("MapEnvironmentSpawner.TryArrangeSpecialFence");
    private static readonly ProfilerMarker TrySpawnSpecialMarker =
      new("MapEnvironmentSpawner.TrySpawnSpecial");
    private static readonly ProfilerMarker TryMoveSpecialMarker =
      new("MapEnvironmentSpawner.TryMoveSpecial");
    private static readonly ProfilerMarker TrySpawnSpecialAtMarker =
      new("MapEnvironmentSpawner.TrySpawnSpecialAt");
    private static readonly ProfilerMarker TryMoveSpecialAtMarker =
      new("MapEnvironmentSpawner.TryMoveSpecialAt");
    private static readonly ProfilerMarker TryPickPlacementMarker =
      new("MapEnvironmentSpawner.TryPickNonOverlappingPlacement");
    private static readonly ProfilerMarker ClearOverlappingMarker =
      new("MapEnvironmentSpawner.ClearOverlapping");
    private static readonly ProfilerMarker SpawnSpecialInstanceMarker =
      new("MapEnvironmentSpawner.SpawnSpecialInstance");
    private static readonly ProfilerMarker InstantiateSpecialMarker =
      new("MapEnvironmentSpawner.InstantiateSpecialPrefab");

    private readonly struct PlannedSpecialPlacement
    {
      public readonly SpecialHouses Type;
      public readonly Vector3 Position;
      public readonly Quaternion Rotation;
      public readonly Vector2 HalfExtents;

      public PlannedSpecialPlacement(SpecialHouses type, Vector3 position, Quaternion rotation, Vector2 halfExtents)
      {
        Type = type;
        Position = position;
        Rotation = rotation;
        HalfExtents = halfExtents;
      }
    }

    private const string ContainerName = "SpawnedEnvironment";
    // Extra margin added on top of a special's real bounding box when clearing houses out of its
    // way, so the cleared area isn't drawn exactly flush with the mesh.
    private const float SpecialClearMargin = 5f;
    // How many random placements to try before giving up on a special that keeps landing on top of
    // another live special.
    private const int MaxPlacementAttempts = 20;
    // Gap between neighbouring objects in the perpendicular fence. Their real footprints determine
    // the overall width, so the fence stays wide without relying on a prefab-specific spacing.
    private const float SpecialFenceGap = 2f;
    // The camera only shows a small part of the map at once. Keeping every child renderer of every
    // spawned map element registered with the renderer culler makes the CPU pay for tens of
    // thousands of renderers every frame, even when they are hundreds of metres away. Buildings
    // and ground cells remain active (their colliders and destruction scripts still work); only
    // their visual renderers are distance culled as a group. Unity's renderer still performs its
    // normal camera-frustum culling, while the distance bands are exposed in
    // EnvironmentVisibilitySettings so they can be tuned without changing code.

    private readonly Dictionary<int, RuntimeEnvironmentObject> _objects = new();
    // Index of ids in _objects that are specials (Timer, Arrow, ...) rather than grid houses, so a
    // new special can be checked against other live specials without disturbing houses.
    private readonly HashSet<int> _specialIds = new();
    // The one live instance of each special type spawned through TrySpawnSpecial, so a runtime
    // respawn (e.g. the Upgrade house respawning alongside the Timer) replaces its predecessor
    // instead of leaving it standing forever as an ever-growing pile of stale duplicates.
    private readonly Dictionary<SpecialHouses, (int id, GameObject instance)> _currentSpecial = new();
    // Real footprint of each special's prefab (from its renderer bounds, not the grid size it's
    // configured with), computed once per type and reused on every spawn/respawn.
    private readonly Dictionary<SpecialHouses, Vector2> _specialHalfExtentsCache = new();
    private readonly List<Renderer> _rendererScratch = new();
    private readonly List<Renderer> _allRenderers = new();
    private readonly HashSet<Vector2Int> _occupied = new();
    private readonly MovementUpdater _movementUpdater;
    private readonly EnvironmentVisibilitySettings _visibilitySettings;
    private readonly SpecialSpawnSettings _specialSpawnSettings;
    private readonly BattleBalanceConfig _battleBalance;

    private Transform _container;
    private HouseSet _houseSet;
    private int _cellSize;
    private int _nextId;
    private CullingGroup _cullingGroup;
    private int[] _rendererStartByObject = System.Array.Empty<int>();
    private int[] _rendererCountByObject = System.Array.Empty<int>();
    private BoundingSphere[] _boundingSpheres = System.Array.Empty<BoundingSphere>();
    private bool[] _distanceVisibleByObject = System.Array.Empty<bool>();
    private bool _hasVisibilityHysteresis;

    public MapEnvironmentSpawner(
      MovementUpdater movementUpdater, EnvironmentVisibilitySettings visibilitySettings, SpecialSpawnSettings specialSpawnSettings,
      BattleBalanceConfig battleBalance)
    {
      _movementUpdater = movementUpdater;
      _visibilitySettings = visibilitySettings;
      _specialSpawnSettings = specialSpawnSettings;
      _battleBalance = battleBalance;
    }

    public IReadOnlyCollection<RuntimeEnvironmentObject> SpawnedObjects => _objects.Values;

    /// <summary>
    /// The currently live instance of a special type spawned through <see cref="TrySpawnSpecial"/>,
    /// if one still stands — lets a caller relocate it with <see cref="TryMoveSpecial"/> on a runtime
    /// respawn instead of spawning (and thereby duplicating) a fresh one.
    /// </summary>
    public bool TryGetCurrentSpecial(SpecialHouses type, out GameObject instance)
    {
      if (_currentSpecial.TryGetValue(type, out var current))
      {
        // A destructible can leave its root alive as a decay husk for a short time. Release removes
        // the runtime record immediately, so require that record as well as a live GameObject before
        // handing a caller something it may try to move.
        if (current.instance != null && _objects.ContainsKey(current.id))
        {
          instance = current.instance;
          return true;
        }

        _currentSpecial.Remove(type);
      }

      instance = null;
      return false;
    }
    public HouseSet CurrentHouseSet => _houseSet;

    /// Refreshes movement blockers after a batch of special placements has completed.
    /// Individual spawn/move operations intentionally do not call this because a timer respawn can
    /// place several specials in one transaction.
    internal void RefreshNoGoZones() =>
      _movementUpdater.RefreshNoGoZones();

    public void Spawn(MapData mapData, HouseSet houseSet, RoadSet roadSet, SidewalkSet sidewalkSet, int seed, Transform parent)
    {
      if (mapData == null)
        return;

      // This service outlives the scene, so the bookkeeping from the previous level is still here.
      // Everything it describes died with that scene; leaving it would keep those cells reserved
      // forever and silently skip every object that survived the last battle.
      _objects.Clear();
      _specialIds.Clear();
      _currentSpecial.Clear();
      _occupied.Clear();
      DisposeRenderCulling();

      _container = new GameObject(ContainerName).transform;
      _container.SetParent(parent, false);

      _houseSet = houseSet;
      var cellSize = mapData.CellSize;
      _cellSize = cellSize;
      var player = GameObject.FindGameObjectWithTag("Player");
      var originPosition = player != null ? player.transform.position : parent.position;
      var originCell = new Vector2(originPosition.x / cellSize, originPosition.z / cellSize);

      var housePlacements = MapFiller.Fill(mapData, houseSet, originCell, seed);
      var roadPlacements = roadSet != null
        ? RoadFiller.Fill(mapData, roadSet, seed)
        : new List<RoadPlacement>();
      var sidewalkPlacements = sidewalkSet != null
        ? SidewalkFiller.Fill(mapData, sidewalkSet, seed)
        : new List<SidewalkPlacement>();

      // Reserve stable index ranges for each placement type. A skipped house can leave a hole
      // in its range, but it must not shift the ground indices that were already allocated.
      var roadCullingStart = housePlacements.Count;
      var sidewalkCullingStart = roadCullingStart + roadPlacements.Count;
      InitializeRenderCulling(sidewalkCullingStart + sidewalkPlacements.Count);

      for (var placementIndex = 0; placementIndex < housePlacements.Count; placementIndex++)
      {
        var placement = housePlacements[placementIndex];
        var size = MapFiller.RotatedSize(placement.House.size, placement.RotationDegrees);
        if (!TryReserve(placement.Cell, size))
          continue;

        var position = new Vector3(
          placement.Cell.x * cellSize + size.x * cellSize * 0.5f,
          0f,
          placement.Cell.y * cellSize + size.y * cellSize * 0.5f);
        var rotation = Quaternion.Euler(0f, placement.RotationDegrees, 0f);

        var instance = Object.Instantiate(placement.House.prefab, position, rotation, _container);
        instance.name = placement.House.name;
        RegisterRenderers(placementIndex, instance);

        var destructible = instance.GetComponentInChildren<DestructibleObject>();
        var id = _nextId++;
        var worldHalfExtents = new Vector2(size.x * cellSize * 0.5f, size.y * cellSize * 0.5f);
        var runtimeObject = new RuntimeEnvironmentObject(
          id, placement.Cell, size, destructible, position, worldHalfExtents, rotation);
        _objects.Add(id, runtimeObject);

        if (destructible != null)
          destructible.Destroyed += _ => Release(id);
      }

      for (var placementIndex = 0; placementIndex < roadPlacements.Count; placementIndex++)
      {
        var placement = roadPlacements[placementIndex];
        var cellPosition = new Vector2(
          placement.Cell.x + 0.5f + placement.CellOffset.x,
          placement.Cell.y + 0.5f + placement.CellOffset.y);
        var position = new Vector3(
          cellPosition.x * cellSize,
          0f,
          cellPosition.y * cellSize);
        var rotation = Quaternion.Euler(0f, placement.RotationDegrees, 0f);

        var instance = Object.Instantiate(placement.Piece.prefab, position, rotation, _container);
        instance.name = placement.Piece.name;
        RegisterRenderers(roadCullingStart + placementIndex, instance);
      }

      for (var placementIndex = 0; placementIndex < sidewalkPlacements.Count; placementIndex++)
      {
        var placement = sidewalkPlacements[placementIndex];
        var position = new Vector3(
          placement.Cell.x * cellSize + cellSize * 0.5f,
          0f,
          placement.Cell.y * cellSize + cellSize * 0.5f);

        var rotation = Quaternion.Euler(0f, placement.RotationDegrees, 0f);
        var instance = Object.Instantiate(placement.Piece.prefab, position, rotation, _container);
        instance.name = placement.Piece.name;
        RegisterRenderers(sidewalkCullingStart + placementIndex, instance);
      }

      if (_cullingGroup != null)
      {
        _cullingGroup.SetBoundingSpheres(_boundingSpheres);
        _cullingGroup.SetBoundingSphereCount(_boundingSpheres.Length);
      }

      // The final goal is authored directly in the scene, outside this service's runtime object
      // bookkeeping. Clear only the generated houses around it so map generation cannot bury the
      // objective or make it impossible to reach.
      ClearSceneGoalFootprint();
      SpawnInitialSpecials(parent);
      RefreshNoGoZones();
    }

    /// <summary>
    /// Places every configured special (e.g. the Timer) after the grid fill, the same freely-placed
    /// way a respawn works (<see cref="TrySpawnSpecial"/>) rather than as part of <see cref="MapFiller.Fill"/>:
    /// specials are rotated to face the player and clear their own footprint, which the blocky grid
    /// placement isn't built for. The Timer goes down first (near the player, per its own configured
    /// distance); every other special then follows the same between/near-player rule a respawn uses
    /// (see <see cref="GetOtherSpecialPlacement"/>), so it never starts right on top of the player.
    /// </summary>
    private void SpawnInitialSpecials(Transform parent)
    {
      if (_houseSet == null)
        return;

      var player = GameObject.FindGameObjectWithTag("Player");
      var fallbackAnchor = player != null ? player.transform.position : parent.position;
      var lookTarget = player != null ? player.transform : null;

      GameObject timerInstance = null;
      foreach (var special in _houseSet.Specials)
      {
        if (special.type != SpecialHouses.Timer || !special.enabled || special.prefab == null)
          continue;

        if (_specialSpawnSettings == null || !_specialSpawnSettings.TryGetInitialDistance(special.type, out var timerMinDistance, out var timerMaxDistance))
        {
          Debug.LogWarning($"MapEnvironmentSpawner.SpawnInitialSpecials: no initial spawn distance configured for '{special.type}' in SpecialSpawnSettings.");
          continue;
        }

        TrySpawnSpecial(special.type, fallbackAnchor, lookTarget, timerMinDistance, timerMaxDistance, out timerInstance);
      }

      foreach (var special in _houseSet.Specials)
      {
        if (special.type == SpecialHouses.Timer || !special.enabled || special.prefab == null)
          continue;

        Vector3 anchor;
        float minDistance;
        float maxDistance;
        if (timerInstance != null && player != null)
        {
          GetOtherSpecialPlacement(timerInstance.transform.position, player.transform.position, out anchor, out minDistance, out maxDistance);
        }
        else if (_specialSpawnSettings != null && _specialSpawnSettings.TryGetInitialDistance(special.type, out minDistance, out maxDistance))
        {
          anchor = fallbackAnchor;
        }
        else
        {
          Debug.LogWarning($"MapEnvironmentSpawner.SpawnInitialSpecials: no initial spawn distance configured for '{special.type}' in SpecialSpawnSettings.");
          continue;
        }

        TrySpawnSpecial(special.type, anchor, lookTarget, minDistance, maxDistance, out _);
      }
    }

    /// <summary>
    /// Where a non-Timer special should aim, given the timer's current position and the player:
    /// somewhere between the two when they're far apart, otherwise a band close to (but not on top
    /// of) the player. Shared between the initial placement and every runtime respawn.
    /// </summary>
    public void GetOtherSpecialPlacement(
      Vector3 timerPosition, Vector3 playerPosition, out Vector3 anchor, out float minDistance, out float maxDistance)
    {
      var distanceToPlayer = Vector3.Distance(timerPosition, playerPosition);
      if (distanceToPlayer > _battleBalance.SpecialBetweenMaxDistance)
      {
        // Far apart: land roughly on the segment between them, with a little spread so it's not
        // pinned to the exact midpoint every time.
        anchor = (timerPosition + playerPosition) * 0.5f;
        minDistance = 0f;
        maxDistance = distanceToPlayer * 0.2f;
      }
      else
      {
        anchor = playerPosition;
        minDistance = _battleBalance.SpecialBetweenMinDistance;
        maxDistance = _battleBalance.SpecialBetweenMaxDistance;
      }
    }

    /// <summary>
    /// Places the available non-Timer specials on a wide line perpendicular to the timer/player
    /// direction when the configured distance threshold is reached. The health bar is always the
    /// centre point; remaining specials are spread to its left and right. Existing objects are moved
    /// and missing respawnable objects are created, so the health bar keeps its current state instead
    /// of being reset by a prefab replacement.
    /// </summary>
    public bool TryArrangeSpecialFence(Vector3 timerPosition, Vector3 playerPosition, Transform lookTarget)
    {
      TryArrangeSpecialFenceMarker.Begin();
      try
      {
        if (_battleBalance == null)
          return false;

      var timerToPlayer = playerPosition - timerPosition;
      timerToPlayer.y = 0f;
      var distance = timerToPlayer.magnitude;
      if (distance < _battleBalance.SpecialFenceStartDistance || distance < 0.0001f)
        return false;

      var fenceTypes = CollectFenceSpecialTypes();
      if (!fenceTypes.Contains(SpecialHouses.Health))
        return false;

      var usableDistance = distance
        - _battleBalance.SpecialFenceTimerOffset
        - _battleBalance.SpecialFencePlayerOffset;
      if (usableDistance <= 0f)
        return false;

      var direction = timerToPlayer / distance;
      var fenceStart = timerPosition + direction * _battleBalance.SpecialFenceTimerOffset;
      var fenceEnd = playerPosition - direction * _battleBalance.SpecialFencePlayerOffset;
      var fenceCenter = Vector3.Lerp(fenceStart, fenceEnd, 0.5f);
      fenceCenter.y = fenceStart.y;
      var fenceSide = new Vector3(-direction.z, 0f, direction.x);
      var nonHealthTypes = new List<SpecialHouses>(fenceTypes.Count - 1);
      foreach (var type in fenceTypes)
        if (type != SpecialHouses.Health)
          nonHealthTypes.Add(type);

      var planned = new List<PlannedSpecialPlacement>(fenceTypes.Count);
      if (!AddFencePlacement(SpecialHouses.Health, fenceCenter, 0f, fenceSide, lookTarget, planned))
        return false;

      var healthHalfWidth = ProjectedHalfExtent(planned[0].HalfExtents, planned[0].Rotation, fenceSide);
      var leftCount = (nonHealthTypes.Count + 1) / 2;
      var leftBoundary = -healthHalfWidth - SpecialFenceGap;
      for (var i = 0; i < leftCount; i++)
      {
        if (!AddFenceSidePlacement(
          nonHealthTypes[i], -1f, ref leftBoundary, fenceCenter, fenceSide, lookTarget, planned))
          return false;
      }

      var rightBoundary = healthHalfWidth + SpecialFenceGap;
      for (var i = leftCount; i < nonHealthTypes.Count; i++)
      {
        if (!AddFenceSidePlacement(
          nonHealthTypes[i], 1f, ref rightBoundary, fenceCenter, fenceSide, lookTarget, planned))
          return false;
      }

      // Ignore all participants' old positions while validating the new layout, then check the
      // planned positions against each other. This makes the operation atomic from the placement
      // point of view: a special never fails just because another fence member has not moved yet.
      var ignoredSpecialIds = CurrentSpecialIds(fenceTypes);
      for (var i = 0; i < planned.Count; i++)
      {
        var placement = planned[i];
        if (HasSpecialOverlap(placement.Position, placement.Rotation, placement.HalfExtents, null, ignoredSpecialIds))
          return false;

        for (var j = i + 1; j < planned.Count; j++)
        {
          var other = planned[j];
          if (RectanglesOverlap(
            placement.Position, placement.Rotation.eulerAngles.y, placement.HalfExtents,
            other.Position, other.Rotation.eulerAngles.y, other.HalfExtents))
            return false;
        }
      }

      foreach (var placement in planned)
      {
        if (TryGetCurrentSpecial(placement.Type, out var existing))
        {
          if (!TryMoveSpecialAt(
            placement.Type, existing, placement.Position, placement.Rotation, placement.HalfExtents, ignoredSpecialIds))
            return false;
        }
        else if (!TrySpawnSpecialAt(
          placement.Type, placement.Position, placement.Rotation, placement.HalfExtents, ignoredSpecialIds, out _))
        {
          return false;
        }

        // A type which was missing at validation time has a new id now. Add it so the next exact
        // placement in this transaction still ignores the already-arranged fence members.
        if (_currentSpecial.TryGetValue(placement.Type, out var current))
          ignoredSpecialIds.Add(current.id);
      }

        return true;
      }
      finally
      {
        TryArrangeSpecialFenceMarker.End();
      }
    }

    private List<SpecialHouses> CollectFenceSpecialTypes()
    {
      var types = new List<SpecialHouses>();
      if (_houseSet == null)
        return types;

      foreach (var special in _houseSet.Specials)
      {
        if (special.type == SpecialHouses.Timer || !special.enabled || special.prefab == null || types.Contains(special.type))
          continue;

        // Health and Arrow are intentionally not recreated after they have been destroyed. If they
        // are still live, they participate in the fence; a missing one is simply left out.
        if ((special.type == SpecialHouses.Health || special.type == SpecialHouses.Arrow)
            && !TryGetCurrentSpecial(special.type, out _))
          continue;

        types.Add(special.type);
      }

      return types;
    }

    private bool AddFencePlacement(
      SpecialHouses type, Vector3 fenceCenter, float lateralOffset, Vector3 fenceSide, Transform lookTarget,
      List<PlannedSpecialPlacement> planned)
    {
      if (!TryResolveSpecial(type, "TryArrangeSpecialFence", out var special))
        return false;

      var position = fenceCenter + fenceSide * lateralOffset;
      var rotation = FaceTarget(position, lookTarget);
      planned.Add(new PlannedSpecialPlacement(type, position, rotation, SpecialHalfExtents(special)));
      return true;
    }

    private bool AddFenceSidePlacement(
      SpecialHouses type, float side, ref float boundary, Vector3 fenceCenter, Vector3 fenceSide,
      Transform lookTarget, List<PlannedSpecialPlacement> planned)
    {
      if (!TryResolveSpecial(type, "TryArrangeSpecialFence", out var special))
        return false;

      var halfExtents = SpecialHalfExtents(special);
      var halfWidth = Mathf.Max(halfExtents.x, halfExtents.y);
      var lateralOffset = boundary + side * halfWidth;

      // Facing changes slightly for objects moved off the centre line. Recalculate their projected
      // width a few times so neighbouring footprints stay separated even when their yaw changes.
      for (var attempt = 0; attempt < 3; attempt++)
      {
        var position = fenceCenter + fenceSide * lateralOffset;
        var rotation = FaceTarget(position, lookTarget);
        halfWidth = ProjectedHalfExtent(halfExtents, rotation, fenceSide);
        lateralOffset = boundary + side * halfWidth;
      }

      var finalPosition = fenceCenter + fenceSide * lateralOffset;
      var finalRotation = FaceTarget(finalPosition, lookTarget);
      planned.Add(new PlannedSpecialPlacement(type, finalPosition, finalRotation, halfExtents));
      halfWidth = ProjectedHalfExtent(halfExtents, finalRotation, fenceSide);
      boundary = lateralOffset + side * (halfWidth + SpecialFenceGap);
      return true;
    }

    private static float ProjectedHalfExtent(Vector2 halfExtents, Quaternion rotation, Vector3 axis)
    {
      var localX = rotation * Vector3.right;
      var localZ = rotation * Vector3.forward;
      return Mathf.Abs(Vector3.Dot(localX, axis)) * halfExtents.x
        + Mathf.Abs(Vector3.Dot(localZ, axis)) * halfExtents.y;
    }

    private HashSet<int> CurrentSpecialIds(IEnumerable<SpecialHouses> types)
    {
      var ids = new HashSet<int>();
      foreach (var type in types)
        if (_currentSpecial.TryGetValue(type, out var current))
          ids.Add(current.id);

      return ids;
    }

    /// <summary>
    /// Spawns a configured special object (e.g. the battle timer) at runtime, at a random point in
    /// the annulus [<paramref name="minDistance"/>, <paramref name="maxDistance"/>] around
    /// <paramref name="anchor"/>, facing <paramref name="lookTarget"/>. Unlike <see cref="Spawn"/>,
    /// this does not use the map grid: any currently standing house whose footprint overlaps the
    /// special's footprint is broken to make room for it.
    /// </summary>
    public bool TrySpawnSpecial(
      SpecialHouses type, Vector3 anchor, Transform lookTarget, float minDistance, float maxDistance, out GameObject instance)
    {
      TrySpawnSpecialMarker.Begin();
      try
      {
        instance = null;

        if (!TryResolveSpecial(type, "TrySpawnSpecial", out var special))
          return false;

        var worldHalfExtents = SpecialHalfExtents(special);
        var previousInstance = TryGetCurrentSpecial(type, out var currentInstance) ? currentInstance : null;
        var excludedTransform = previousInstance != null ? previousInstance.transform : null;

      // Keep the old instance in place until a valid replacement has been found. It is excluded from
      // the overlap test, so the replacement may occupy its old footprint, but a failed placement
      // cannot leave the world without this special at all.
        if (!TryPickNonOverlappingPlacement(anchor, lookTarget, minDistance, maxDistance, worldHalfExtents, excludedTransform, out var position, out var rotation))
        {
          Debug.LogWarning($"MapEnvironmentSpawner.TrySpawnSpecial: '{type}' found no placement clear of other specials after {MaxPlacementAttempts} attempts; spawn skipped.");
          return false;
        }

        ClearOverlapping(position, rotation, worldHalfExtents + ClearMargin, excludedTransform);

        DespawnCurrentSpecial(type);

        instance = SpawnSpecialInstance(special, type, position, rotation, worldHalfExtents);
        return true;
      }
      finally
      {
        TrySpawnSpecialMarker.End();
      }
    }

    private bool TrySpawnSpecialAt(
      SpecialHouses type, Vector3 position, Quaternion rotation, Vector2 worldHalfExtents,
      ISet<int> ignoredSpecialIds, out GameObject instance)
    {
      TrySpawnSpecialAtMarker.Begin();
      try
      {
        instance = null;
        if (!TryResolveSpecial(type, "TryArrangeSpecialFence", out var special)
            || HasSpecialOverlap(position, rotation, worldHalfExtents, null, ignoredSpecialIds))
          return false;

        ClearOverlapping(position, rotation, worldHalfExtents + ClearMargin, null);
        DespawnCurrentSpecial(type);
        instance = SpawnSpecialInstance(special, type, position, rotation, worldHalfExtents);
        return true;
      }
      finally
      {
        TrySpawnSpecialAtMarker.End();
      }
    }

    private GameObject SpawnSpecialInstance(
      SpecialHouseObject special, SpecialHouses type, Vector3 position, Quaternion rotation, Vector2 worldHalfExtents)
    {
      SpawnSpecialInstanceMarker.Begin();
      try
      {
        InstantiateSpecialMarker.Begin();
        GameObject instance = null;
        try
        {
          instance = Object.Instantiate(special.prefab, position, rotation, _container);
        }
        finally
        {
          InstantiateSpecialMarker.End();
        }

        instance.name = special.type.ToString();

        var destructible = instance.GetComponentInChildren<DestructibleObject>();
        var id = _nextId++;
        // Vector2Int.zero: a freely-placed special never reserves grid cells via TryReserve, so it
        // must not claim a grid Size either — Release would otherwise free cells on the grid that
        // happen to fall under its footprint but were never its own.
        var runtimeObject = new RuntimeEnvironmentObject(
          id, CellOf(position), Vector2Int.zero, destructible, position, worldHalfExtents, rotation);
        _objects.Add(id, runtimeObject);
        _specialIds.Add(id);
        _currentSpecial[type] = (id, instance);

        if (destructible != null)
          destructible.Destroyed += _ => Release(id);

        return instance;
      }
      finally
      {
        SpawnSpecialInstanceMarker.End();
      }
    }

    /// <summary>
    /// Removes the previous live instance of a special type, if any, so a fresh runtime respawn
    /// never leaves it standing alongside its replacement. This is a silent swap, not a destruction
    /// action — the old instance is destroyed directly, without going through DestructibleObject at
    /// all, so nothing plays a break effect or fires a second, misleading Destroyed event.
    /// </summary>
    private void DespawnCurrentSpecial(SpecialHouses type)
    {
      if (!_currentSpecial.Remove(type, out var previous) || previous.instance == null)
        return;

      Debug.Log($"Despawning special {type} at {previous.instance.transform.position}");
      Release(previous.id);
      Object.Destroy(previous.instance);
    }

    /// <summary>
    /// Picks a fresh spot for a special that is already standing and moves it there, clearing houses
    /// out of the way exactly as a spawn would. Used for objects that must stay unique across a
    /// battle: relocating the one that exists keeps a single instance in the world (and a single
    /// scene HUD camera behind it) while still making the player go and find it again.
    /// </summary>
    public bool TryMoveSpecial(
      SpecialHouses type, GameObject instance, Vector3 anchor, Transform lookTarget, float minDistance, float maxDistance)
    {
      TryMoveSpecialMarker.Begin();
      try
      {
        if (instance == null)
          return false;

        if (!_currentSpecial.TryGetValue(type, out var current)
            || current.instance != instance
            || !_objects.ContainsKey(current.id))
          return false;

        if (!TryResolveSpecial(type, "TryMoveSpecial", out var special))
          return false;

        var worldHalfExtents = SpecialHalfExtents(special);

        if (!TryPickNonOverlappingPlacement(anchor, lookTarget, minDistance, maxDistance, worldHalfExtents, instance.transform, out var position, out var rotation))
        {
          Debug.LogWarning($"MapEnvironmentSpawner.TryMoveSpecial: '{type}' found no placement clear of other specials after {MaxPlacementAttempts} attempts; move skipped.");
          return false;
        }

        return TryMoveSpecialAt(type, instance, position, rotation, worldHalfExtents, null);
      }
      finally
      {
        TryMoveSpecialMarker.End();
      }
    }

    private bool TryMoveSpecialAt(
      SpecialHouses type, GameObject instance, Vector3 position, Quaternion rotation, Vector2 worldHalfExtents,
      ISet<int> ignoredSpecialIds)
    {
      TryMoveSpecialAtMarker.Begin();
      try
      {
        if (instance == null
            || !_currentSpecial.TryGetValue(type, out var current)
            || current.instance != instance
            || !_objects.ContainsKey(current.id)
            || HasSpecialOverlap(position, rotation, worldHalfExtents, instance.transform, ignoredSpecialIds))
          return false;

        // Excluded from the sweep, or an object whose new footprint overlaps its old one would break
        // itself on arrival.
        ClearOverlapping(position, rotation, worldHalfExtents + ClearMargin, instance.transform);

        Debug.Log($"Moving special {type} to {position}");
        instance.transform.SetPositionAndRotation(position, rotation);
        Reseat(instance.transform, position, rotation, worldHalfExtents);

        return true;
      }
      finally
      {
        TryMoveSpecialAtMarker.End();
      }
    }

    /// <summary>
    /// Rewrites the moved object's registered footprint. Without this the stale one keeps standing
    /// in for it: later specials would clear houses around the spot it left and land on top of it at
    /// the spot it moved to.
    /// </summary>
    private void Reseat(Transform instance, Vector3 position, Quaternion rotation, Vector2 worldHalfExtents)
    {
      foreach (var pair in _objects)
      {
        var standing = pair.Value;
        if (standing.Destructible == null || !standing.Destructible.transform.IsChildOf(instance))
          continue;

        _objects[pair.Key] = new RuntimeEnvironmentObject(
          standing.Id, CellOf(position), standing.Size, standing.Destructible, position, worldHalfExtents, rotation);
        return;
      }
    }

    private bool TryResolveSpecial(SpecialHouses type, string caller, out SpecialHouseObject special)
    {
      special = null;
      if (_houseSet != null)
        foreach (var candidate in _houseSet.Specials)
          if (candidate.type == type && candidate.enabled && candidate.prefab != null)
          {
            special = candidate;
            return true;
          }

      Debug.LogWarning($"MapEnvironmentSpawner.{caller}: no enabled '{type}' entry configured on the HouseSet.");
      return false;
    }

    /// A random point in the annulus [minDistance, maxDistance] around the anchor, facing lookTarget.
    private static void PickPlacement(
      Vector3 anchor, Transform lookTarget, float minDistance, float maxDistance, out Vector3 position, out Quaternion rotation)
    {
      var angle = Random.Range(0f, Mathf.PI * 2f);
      var radius = Random.Range(minDistance, maxDistance);
      position = anchor + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
      rotation = FaceTarget(position, lookTarget);
    }

    private static Quaternion FaceTarget(Vector3 position, Transform lookTarget)
    {
      var facing = lookTarget != null ? lookTarget.position - position : Vector3.zero;
      facing.y = 0f;
      return facing.sqrMagnitude > 0.0001f
        ? Quaternion.LookRotation(facing.normalized, Vector3.up)
        : Quaternion.identity;
    }

    /// <summary>
    /// Removes every standing object whose footprint overlaps the given one, so a freely-placed
    /// special never lands inside a house. This is a silent clearing, not a destruction action, so
    /// the house is removed instantly with no break FX/physics. <paramref name="exclude"/> keeps a
    /// moving object from clearing itself.
    /// </summary>
    private void ClearOverlapping(Vector3 position, Quaternion rotation, Vector2 worldHalfExtents, Transform exclude)
    {
      ClearOverlappingMarker.Begin();
      try
      {
        // Snapshot first: removing a house fires its Destroyed event synchronously, which removes it
        // from _objects via Release and would otherwise mutate the dictionary mid-enumeration.
        var standingObjects = new List<RuntimeEnvironmentObject>(_objects.Values);
        foreach (var standing in standingObjects)
        {
          // This pass is for grid houses only. Specials are kept separate because an inaccurate
          // footprint must never turn a placement attempt into destruction of another objective.
          if (_specialIds.Contains(standing.Id))
            continue;

          if (standing.Destructible != null
              && (exclude == null || !standing.Destructible.transform.IsChildOf(exclude))
              && RectanglesOverlap(
                position, rotation.eulerAngles.y, worldHalfExtents,
                standing.WorldCenter, standing.WorldRotation.eulerAngles.y, standing.WorldHalfExtents))
            standing.Destructible.DestroyInstant();
        }
      }
      finally
      {
        ClearOverlappingMarker.End();
      }
    }

    /// <summary>
    /// Rolls up to <see cref="MaxPlacementAttempts"/> random placements in the given annulus, and
    /// returns the first one that doesn't land on top of another live special. Houses are never a
    /// reason to retry — those are cleared out of the way instead — only other specials are.
    /// </summary>
    private bool TryPickNonOverlappingPlacement(
      Vector3 anchor, Transform lookTarget, float minDistance, float maxDistance, Vector2 worldHalfExtents, Transform exclude,
      out Vector3 position, out Quaternion rotation)
    {
      TryPickPlacementMarker.Begin();
      try
      {
        for (var attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
          PickPlacement(anchor, lookTarget, minDistance, maxDistance, out position, out rotation);
          if (!HasSpecialOverlap(position, rotation, worldHalfExtents, exclude, null))
            return true;
        }

        position = default;
        rotation = default;
        return false;
      }
      finally
      {
        TryPickPlacementMarker.End();
      }
    }

    /// <summary>
    /// Whether the given footprint overlaps any currently live special (Timer, Arrow, ...),
    /// checked against the <see cref="_specialIds"/> index rather than houses. <paramref name="exclude"/>
    /// keeps a special being relocated from colliding with itself.
    /// </summary>
    private bool HasSpecialOverlap(
      Vector3 position, Quaternion rotation, Vector2 worldHalfExtents, Transform exclude, ISet<int> ignoredSpecialIds)
    {
      if (TryGetSceneGoalFootprint(out var goalCenter, out var goalHalfExtents)
          && RectanglesOverlap(position, rotation.eulerAngles.y, worldHalfExtents, goalCenter, 0f, goalHalfExtents))
        return true;

      foreach (var id in _specialIds)
      {
        if (!_objects.TryGetValue(id, out var standing))
          continue;

        if (ignoredSpecialIds != null && ignoredSpecialIds.Contains(id))
          continue;

        if (exclude != null && standing.Destructible != null && standing.Destructible.transform.IsChildOf(exclude))
          continue;

        if (RectanglesOverlap(
          position, rotation.eulerAngles.y, worldHalfExtents,
          standing.WorldCenter, standing.WorldRotation.eulerAngles.y, standing.WorldHalfExtents))
          return true;
      }

      return false;
    }

    private void ClearSceneGoalFootprint()
    {
      if (!TryGetSceneGoalFootprint(out var goalCenter, out var goalHalfExtents))
        return;

      ClearOverlapping(goalCenter, Quaternion.identity, goalHalfExtents + ClearMargin, null);
    }

    private static bool TryGetSceneGoalFootprint(out Vector3 center, out Vector2 halfExtents)
    {
      var goal = TheGoal.Current;
      if (goal == null)
        goal = Object.FindFirstObjectByType<TheGoal>();

      var collider = goal != null ? goal.GetComponent<BoxCollider>() : null;
      if (goal == null || goal.IsDestroyed || collider == null || !collider.enabled)
      {
        center = default;
        halfExtents = default;
        return false;
      }

      var bounds = collider.bounds;
      center = bounds.center;
      halfExtents = new Vector2(bounds.extents.x, bounds.extents.z);
      return true;
    }

    /// <summary>
    /// The special's real footprint in the XZ plane, taken from its prefab's own renderer bounds —
    /// the grid <c>size</c> it's configured with is ignored entirely, since that's sized for the
    /// blocky grid fill, not for a freely-placed object. Cached per type since the prefab never
    /// changes between spawns.
    /// </summary>
    private Vector2 SpecialHalfExtents(SpecialHouseObject special)
    {
      if (_specialHalfExtentsCache.TryGetValue(special.type, out var cached))
        return cached;

      var halfExtents = PrefabHalfExtents(special.prefab);
      _specialHalfExtentsCache[special.type] = halfExtents;
      return halfExtents;
    }

    private static Vector2 ClearMargin => new(SpecialClearMargin, SpecialClearMargin);

    private Vector2 PrefabHalfExtents(GameObject prefab)
    {
      _rendererScratch.Clear();
      prefab.GetComponentsInChildren<Renderer>(true, _rendererScratch);
      if (_rendererScratch.Count == 0)
        return Vector2.zero;

      var bounds = _rendererScratch[0].bounds;
      for (var i = 1; i < _rendererScratch.Count; i++)
        bounds.Encapsulate(_rendererScratch[i].bounds);

      return new Vector2(bounds.extents.x, bounds.extents.z);
    }

    private Vector2Int CellOf(Vector3 position) =>
      new(Mathf.RoundToInt(position.x / _cellSize), Mathf.RoundToInt(position.z / _cellSize));

    /// 2D SAT test in the XZ plane between two oriented rectangles. Grid houses pass an identity
    /// rotation, while specials retain their actual world rotation after every spawn or move.
    private static bool RectanglesOverlap(
      Vector3 centerA, float rotationADegrees, Vector2 halfExtentsA,
      Vector3 centerB, float rotationBDegrees, Vector2 halfExtentsB)
    {
      var angleA = rotationADegrees * Mathf.Deg2Rad;
      var angleB = rotationBDegrees * Mathf.Deg2Rad;
      // Unity's yaw rotates +Z, while the flattened Vector2 uses (X, Z). Keep axis 0 on the
      // object's local +X and axis 1 on local +Z so the half-extents remain (x, z).
      var axisA0 = new Vector2(Mathf.Cos(angleA), -Mathf.Sin(angleA));
      var axisA1 = new Vector2(Mathf.Sin(angleA), Mathf.Cos(angleA));
      var axisB0 = new Vector2(Mathf.Cos(angleB), -Mathf.Sin(angleB));
      var axisB1 = new Vector2(Mathf.Sin(angleB), Mathf.Cos(angleB));

      System.Span<Vector2> axes = stackalloc Vector2[] { axisA0, axisA1, axisB0, axisB1 };
      var delta = new Vector2(centerB.x - centerA.x, centerB.z - centerA.z);

      foreach (var axis in axes)
      {
        var projectionA = Mathf.Abs(Vector2.Dot(axisA0, axis)) * halfExtentsA.x + Mathf.Abs(Vector2.Dot(axisA1, axis)) * halfExtentsA.y;
        var projectionB = Mathf.Abs(Vector2.Dot(axisB0, axis)) * halfExtentsB.x + Mathf.Abs(Vector2.Dot(axisB1, axis)) * halfExtentsB.y;
        var distance = Mathf.Abs(Vector2.Dot(delta, axis));
        if (distance > projectionA + projectionB)
          return false;
      }

      return true;
    }

    private void InitializeRenderCulling(int objectCount)
    {
      if (objectCount <= 0)
        return;

      _rendererStartByObject = new int[objectCount];
      _rendererCountByObject = new int[objectCount];
      _allRenderers.Clear();
      _boundingSpheres = new BoundingSphere[objectCount];
      _distanceVisibleByObject = new bool[objectCount];

      var camera = Camera.main;
      if (camera == null)
        camera = Object.FindFirstObjectByType<Camera>();

      // A missing camera is a valid editor/test setup. Leave all renderers enabled in that case;
      // silently hiding the whole environment is much harder to diagnose than a missed culling
      // opportunity.
      if (camera == null)
        return;

      // Keep targetCamera assigned so Unity runs the CullingGroup state updates. The callback below
      // intentionally ignores CullingGroupEvent.isVisible: that value is frustum visibility, not
      // distance eligibility, and using it would make the configured near-player band collapse to
      // the current viewport after movement.
      _cullingGroup = new CullingGroup
      {
        targetCamera = camera,
      };
      var visibleRadius = _visibilitySettings.VisibleRadius;
      var hiddenRadius = _visibilitySettings.HiddenRadius;
      _hasVisibilityHysteresis = hiddenRadius > visibleRadius;
      _cullingGroup.SetBoundingDistances(_hasVisibilityHysteresis
        ? new[] { visibleRadius, hiddenRadius }
        : new[] { visibleRadius });
      _cullingGroup.onStateChanged += OnCullingStateChanged;

      var player = GameObject.FindGameObjectWithTag("Player");
      if (player != null)
        _cullingGroup.SetDistanceReferencePoint(player.transform);
      else
        _cullingGroup.SetDistanceReferencePoint(camera.transform);
    }

    private void RegisterRenderers(int index, GameObject instance)
    {
      _rendererScratch.Clear();
      instance.GetComponentsInChildren<Renderer>(true, _rendererScratch);

      if (_cullingGroup == null)
        return;

      _rendererStartByObject[index] = _allRenderers.Count;
      _rendererCountByObject[index] = _rendererScratch.Count;
      _allRenderers.AddRange(_rendererScratch);
      _distanceVisibleByObject[index] = true;

      var bounds = new Bounds(instance.transform.position, Vector3.zero);
      if (_rendererScratch.Count > 0)
      {
        bounds = _rendererScratch[0].bounds;
        for (var i = 1; i < _rendererScratch.Count; i++)
          bounds.Encapsulate(_rendererScratch[i].bounds);
      }

      _boundingSpheres[index] = new BoundingSphere(bounds.center, bounds.extents.magnitude);
    }

    private void OnCullingStateChanged(CullingGroupEvent eventData)
    {
      var index = eventData.index;
      if (index < 0 || index >= _rendererStartByObject.Length)
        return;

      var visible = _distanceVisibleByObject[index];
      if (eventData.currentDistance == 0)
        visible = true;
      else if (!_hasVisibilityHysteresis || eventData.currentDistance > 1)
        visible = false;

      if (_distanceVisibleByObject[index] == visible)
        return;

      _distanceVisibleByObject[index] = visible;
      ApplyVisibility(index, visible);
    }

    private void ApplyVisibility(int index, bool visible)
    {
      if (index < 0 || index >= _rendererStartByObject.Length)
        return;

      var start = _rendererStartByObject[index];
      var end = start + _rendererCountByObject[index];

      for (var i = start; i < end; i++)
      {
        var renderer = _allRenderers[i];
        if (renderer != null)
          renderer.enabled = visible;
      }
    }

    private void DisposeRenderCulling()
    {
      if (_cullingGroup != null)
      {
        _cullingGroup.onStateChanged -= OnCullingStateChanged;
        _cullingGroup.Dispose();
        _cullingGroup = null;
      }

      _rendererStartByObject = System.Array.Empty<int>();
      _rendererCountByObject = System.Array.Empty<int>();
      _allRenderers.Clear();
      _rendererScratch.Clear();
      _boundingSpheres = System.Array.Empty<BoundingSphere>();
      _distanceVisibleByObject = System.Array.Empty<bool>();
      _hasVisibilityHysteresis = false;
    }

    void System.IDisposable.Dispose()
    {
      DisposeRenderCulling();
    }

    private bool TryReserve(Vector2Int origin, Vector2Int size)
    {
      for (var x = 0; x < size.x; x++)
      for (var y = 0; y < size.y; y++)
        if (_occupied.Contains(origin + new Vector2Int(x, y)))
          return false;

      for (var x = 0; x < size.x; x++)
      for (var y = 0; y < size.y; y++)
        _occupied.Add(origin + new Vector2Int(x, y));

      return true;
    }

    private void Release(int id)
    {
      if (!_objects.TryGetValue(id, out var runtimeObject))
        return;

      for (var x = 0; x < runtimeObject.Size.x; x++)
      for (var y = 0; y < runtimeObject.Size.y; y++)
        _occupied.Remove(runtimeObject.Cell + new Vector2Int(x, y));

      _objects.Remove(id);
      _specialIds.Remove(id);

      SpecialHouses? releasedType = null;
      foreach (var pair in _currentSpecial)
        if (pair.Value.id == id)
        {
          releasedType = pair.Key;
          break;
        }

      if (releasedType.HasValue)
        _currentSpecial.Remove(releasedType.Value);
    }
  }
}
