using System.Collections.Generic;
using Destruction;
using Movement;
using UnityEngine;

namespace Map
{
  public class MapEnvironmentSpawner
  {
    private const string ContainerName = "SpawnedEnvironment";

    private readonly Dictionary<int, RuntimeEnvironmentObject> _objects = new();
    private readonly HashSet<Vector2Int> _occupied = new();
    private readonly MovementUpdater _movementUpdater;

    private Transform _container;
    private int _nextId;

    public MapEnvironmentSpawner(MovementUpdater movementUpdater) =>
      _movementUpdater = movementUpdater;

    public IReadOnlyCollection<RuntimeEnvironmentObject> SpawnedObjects => _objects.Values;

    public void Spawn(MapData mapData, HouseSet houseSet, int seed, Transform parent)
    {
      if (mapData == null || houseSet == null)
        return;

      // This service outlives the scene, so the bookkeeping from the previous level is still here.
      // Everything it describes died with that scene; leaving it would keep those cells reserved
      // forever and silently skip every object that survived the last battle.
      _objects.Clear();
      _occupied.Clear();

      _container = new GameObject(ContainerName).transform;
      _container.SetParent(parent, false);

      var cellSize = mapData.CellSize;
      var originCell = new Vector2(parent.position.x / cellSize, parent.position.z / cellSize);

      foreach (var placement in MapFiller.Fill(mapData, houseSet, originCell, seed))
      {
        if (!TryReserve(placement.Cell, placement.House.size))
          continue;

        var position = new Vector3(
          placement.Cell.x * cellSize + placement.House.size.x * cellSize * 0.5f,
          0f,
          placement.Cell.y * cellSize + placement.House.size.y * cellSize * 0.5f);

        var instance = Object.Instantiate(placement.House.prefab, position, Quaternion.identity, _container);
        instance.name = placement.House.name;

        var destructible = instance.GetComponentInChildren<DestructibleObject>();
        var id = _nextId++;
        var runtimeObject = new RuntimeEnvironmentObject(id, placement.Cell, placement.House.size, destructible);
        _objects.Add(id, runtimeObject);

        if (destructible != null)
          destructible.Destroyed += _ => Release(id);
      }

      _movementUpdater.RefreshNoGoZones();
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
