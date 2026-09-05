using Balance;
using System.Collections.Generic;
using Destruction;
using Movement;
using UnityEngine;

namespace Map
{
  public class MapEnvironmentSpawner
  {
    private const string ContainerName = "SpawnedEnvironment";
    // The camera only shows a small part of the map at once. Keeping every child renderer of every
    // destructible building registered with the renderer culler makes the CPU pay for tens of
    // thousands of renderers every frame, even when they are hundreds of metres away. Buildings
    // remain active (their navigation colliders and destruction scripts still work); only their
    // visual renderers are distance/frustum culled as a group. The distance bands are exposed in
    // EnvironmentVisibilitySettings so they can be tuned without changing code.

    private readonly Dictionary<int, RuntimeEnvironmentObject> _objects = new();
    private readonly HashSet<Vector2Int> _occupied = new();
    private readonly MovementUpdater _movementUpdater;
    private readonly EnvironmentVisibilitySettings _visibilitySettings;
    private readonly SpecialSpawnSettings _specialSpawnSettings;

    private Transform _container;
    private HouseSet _houseSet;
    private int _cellSize;
    private int _nextId;
    private CullingGroup _cullingGroup;
    private Renderer[][] _renderersByObject = System.Array.Empty<Renderer[]>();
    private BoundingSphere[] _boundingSpheres = System.Array.Empty<BoundingSphere>();
    private bool[] _distanceVisibleByObject = System.Array.Empty<bool>();
    private bool[] _frustumVisibleByObject = System.Array.Empty<bool>();
    private bool _hasVisibilityHysteresis;

