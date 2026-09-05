using System.Collections.Generic;
using App;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using VContainer;

namespace Map
{
  public class LevelData : MonoBehaviour
  {
    private const string ContainerName = "SpawnedHouses";

    [SerializeField] private MapData mapData;
    [SerializeField] private HouseSet houseSet;
    [SerializeField] private RoadSet roadSet;
    [SerializeField] private SidewalkSet sidewalkSet;
    [SerializeField] private int seed;
    [SerializeField] private int gridExtent = 10;
    [SerializeField] private bool showGrid = true;
    [HideInInspector] public bool isEditing;

    [Inject] private MapEnvironmentSpawner _spawner;

    public MapData MapData => mapData;
    public HouseSet HouseSet => houseSet;
    public RoadSet RoadSet => roadSet;
    public SidewalkSet SidewalkSet => sidewalkSet;

    private void Awake() => this.AsInjected();

    private void Start() => _spawner.Spawn(mapData, houseSet, roadSet, sidewalkSet, seed, transform);

    private void OnDrawGizmos()
    {
      if (!showGrid || mapData == null)
        return;

      var cellSize = mapData.CellSize;

      Gizmos.color = isEditing ? new Color(0.2f, 0.8f, 1f, 0.5f) : new Color(0.2f, 0.8f, 0.2f, 0.5f);
      foreach (var cell in mapData.FilledCells)
      {
        var center = new Vector3(
          cell.x * cellSize + cellSize * 0.5f,
          0f,
          cell.y * cellSize + cellSize * 0.5f);
        Gizmos.DrawCube(center, new Vector3(cellSize, 0.1f, cellSize));
      }

      DrawRoadGizmos(mapData.RoadCells, cellSize);
      DrawZoneGizmos(mapData.SidewalkCells, cellSize, new Color(0.9f, 0.5f, 0.1f, 0.5f));

      var extent = gridExtent * cellSize;
      Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
      for (var i = -gridExtent; i <= gridExtent; i++)
      {
        var offset = i * cellSize;
        Gizmos.DrawLine(new Vector3(offset, 0f, -extent), new Vector3(offset, 0f, extent));
        Gizmos.DrawLine(new Vector3(-extent, 0f, offset), new Vector3(extent, 0f, offset));
      }
    }

    private static void DrawZoneGizmos(IReadOnlyList<Vector2Int> cells, int cellSize, Color color)
    {
      Gizmos.color = color;
      foreach (var cell in cells)
      {
        var center = new Vector3(
          cell.x * cellSize + cellSize * 0.5f,
          0f,
          cell.y * cellSize + cellSize * 0.5f);
        Gizmos.DrawCube(center, new Vector3(cellSize, 0.1f, cellSize));
      }
    }

    private static void DrawRoadGizmos(IReadOnlyList<RoadCellData> cells, int cellSize)
    {
      foreach (var road in cells)
      {
        Gizmos.color = road.width == RoadWidth.OneWay
          ? new Color(0.9f, 0.9f, 0.2f, 0.5f)
          : new Color(0.6f, 0.6f, 0.1f, 0.5f);

        var center = new Vector3(
          road.cell.x * cellSize + cellSize * 0.5f,
          0f,
          road.cell.y * cellSize + cellSize * 0.5f);
        Gizmos.DrawCube(center, new Vector3(cellSize, 0.1f, cellSize));
      }
    }

    public void Fill()
    {
      Clear();

      if (mapData == null)
        return;

      var container = CreateContainer();
      var cellSize = mapData.CellSize;
      var originCell = new Vector2(transform.position.x / cellSize, transform.position.z / cellSize);

      if (houseSet != null)
        foreach (var placement in MapFiller.Fill(mapData, houseSet, originCell, seed))
        {
          var position = new Vector3(
            placement.Cell.x * cellSize + placement.House.size.x * cellSize * 0.5f,
            0f,
            placement.Cell.y * cellSize + placement.House.size.y * cellSize * 0.5f);

          SpawnInstance(placement.House.prefab, placement.House.name, position, Quaternion.identity, container);
        }

      if (roadSet != null)
        foreach (var placement in RoadFiller.Fill(mapData, roadSet, seed))
        {
          var cellPosition = new Vector2(
            placement.Cell.x + 0.5f + placement.CellOffset.x,
            placement.Cell.y + 0.5f + placement.CellOffset.y);
          var position = new Vector3(
            cellPosition.x * cellSize,
            0f,
            cellPosition.y * cellSize);
          var rotation = Quaternion.Euler(0f, placement.RotationDegrees, 0f);

          SpawnInstance(placement.Piece.prefab, placement.Piece.name, position, rotation, container);
        }

      if (sidewalkSet != null)
        foreach (var placement in SidewalkFiller.Fill(mapData, sidewalkSet, seed))
        {
          var position = new Vector3(
            placement.Cell.x * cellSize + cellSize * 0.5f,
            0f,
            placement.Cell.y * cellSize + cellSize * 0.5f);

          SpawnInstance(placement.Piece.prefab, placement.Piece.name, position, Quaternion.identity, container);
        }
    }

    private static void SpawnInstance(
      GameObject prefab, string name, Vector3 position, Quaternion rotation, Transform container)
    {
      var instance = Instantiate(prefab, position, rotation, container);
      instance.name = name;

#if UNITY_EDITOR
      if (!Application.isPlaying)
        Undo.RegisterCreatedObjectUndo(instance, "Simulate Placement");
#endif
    }

    public void Clear()
    {
      var container = transform.Find(ContainerName);
      if (container == null)
        return;

      if (Application.isPlaying)
      {
        Destroy(container.gameObject);
      }
      else
      {
#if UNITY_EDITOR
        Undo.DestroyObjectImmediate(container.gameObject);
#else
        DestroyImmediate(container.gameObject);
#endif
      }
    }

    private Transform CreateContainer()
    {
      var container = new GameObject(ContainerName).transform;
      container.SetParent(transform, false);

      var parentScale = transform.lossyScale;
      container.localScale = new Vector3(
        parentScale.x != 0f ? 1f / parentScale.x : 1f,
        parentScale.y != 0f ? 1f / parentScale.y : 1f,
        parentScale.z != 0f ? 1f / parentScale.z : 1f);

#if UNITY_EDITOR
      if (!Application.isPlaying)
        Undo.RegisterCreatedObjectUndo(container.gameObject, "Simulate Placement");
#endif

      return container;
    }
  }
}
