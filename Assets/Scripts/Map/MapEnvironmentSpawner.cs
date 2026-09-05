using Balance;
using System.Collections.Generic;
using Destruction;
using Movement;
using UnityEngine;

namespace Map
{
  public class MapEnvironmentSpawner : System.IDisposable
  {
    private const string ContainerName = "SpawnedEnvironment";
    // Extra margin added on top of a special's real bounding box when clearing houses out of its
    // way, so the cleared area isn't drawn exactly flush with the mesh.
    private const float SpecialClearMargin = 5f;
    private const int SpecialPlacementAttemptCount = 20;
    // The camera only shows a small part of the map at once. Keeping every child renderer of every
    // spawned map element registered with the renderer culler makes the CPU pay for tens of
    // thousands of renderers every frame, even when they are hundreds of metres away. Buildings
    // and ground cells remain active (their colliders and destruction scripts still work); only
    // their visual renderers are distance/frustum culled as a group. The distance bands are
    // exposed in EnvironmentVisibilitySettings so they can be tuned without changing code.

    private readonly Dictionary<int, RuntimeEnvironmentObject> _objects = new();
    // Index of ids in _objects that are specials (Timer, Arrow, ...) rather than grid houses, so a
    // new special can be checked against other live specials without disturbing houses.
    private readonly HashSet<int> _specialIds = new();
    // Real footprint of each special's prefab (from its renderer bounds, not the grid size it's
    // configured with), computed once per type and reused on every spawn/respawn.
    private readonly Dictionary<SpecialHouses, Vector2> _specialHalfExtentsCache = new();
    private readonly HashSet<Vector2Int> _occupied = new();
    private readonly MovementUpdater _movementUpdater;
    private readonly EnvironmentVisibilitySettings _visibilitySettings;
    private readonly SpecialSpawnSettings _specialSpawnSettings;
    private readonly BattleBalanceConfig _battleBalance;

    private Transform _container;
    private HouseSet _houseSet;
    private GroundDamageMask _groundDamageMask;
    private int _cellSize;
    private int _nextId;
    private CullingGroup _cullingGroup;
    private Renderer[][] _renderersByObject = System.Array.Empty<Renderer[]>();
    private BoundingSphere[] _boundingSpheres = System.Array.Empty<BoundingSphere>();
    private bool[] _distanceVisibleByObject = System.Array.Empty<bool>();
    private bool[] _frustumVisibleByObject = System.Array.Empty<bool>();
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
    public HouseSet CurrentHouseSet => _houseSet;

