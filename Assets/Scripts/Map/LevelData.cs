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
    [SerializeField] private bool showTimerRoute = true;
    [SerializeField] private bool showTimerRouteLabels = true;
    [HideInInspector] public bool isEditing;

    [Inject] private MapEnvironmentSpawner _spawner;

    public MapData MapData => mapData;
    public HouseSet HouseSet => houseSet;
    public RoadSet RoadSet => roadSet;
    public SidewalkSet SidewalkSet => sidewalkSet;

    /// <summary>
    /// The world-space footprint covered by the cells in this level. Map cells are authored in
    /// world grid coordinates, so the bounds include the complete outer edge of the outermost
    /// cell rather than just its center.
    /// </summary>
    public bool TryGetWorldBounds(out Bounds bounds)
    {
      bounds = default;
      if (mapData == null || mapData.CellSize <= 0)
        return false;

      var hasCells = false;
      var minCell = new Vector2Int(int.MaxValue, int.MaxValue);
      var maxCell = new Vector2Int(int.MinValue, int.MinValue);

      IncludeCells(mapData.FilledCells, ref hasCells, ref minCell, ref maxCell);
      IncludeRoadCells(mapData.RoadCells, ref hasCells, ref minCell, ref maxCell);
      IncludeCells(mapData.SidewalkCells, ref hasCells, ref minCell, ref maxCell);

      if (!hasCells)
        return false;

      var cellSize = mapData.CellSize;
      var min = new Vector3(minCell.x * cellSize, 0f, minCell.y * cellSize);
      var max = new Vector3((maxCell.x + 1) * cellSize, 0f, (maxCell.y + 1) * cellSize);
      bounds = new Bounds((min + max) * 0.5f, new Vector3(max.x - min.x, 0f, max.z - min.z));
      return true;
    }

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

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
      if (!showTimerRoute)
        return;

      var player = GameObject.FindGameObjectWithTag("Player");
      var origin = player != null ? player.transform.position : transform.position;
      var battleBalance = Resources.Load<Balance.BattleBalanceConfig>("BattleBalanceConfig");
      var spawnSettings = Resources.Load<SpecialSpawnSettings>("SpecialSpawnSettings");
      if (!TimerRoute.TryCreateForBattle(
        origin,
        battleBalance != null ? battleBalance.TimerRouteLateralAmplitude : 60f,
        battleBalance != null ? battleBalance.TimerRouteForwardFraction : 0.9f,
        battleBalance != null ? battleBalance.TimerRouteOscillations : 1.5f,
        spawnSettings,
        seed,
        out var route))
        return;

      var points = route.PathPoints;
      for (var i = 1; i < points.Count; i++)
      {
        var isGoal = i == points.Count - 1;
        Handles.color = isGoal
          ? Color.white
          : RouteColor(RouteStage(route.NormalizeProgress(route.GetPathProgress(i))));
        var start = points[i - 1] + Vector3.up * 0.35f;
        var end = points[i] + Vector3.up * 0.35f;
        if (isGoal)
          Handles.DrawDottedLine(start, end, 5f);
        else
          Handles.DrawAAPolyLine(4f, start, end);
      }

      var routeOrigin = route.Origin + Vector3.up * 0.35f;
      Handles.color = Color.white;
      Handles.SphereHandleCap(0, routeOrigin, Quaternion.identity, 1.1f, EventType.Repaint);
      if (showTimerRouteLabels)
        Handles.Label(routeOrigin + Vector3.up * 1.25f, "Player");

      var checkpoints = route.CheckpointPoints;
      for (var i = 0; i < checkpoints.Count; i++)
      {
        var point = checkpoints[i] + Vector3.up * 0.35f;
        Handles.color = RouteColor(RouteStage(route.NormalizeProgress(route.GetCheckpointProgress(i))));
        Handles.SphereHandleCap(0, point, Quaternion.identity, 1.1f, EventType.Repaint);

        if (!showTimerRouteLabels)
          continue;

        var label = i == 0
          ? $"Initial Timer  {route.GetSegmentLength(0):0} wu"
          : $"Timer {i} (T{RouteStage(route.NormalizeProgress(route.GetCheckpointProgress(i)))})  {route.GetSegmentLength(i):0} wu";
        Handles.Label(point + Vector3.up * 1.25f, label);
      }

      var goal = route.Goal + Vector3.up * 0.35f;
      Handles.color = Color.white;
      Handles.SphereHandleCap(0, goal, Quaternion.identity, 1.5f, EventType.Repaint);
      if (showTimerRouteLabels)
        Handles.Label(goal + Vector3.up * 1.25f, "Goal");
