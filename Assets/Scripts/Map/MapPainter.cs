using UnityEngine;

namespace Map
{
  public class MapPainter : MonoBehaviour
  {
    [SerializeField] private MapData mapData;
    [SerializeField] private int gridExtent = 10;
    [HideInInspector] public bool isEditing;

    public MapData MapData => mapData;

    private void OnDrawGizmos()
    {
      if (mapData == null)
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
    }

    private void OnDrawGizmosSelected()
    {
      if (mapData == null)
        return;

      var cellSize = mapData.CellSize;
      var extent = gridExtent * cellSize;

      Gizmos.color = new Color(1f, 1f, 1f, 0.3f);

      for (var i = -gridExtent; i <= gridExtent; i++)
      {
        var offset = i * cellSize;
        Gizmos.DrawLine(new Vector3(offset, 0f, -extent), new Vector3(offset, 0f, extent));
        Gizmos.DrawLine(new Vector3(-extent, 0f, offset), new Vector3(extent, 0f, offset));
      }
    }
  }
}
