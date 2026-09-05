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

    private Transform _container;
    private int _nextId;
    private CullingGroup _cullingGroup;
    private Renderer[][] _renderersByObject = System.Array.Empty<Renderer[]>();
    private BoundingSphere[] _boundingSpheres = System.Array.Empty<BoundingSphere>();
    private bool[] _distanceVisibleByObject = System.Array.Empty<bool>();
    private bool[] _frustumVisibleByObject = System.Array.Empty<bool>();
    private bool _hasVisibilityHysteresis;

    public MapEnvironmentSpawner(MovementUpdater movementUpdater, EnvironmentVisibilitySettings visibilitySettings)
    {
      _movementUpdater = movementUpdater;
      _visibilitySettings = visibilitySettings;
    }

    public IReadOnlyCollection<RuntimeEnvironmentObject> SpawnedObjects => _objects.Values;

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

      var cellSize = mapData.CellSize;
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
        RegisterRenderers(placementIndex, instance);

        var destructible = instance.GetComponentInChildren<DestructibleObject>();
        var id = _nextId++;
        var runtimeObject = new RuntimeEnvironmentObject(id, placement.Cell, placement.House.size, destructible);
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
