using System.Collections.Generic;
using UnityEngine;

namespace Map
{
  [CreateAssetMenu(fileName = "MapData", menuName = "Map/Map Data")]
  public class MapData : ScriptableObject
  {
    [SerializeField] private int cellSize = 8;
    [SerializeField] private List<Vector2Int> filledCells = new();

    public int CellSize => cellSize;
    public IReadOnlyList<Vector2Int> FilledCells => filledCells;

    public bool IsFilled(Vector2Int cell) => filledCells.Contains(cell);

    public void SetFilled(Vector2Int cell, bool filled)
    {
      if (filled)
      {
        if (!filledCells.Contains(cell))
          filledCells.Add(cell);
      }
      else
      {
        filledCells.Remove(cell);
      }
    }
  }
}