#endif
    }

#if UNITY_EDITOR
    private static Color RouteColor(int routeStage)
    {
      if (routeStage == 1)
        return new Color(0.2f, 0.85f, 1f, 0.95f);

      if (routeStage == 2)
        return new Color(1f, 0.8f, 0.15f, 0.95f);

      return new Color(1f, 0.3f, 0.25f, 0.95f);
    }

    private static int RouteStage(float normalizedProgress) =>
      normalizedProgress < HouseSet.RouteT1End ? 1
        : normalizedProgress < HouseSet.RouteT2End ? 2
        : 3;
#endif

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

    private static void IncludeCells(
      IReadOnlyList<Vector2Int> cells,
      ref bool hasCells,
      ref Vector2Int minCell,
      ref Vector2Int maxCell)
    {
      for (var i = 0; i < cells.Count; i++)
      {
        var cell = cells[i];
        hasCells = true;
        minCell = Vector2Int.Min(minCell, cell);
        maxCell = Vector2Int.Max(maxCell, cell);
      }
    }

    private static void IncludeRoadCells(
      IReadOnlyList<RoadCellData> cells,
      ref bool hasCells,
      ref Vector2Int minCell,
      ref Vector2Int maxCell)
    {
      for (var i = 0; i < cells.Count; i++)
      {
        var cell = cells[i].cell;
        hasCells = true;
        minCell = Vector2Int.Min(minCell, cell);
        maxCell = Vector2Int.Max(maxCell, cell);
      }
    }

    public void Fill()
    {
      Clear();

      if (mapData == null)
        return;

      var container = CreateContainer();
      var cellSize = mapData.CellSize;
      var player = GameObject.FindGameObjectWithTag("Player");
      var originPosition = player != null ? player.transform.position : transform.position;
      var originCell = new Vector2(originPosition.x / cellSize, originPosition.z / cellSize);
      var battleBalance = Resources.Load<Balance.BattleBalanceConfig>("BattleBalanceConfig");
      var spawnSettings = Resources.Load<SpecialSpawnSettings>("SpecialSpawnSettings");
      TimerRoute.TryCreateForBattle(
        originPosition,
        battleBalance != null ? battleBalance.TimerRouteLateralAmplitude : 60f,
        battleBalance != null ? battleBalance.TimerRouteForwardFraction : 0.9f,
        battleBalance != null ? battleBalance.TimerRouteOscillations : 1.5f,
        spawnSettings,
        seed,
        out var timerRoute);

      if (houseSet != null)
        foreach (var placement in MapFiller.Fill(mapData, houseSet, originCell, seed, timerRoute))
        {
          var size = MapFiller.RotatedSize(placement.House.size, placement.RotationDegrees);
          var position = new Vector3(
            placement.Cell.x * cellSize + size.x * cellSize * 0.5f,
            0f,
            placement.Cell.y * cellSize + size.y * cellSize * 0.5f);
          var rotation = Quaternion.Euler(0f, placement.RotationDegrees, 0f);

          SpawnInstance(placement.House.prefab, placement.House.name, position, rotation, container);
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

          var rotation = Quaternion.Euler(0f, placement.RotationDegrees, 0f);
          SpawnInstance(placement.Piece.prefab, placement.Piece.name, position, rotation, container);
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