    public void Spawn(MapData mapData, HouseSet houseSet, RoadSet roadSet, SidewalkSet sidewalkSet, int seed, Transform parent)
    {
      if (mapData == null)
        return;

      // This service outlives the scene, so the bookkeeping from the previous level is still here.
      // Everything it describes died with that scene; leaving it would keep those cells reserved
      // forever and silently skip every object that survived the last battle.
      _objects.Clear();
      _specialIds.Clear();
      _occupied.Clear();
      DisposeRenderCulling();
      _groundDamageMask = GroundDamageMask.Instance;

      _container = new GameObject(ContainerName).transform;
      _container.SetParent(parent, false);

      _houseSet = houseSet;
      var cellSize = mapData.CellSize;
      _cellSize = cellSize;
      var originCell = new Vector2(parent.position.x / cellSize, parent.position.z / cellSize);

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
        if (!TryReserve(placement.Cell, placement.House.size))
          continue;

        var position = new Vector3(
          placement.Cell.x * cellSize + placement.House.size.x * cellSize * 0.5f,
          0f,
          placement.Cell.y * cellSize + placement.House.size.y * cellSize * 0.5f);

        var instance = Object.Instantiate(placement.House.prefab, position, Quaternion.identity, _container);
        instance.name = placement.House.name;
        RegisterRenderers(placementIndex, instance);

        var destructible = instance.GetComponentInChildren<DestructibleObject>();
        var id = _nextId++;
        var worldHalfExtents = new Vector2(placement.House.size.x * cellSize * 0.5f, placement.House.size.y * cellSize * 0.5f);
        var runtimeObject = new RuntimeEnvironmentObject(id, placement.Cell, placement.House.size, destructible, position, worldHalfExtents);
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

        var instance = Object.Instantiate(placement.Piece.prefab, position, Quaternion.identity, _container);
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
      _movementUpdater.RefreshNoGoZones();

      SpawnInitialSpecials(parent);
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
    /// Spawns a configured special object (e.g. the battle timer) at runtime, at a random point in
    /// the annulus [<paramref name="minDistance"/>, <paramref name="maxDistance"/>] around
    /// <paramref name="anchor"/>, facing <paramref name="lookTarget"/>. Unlike <see cref="Spawn"/>,
    /// this does not use the map grid: any currently standing house whose footprint overlaps the
    /// special's footprint is broken to make room for it.
    /// </summary>
    public bool TrySpawnSpecial(
      SpecialHouses type, Vector3 anchor, Transform lookTarget, float minDistance, float maxDistance, out GameObject instance)
    {
      instance = null;

      if (!TryResolveSpecial(type, "TrySpawnSpecial", out var special))
        return false;

      var worldHalfExtents = SpecialHalfExtents(special);

      if (!TryPickSpecialPlacement(
            anchor, lookTarget, minDistance, maxDistance, worldHalfExtents, null,
            out var position, out var rotation))
      {
        Debug.LogWarning($"MapEnvironmentSpawner.TrySpawnSpecial: '{type}' placement overlaps another special object; spawn skipped.");
        return false;
      }

      ClearOverlapping(position, rotation, worldHalfExtents + ClearMargin, null);

      instance = Object.Instantiate(special.prefab, position, rotation, _container);
      instance.name = special.type.ToString();
      RegisterDamageMaskRenderers(instance.GetComponentsInChildren<Renderer>(true));

      var destructible = instance.GetComponentInChildren<DestructibleObject>();
      var id = _nextId++;
      // Vector2Int.zero: a freely-placed special never reserves grid cells via TryReserve, so it
      // must not claim a grid Size either — Release would otherwise free cells on the grid that
      // happen to fall under its footprint but were never its own.
      var runtimeObject = new RuntimeEnvironmentObject(
        id, CellOf(position), Vector2Int.zero, destructible, position, worldHalfExtents);
      _objects.Add(id, runtimeObject);
      _specialIds.Add(id);

      if (destructible != null)
        destructible.Destroyed += _ => Release(id);

      _movementUpdater.RefreshNoGoZones();
      return true;
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
      if (instance == null)
        return false;

      if (!TryResolveSpecial(type, "TryMoveSpecial", out var special))
        return false;

      var worldHalfExtents = SpecialHalfExtents(special);

      if (!TryPickSpecialPlacement(
            anchor, lookTarget, minDistance, maxDistance, worldHalfExtents, instance.transform,
            out var position, out var rotation))
      {
        Debug.LogWarning($"MapEnvironmentSpawner.TryMoveSpecial: '{type}' relocation overlaps another special object; move skipped.");
        return false;
      }

      // Excluded from the sweep, or an object whose new footprint overlaps its old one would break
      // itself on arrival.
      ClearOverlapping(position, rotation, worldHalfExtents + ClearMargin, instance.transform);

      instance.transform.SetPositionAndRotation(position, rotation);
      Reseat(instance.transform, position, worldHalfExtents);

      _movementUpdater.RefreshNoGoZones();
      return true;
    }

    /// <summary>
    /// Rewrites the moved object's registered footprint. Without this the stale one keeps standing
    /// in for it: later specials would clear houses around the spot it left and land on top of it at
    /// the spot it moved to.
    /// </summary>
    private void Reseat(Transform instance, Vector3 position, Vector2 worldHalfExtents)
    {
      foreach (var pair in _objects)
      {
        var standing = pair.Value;
        if (standing.Destructible == null || !standing.Destructible.transform.IsChildOf(instance))
          continue;

        _objects[pair.Key] = new RuntimeEnvironmentObject(
          standing.Id, CellOf(position), standing.Size, standing.Destructible, position, worldHalfExtents);
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

    private bool TryPickSpecialPlacement(
      Vector3 anchor, Transform lookTarget, float minDistance, float maxDistance,
      Vector2 worldHalfExtents, Transform exclude, out Vector3 position, out Quaternion rotation)
    {
      for (var attempt = 0; attempt < SpecialPlacementAttemptCount; attempt++)
      {
        PickPlacement(anchor, lookTarget, minDistance, maxDistance, out position, out rotation);
        if (!HasSpecialOverlap(position, rotation, worldHalfExtents, exclude))
          return true;
      }

      position = default;
      rotation = Quaternion.identity;
      return false;
    }

    /// A random point in the annulus [minDistance, maxDistance] around the anchor, facing lookTarget.
    private static void PickPlacement(
      Vector3 anchor, Transform lookTarget, float minDistance, float maxDistance, out Vector3 position, out Quaternion rotation)
    {
      var angle = Random.Range(0f, Mathf.PI * 2f);
      var radius = Random.Range(minDistance, maxDistance);
      position = anchor + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

      var facing = lookTarget != null ? lookTarget.position - position : Vector3.zero;
      facing.y = 0f;
      rotation = facing.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(facing.normalized, Vector3.up) : Quaternion.identity;
    }

    /// <summary>
    /// Removes every standing object whose footprint overlaps the given one, so a freely-placed
    /// special never lands inside a house. This is a silent clearing, not a destruction action, so
    /// the house is removed instantly with no break FX/physics. <paramref name="exclude"/> keeps a
    /// moving object from clearing itself.
    /// </summary>
    private void ClearOverlapping(Vector3 position, Quaternion rotation, Vector2 worldHalfExtents, Transform exclude)
    {
      // Snapshot first: removing a house fires its Destroyed event synchronously, which removes it
      // from _objects via Release and would otherwise mutate the dictionary mid-enumeration.
      var standingObjects = new List<RuntimeEnvironmentObject>(_objects.Values);
      foreach (var standing in standingObjects)
        if (standing.Destructible != null
            && (exclude == null || !standing.Destructible.transform.IsChildOf(exclude))
            && RectanglesOverlap(position, rotation.eulerAngles.y, worldHalfExtents, standing.WorldCenter, standing.WorldHalfExtents))
          standing.Destructible.DestroyInstant();
    }

    /// <summary>
    /// Whether the given footprint overlaps any currently live special (Timer, Arrow, ...),
    /// checked against the <see cref="_specialIds"/> index rather than houses. <paramref name="exclude"/>
    /// keeps a special being relocated from colliding with itself.
    /// </summary>
    private bool HasSpecialOverlap(Vector3 position, Quaternion rotation, Vector2 worldHalfExtents, Transform exclude)
    {
      if (TryGetSceneGoalFootprint(out var goalCenter, out var goalHalfExtents)
          && RectanglesOverlap(position, rotation.eulerAngles.y, worldHalfExtents, goalCenter, goalHalfExtents))
        return true;

      foreach (var id in _specialIds)
      {
        if (!_objects.TryGetValue(id, out var standing))
          continue;

        if (exclude != null && standing.Destructible != null && standing.Destructible.transform.IsChildOf(exclude))
          continue;

        if (RectanglesOverlap(position, rotation.eulerAngles.y, worldHalfExtents, standing.WorldCenter, standing.WorldHalfExtents))
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

    private static Vector2 PrefabHalfExtents(GameObject prefab)
    {
      var renderers = prefab.GetComponentsInChildren<Renderer>(true);
      if (renderers.Length == 0)
        return Vector2.zero;

      var bounds = renderers[0].bounds;
      for (var i = 1; i < renderers.Length; i++)
        bounds.Encapsulate(renderers[i].bounds);

      return new Vector2(bounds.extents.x, bounds.extents.z);
    }

    private Vector2Int CellOf(Vector3 position) =>
      new(Mathf.RoundToInt(position.x / _cellSize), Mathf.RoundToInt(position.z / _cellSize));

    /// 2D SAT test in the XZ plane between rectangle A (center/rotation/half-extents) and axis-aligned
    /// rectangle B, used to find which grid-placed houses a freely-placed special object overlaps.
    private static bool RectanglesOverlap(Vector3 centerA, float rotationADegrees, Vector2 halfExtentsA, Vector3 centerB, Vector2 halfExtentsB)
    {
      var angle = rotationADegrees * Mathf.Deg2Rad;
      var axisA0 = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
      var axisA1 = new Vector2(-axisA0.y, axisA0.x);

      System.Span<Vector2> axes = stackalloc Vector2[] { axisA0, axisA1, Vector2.right, Vector2.up };
      var delta = new Vector2(centerB.x - centerA.x, centerB.z - centerA.z);

      foreach (var axis in axes)
      {
        var projectionA = Mathf.Abs(Vector2.Dot(axisA0, axis)) * halfExtentsA.x + Mathf.Abs(Vector2.Dot(axisA1, axis)) * halfExtentsA.y;
        var projectionB = Mathf.Abs(axis.x) * halfExtentsB.x + Mathf.Abs(axis.y) * halfExtentsB.y;
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

      _renderersByObject = new Renderer[objectCount][];
      _boundingSpheres = new BoundingSphere[objectCount];
      _distanceVisibleByObject = new bool[objectCount];
      _frustumVisibleByObject = new bool[objectCount];

      var camera = Camera.main;
      if (camera == null)
        camera = Object.FindFirstObjectByType<Camera>();

      // A missing camera is a valid editor/test setup. Leave all renderers enabled in that case;
      // silently hiding the whole environment is much harder to diagnose than a missed culling
      // opportunity.
      if (camera == null)
        return;

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
      var renderers = instance.GetComponentsInChildren<Renderer>(true);
      RegisterDamageMaskRenderers(renderers);

      if (_cullingGroup == null)
        return;

      _renderersByObject[index] = renderers;
      _distanceVisibleByObject[index] = true;
      _frustumVisibleByObject[index] = true;

      var bounds = new Bounds(instance.transform.position, Vector3.zero);
      if (renderers.Length > 0)
      {
        bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
          bounds.Encapsulate(renderers[i].bounds);
      }

      _boundingSpheres[index] = new BoundingSphere(bounds.center, bounds.extents.magnitude);
    }

    private void RegisterDamageMaskRenderers(Renderer[] renderers)
    {
      for (var i = 0; i < renderers.Length; i++)
      {
        var renderer = renderers[i];
        if (renderer == null)
          continue;

        _groundDamageMask?.ApplyDamageMaskToRenderer(renderer);
      }
    }

    private void OnCullingStateChanged(CullingGroupEvent eventData)
    {
      var index = eventData.index;
      if (index < 0 || index >= _renderersByObject.Length)
        return;

      _frustumVisibleByObject[index] = eventData.isVisible;

      if (eventData.currentDistance == 0)
        _distanceVisibleByObject[index] = true;
      else if (!_hasVisibilityHysteresis || eventData.currentDistance > 1)
        _distanceVisibleByObject[index] = false;

      ApplyVisibility(index, _frustumVisibleByObject[index] && _distanceVisibleByObject[index]);
    }

    private void ApplyVisibility(int index, bool visible)
    {
      if (index < 0 || index >= _renderersByObject.Length)
        return;

      var renderers = _renderersByObject[index];
      if (renderers == null)
        return;

      for (var i = 0; i < renderers.Length; i++)
      {
        var renderer = renderers[i];
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

      _renderersByObject = System.Array.Empty<Renderer[]>();
      _boundingSpheres = System.Array.Empty<BoundingSphere>();
      _distanceVisibleByObject = System.Array.Empty<bool>();
      _frustumVisibleByObject = System.Array.Empty<bool>();
      _hasVisibilityHysteresis = false;
    }

    void System.IDisposable.Dispose()
    {
      DisposeRenderCulling();
      _groundDamageMask = null;
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
    }
  }
}
