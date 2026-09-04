using System.Collections.Generic;
using UnityEngine;

namespace Movement
{
  internal sealed class SpatialMap
  {
    private readonly Dictionary<Vector2Int, List<MovementAgent>> _cells = new();
    private readonly HashSet<MovementAgent> _seen = new();

    private float _cellSize = 1f;

    public void Rebuild(IReadOnlyList<MovementAgent> agents, float cellSize)
    {
      _cellSize = Mathf.Max(0.01f, cellSize);

      foreach (var entries in _cells.Values)
        entries.Clear();

      for (var i = 0; i < agents.Count; i++)
      {
        var agent = agents[i];
        if (agent == null || !agent.isActiveAndEnabled || agent.Controller == null)
          continue;

        var position = ToPlane(agent.Position);
        var radius = agent.Controller.Radius;
        var min = GetCell(position - Vector2.one * radius);
        var max = GetCell(position + Vector2.one * radius);

        for (var x = min.x; x <= max.x; x++)
        for (var y = min.y; y <= max.y; y++)
        {
          var cell = new Vector2Int(x, y);
          if (!_cells.TryGetValue(cell, out var entries))
          {
            entries = new List<MovementAgent>(4);
            _cells.Add(cell, entries);
          }

          entries.Add(agent);
        }
      }
    }

    public void QueryCircle(
      MovementAgent self,
      float radius,
      MovementLayer collisionMask,
      List<MovementAgent> result)
    {
      QueryCircle(self, self.Position, radius, collisionMask, result);
    }

    public void QueryCircle(
      Vector3 centerPosition,
      float radius,
      MovementLayer collisionMask,
      List<MovementAgent> result)
    {
      QueryCircle(null, centerPosition, radius, collisionMask, result);
    }

    private void QueryCircle(
      MovementAgent self,
      Vector3 centerPosition,
      float radius,
      MovementLayer collisionMask,
      List<MovementAgent> result)
    {
      result.Clear();
      _seen.Clear();

      var center = ToPlane(centerPosition);
      var min = GetCell(center - Vector2.one * radius);
      var max = GetCell(center + Vector2.one * radius);

      for (var x = min.x; x <= max.x; x++)
      for (var y = min.y; y <= max.y; y++)
      {
        if (!_cells.TryGetValue(new Vector2Int(x, y), out var entries))
          continue;

        for (var i = 0; i < entries.Count; i++)
        {
          var other = entries[i];
          if (other == self || !_seen.Add(other) || other.Controller == null)
            continue;

          if ((collisionMask & other.Controller.Layer) == 0)
            continue;

          var combinedRadius = radius + other.Controller.Radius;
          if ((ToPlane(other.Position) - center).sqrMagnitude > combinedRadius * combinedRadius)
            continue;

          result.Add(other);
        }
      }
    }

    private Vector2Int GetCell(Vector2 position) =>
      new(
        Mathf.FloorToInt(position.x / _cellSize),
        Mathf.FloorToInt(position.y / _cellSize));

    private static Vector2 ToPlane(Vector3 position) =>
      new(position.x, position.z);
  }
}
