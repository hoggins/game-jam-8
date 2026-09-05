using App;
using UnityEngine;
using VContainer;

namespace Movement
{
  [DisallowMultipleComponent]
  public sealed class FlowMapVisualizer : MonoBehaviour
  {
    [SerializeField] private bool _drawBlockedCells = true;
    [SerializeField] private bool _drawUnreachableCells;
    [SerializeField, Min(1)] private int _maxDirectionArrows = 4096;
    [SerializeField, Min(0f)] private float _heightOffset = 0.1f;

    [Inject] private MovementUpdater _movementUpdater;

    private void Awake() =>
      this.AsInjected();

    private void OnDrawGizmos()
    {
      return;
      if (_movementUpdater == null && Application.isPlaying)
        this.AsInjected();

      if (_movementUpdater == null)
        return;

      var flowMap = _movementUpdater.FlowMap;
      if (!flowMap.HasField)
        return;

      var cellCount = flowMap.Width * flowMap.Height;
      var arrowStride = Mathf.Max(
        1,
        Mathf.CeilToInt(Mathf.Sqrt(cellCount / (float)_maxDirectionArrows)));
      var cellSize = flowMap.CellSize;
      var y = transform.position.y + _heightOffset;

      for (var cellY = 0; cellY < flowMap.Height; cellY++)
      for (var cellX = 0; cellX < flowMap.Width; cellX++)
      {
        var center = new Vector3(
          flowMap.Offset.x + (cellX + 0.5f) * cellSize,
          y,
          flowMap.Offset.y + (cellY + 0.5f) * cellSize);

        if (flowMap.IsCellBlocked(cellX, cellY))
        {
          if (_drawBlockedCells)
          {
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.45f);
            Gizmos.DrawCube(center, new Vector3(cellSize * 0.9f, 0.05f, cellSize * 0.9f));
          }

          continue;
        }

        if (!flowMap.IsCellReachable(cellX, cellY))
        {
          if (_drawUnreachableCells)
          {
            Gizmos.color = new Color(0.4f, 0.4f, 0.4f, 0.35f);
            Gizmos.DrawWireCube(center, new Vector3(cellSize * 0.8f, 0f, cellSize * 0.8f));
          }

          continue;
        }

        if (cellX % arrowStride != 0 || cellY % arrowStride != 0)
          continue;

        if (new Vector2Int(cellX, cellY) == flowMap.TargetCell)
        {
          Gizmos.color = Color.yellow;
          Gizmos.DrawSphere(center, cellSize * 0.15f);
          continue;
        }

        DrawDirection(center, flowMap.GetCellDirection(cellX, cellY), cellSize);
      }
    }

    private void DrawDirection(Vector3 center, Vector2 direction, float cellSize)
    {
      if (direction.sqrMagnitude <= 0.0001f)
        return;

      direction.Normalize();
      var direction3D = new Vector3(direction.x, 0f, direction.y);
      var perpendicular = new Vector3(-direction.y, 0f, direction.x);
      var halfLength = cellSize * 0.32f;
      var arrowHeadLength = cellSize * 0.12f;
      var start = center - direction3D * halfLength;
      var end = center + direction3D * halfLength;

      Gizmos.color = Color.cyan;
      Gizmos.DrawLine(start, end);
      Gizmos.DrawLine(end, end - direction3D * arrowHeadLength + perpendicular * arrowHeadLength);
      Gizmos.DrawLine(end, end - direction3D * arrowHeadLength - perpendicular * arrowHeadLength);
    }

    private void OnValidate()
    {
      _maxDirectionArrows = Mathf.Max(1, _maxDirectionArrows);
      _heightOffset = Mathf.Max(0f, _heightOffset);
    }
  }
}
