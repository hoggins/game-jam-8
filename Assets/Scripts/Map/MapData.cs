using System;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
  [Serializable]
  public struct RoadCellData
  {
    public Vector2Int cell;
    public RoadWidth width;
  }

  [CreateAssetMenu(fileName = "MapData", menuName = "Map/Map Data")]
  public class MapData : ScriptableObject
  {
    [SerializeField] private int cellSize = 8;
    [SerializeField] private List<Vector2Int> filledCells = new();
    [SerializeField] private List<RoadCellData> roadCells = new();
    [SerializeField] private List<Vector2Int> sidewalkCells = new();

    public int CellSize => cellSize;
    public IReadOnlyList<Vector2Int> FilledCells => filledCells;
    public IReadOnlyList<RoadCellData> RoadCells => roadCells;
    public IReadOnlyList<Vector2Int> SidewalkCells => sidewalkCells;

    public bool IsFilled(Vector2Int cell) => filledCells.Contains(cell);
    public void SetFilled(Vector2Int cell, bool filled) => SetCell(filledCells, cell, filled);

    public bool IsRoad(Vector2Int cell) => roadCells.FindIndex(data => data.cell == cell) >= 0;

    public RoadWidth GetRoadWidth(Vector2Int cell, RoadWidth fallback = RoadWidth.TwoWay)
    {
      var index = roadCells.FindIndex(data => data.cell == cell);
      return index >= 0 ? roadCells[index].width : fallback;
    }

    public void SetRoad(Vector2Int cell, bool road, RoadWidth width = RoadWidth.TwoWay)
    {
      var index = roadCells.FindIndex(data => data.cell == cell);
      if (road)
      {
        var data = new RoadCellData { cell = cell, width = width };
        if (index >= 0)
          roadCells[index] = data;
        else
          roadCells.Add(data);
      }
      else if (index >= 0)
      {
        roadCells.RemoveAt(index);
      }
    }

    public bool IsSidewalk(Vector2Int cell) => sidewalkCells.Contains(cell);
    public void SetSidewalk(Vector2Int cell, bool sidewalk) => SetCell(sidewalkCells, cell, sidewalk);

    private static void SetCell(List<Vector2Int> cells, Vector2Int cell, bool value)
    {
      if (value)
      {
        if (!cells.Contains(cell))
          cells.Add(cell);
      }
      else
      {
        cells.Remove(cell);
      }
    }
  }
}