    public MapEnvironmentSpawner(
      MovementUpdater movementUpdater, EnvironmentVisibilitySettings visibilitySettings, SpecialSpawnSettings specialSpawnSettings)
    {
      _movementUpdater = movementUpdater;
      _visibilitySettings = visibilitySettings;
      _specialSpawnSettings = specialSpawnSettings;
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
      _occupied.Clear();
      DisposeRenderCulling();

      _container = new GameObject(ContainerName).transform;
      _container.SetParent(parent, false);

      _houseSet = houseSet;
      var cellSize = mapData.CellSize;
      _cellSize = cellSize;
      var originCell = new Vector2(parent.position.x / cellSize, parent.position.z / cellSize);

      var placements = MapFiller.Fill(mapData, houseSet, originCell, seed);
      InitializeRenderCulling(placements.Count);

      for (var placementIndex = 0; placementIndex < placements.Count; placementIndex++)
      {
        var placement = placements[placementIndex];
        if (!TryReserve(placement.Cell, placement.House.size))
          continue;

        var position = new Vector3(
          placement.Cell.x * cellSize + placement.House.size.x * cellSize * 0.5f,
          0f,
          placement.Cell.y * cellSize + placement.House.size.y * cellSize * 0.5f);

        var instance = Object.Instantiate(placement.House.prefab, position, Quaternion.identity, _container);
        instance.name = placement.House.name;
        if (!placement.House.unique)
          RegisterRenderers(placementIndex, instance);

        var destructible = instance.GetComponentInChildren<DestructibleObject>();
        var id = _nextId++;
        var worldHalfExtents = new Vector2(placement.House.size.x * cellSize * 0.5f, placement.House.size.y * cellSize * 0.5f);
        var runtimeObject = new RuntimeEnvironmentObject(id, placement.Cell, placement.House.size, destructible, position, worldHalfExtents);
        _objects.Add(id, runtimeObject);

        if (destructible != null)
          destructible.Destroyed += _ => Release(id);
      }

      if (roadSet != null)
        foreach (var placement in RoadFiller.Fill(mapData, roadSet))
        {
          var position = new Vector3(
            placement.Cell.x * cellSize + cellSize * 0.5f,
            0f,
            placement.Cell.y * cellSize + cellSize * 0.5f);
          var rotation = Quaternion.Euler(0f, placement.RotationDegrees, 0f);

          var instance = Object.Instantiate(placement.Piece.prefab, position, rotation, _container);
          instance.name = placement.Piece.name;
        }

      if (sidewalkSet != null)
        foreach (var placement in SidewalkFiller.Fill(mapData, sidewalkSet, seed))
        {
          var position = new Vector3(
            placement.Cell.x * cellSize + cellSize * 0.5f,
            0f,
            placement.Cell.y * cellSize + cellSize * 0.5f);

          var instance = Object.Instantiate(placement.Piece.prefab, position, Quaternion.identity, _container);
          instance.name = placement.Piece.name;
        }

      if (_cullingGroup != null)
      {
        _cullingGroup.SetBoundingSpheres(_boundingSpheres);
        _cullingGroup.SetBoundingSphereCount(_boundingSpheres.Length);
      }

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

        if (_specialSpawnSettings == null || !_specialSpawnSettings.TryGetInitialMaxDistance(special.type, out var timerMaxDistance))
        {
          Debug.LogWarning($"MapEnvironmentSpawner.SpawnInitialSpecials: no initial spawn distance configured for '{special.type}' in SpecialSpawnSettings.");
          continue;
        }

        TrySpawnSpecial(special.type, fallbackAnchor, lookTarget, 0f, timerMaxDistance, out timerInstance);
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
        else if (_specialSpawnSettings != null && _specialSpawnSettings.TryGetInitialMaxDistance(special.type, out maxDistance))
        {
          anchor = fallbackAnchor;
          minDistance = 0f;
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
    public static void GetOtherSpecialPlacement(
      Vector3 timerPosition, Vector3 playerPosition, out Vector3 anchor, out float minDistance, out float maxDistance)
    {
      var distanceToPlayer = Vector3.Distance(timerPosition, playerPosition);
      if (distanceToPlayer > BattleBalance.SpecialBetweenMaxDistance)
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
        minDistance = BattleBalance.SpecialBetweenMinDistance;
        maxDistance = BattleBalance.SpecialBetweenMaxDistance;
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

      SpecialHouseObject special = null;
      if (_houseSet != null)
        foreach (var candidate in _houseSet.Specials)
          if (candidate.type == type && candidate.enabled && candidate.prefab != null)
          {
            special = candidate;
            break;
          }

      if (special == null)
      {
        Debug.LogWarning($"MapEnvironmentSpawner.TrySpawnSpecial: no enabled '{type}' entry configured on the HouseSet.");
        return false;
      }

      var angle = Random.Range(0f, Mathf.PI * 2f);
      var radius = Random.Range(minDistance, maxDistance);
      var position = anchor + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

      var facing = lookTarget != null ? lookTarget.position - position : Vector3.zero;
      facing.y = 0f;
      var rotation = facing.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(facing.normalized, Vector3.up) : Quaternion.identity;

      var worldHalfExtents = new Vector2(special.size.x * _cellSize * 0.5f, special.size.y * _cellSize * 0.5f);

      // Snapshot first: breaking a house fires its Destroyed event synchronously, which removes it
      // from _objects via Release and would otherwise mutate the dictionary mid-enumeration.
      var standingObjects = new List<RuntimeEnvironmentObject>(_objects.Values);
      foreach (var standing in standingObjects)
        if (standing.Destructible != null
            && RectanglesOverlap(position, rotation.eulerAngles.y, worldHalfExtents, standing.WorldCenter, standing.WorldHalfExtents))
          standing.Destructible.Break(position);

      instance = Object.Instantiate(special.prefab, position, rotation, _container);
      instance.name = special.type.ToString();

      var destructible = instance.GetComponentInChildren<DestructibleObject>();
      var id = _nextId++;
      var cell = new Vector2Int(Mathf.RoundToInt(position.x / _cellSize), Mathf.RoundToInt(position.z / _cellSize));
      var runtimeObject = new RuntimeEnvironmentObject(id, cell, special.size, destructible, position, worldHalfExtents);
      _objects.Add(id, runtimeObject);

      if (destructible != null)
        destructible.Destroyed += _ => Release(id);

      _movementUpdater.RefreshNoGoZones();
      return true;
    }

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
      if (_cullingGroup == null)
        return;

      var renderers = instance.GetComponentsInChildren<Renderer>(true);
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
    }
  }
}
