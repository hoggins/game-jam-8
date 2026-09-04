using System.Collections.Generic;
using UnityEngine;

namespace Movement
{
  internal sealed class FlowMap
  {
    private static readonly Vector2Int[] NeighborOffsets =
    {
      new(0, -1),
      new(-1, 0),
      new(0, 1),
      new(1, 0),
      new(-1, -1),
      new(1, -1),
      new(-1, 1),
      new(1, 1),
    };

    private readonly MinHeap _candidates = new();

    private Vector2[] _directions = System.Array.Empty<Vector2>();
    private float[] _costs = System.Array.Empty<float>();
    private bool[] _blocked = System.Array.Empty<bool>();
    private Vector2 _offset;
    private Vector2Int _targetCell;
    private float _cellSize;
    private int _width;
    private int _height;
    private bool _hasField;
    private bool _overflowed;
    private int _noGoZoneRevision = -1;
    private float _clearance;

    internal bool HasField => _hasField;
    internal Vector2 Offset => _offset;
    internal Vector2Int TargetCell => _targetCell;
    internal float CellSize => _cellSize;
    internal int Width => _width;
    internal int Height => _height;

    public void Update(
      Vector3 targetPosition,
      IReadOnlyList<MovementAgent> agents,
      IReadOnlyList<FlowMapNoGoZone> noGoZones,
      int noGoZoneRevision,
      float clearance,
      float cellSize,
      float padding,
      int targetCellDeviation,
      int maxCellCount)
    {
      cellSize = Mathf.Max(0.01f, cellSize);
      padding = Mathf.Max(0f, padding);
      clearance = Mathf.Max(0f, clearance);

      var targetCell = PositionToCell(targetPosition, _offset, cellSize);
      var targetMoved = (!_hasField && !_overflowed)
                        || _cellSize != cellSize
                        || _clearance != clearance
                        || _noGoZoneRevision != noGoZoneRevision
                        || Mathf.Abs(targetCell.x - _targetCell.x) > targetCellDeviation
                        || Mathf.Abs(targetCell.y - _targetCell.y) > targetCellDeviation;

      if (!targetMoved && ContainsAllActiveAgents(agents))
        return;

      Recalculate(
        targetPosition,
        agents,
        noGoZones,
        noGoZoneRevision,
        clearance,
        cellSize,
        padding,
        maxCellCount);
    }

    public Vector3 GetDirection(Vector3 position, Vector3 fallbackTarget)
    {
      var direct = fallbackTarget - position;
      direct.y = 0f;
      direct = direct.sqrMagnitude > 0.0001f ? direct.normalized : Vector3.zero;

      if (!_hasField)
        return direct;

      var cell = PositionToCell(position, _offset, _cellSize);
      if (!TryGetIndex(cell, out var index))
        return direct;

      if (cell == _targetCell)
        return direct;

      if (_blocked[index])
        return GetEscapeDirection(position, cell);

      var direction = _directions[index];
      return direction.sqrMagnitude > 0.0001f
        ? new Vector3(direction.x, 0f, direction.y).normalized
        : Vector3.zero;
    }

    private Vector3 GetEscapeDirection(Vector3 position, Vector2Int blockedCell)
    {
      for (var radius = 1; radius <= 4; radius++)
      {
        var bestDistanceSq = float.PositiveInfinity;
        var bestCost = float.PositiveInfinity;
        var bestDirection = Vector3.zero;

        for (var y = -radius; y <= radius; y++)
        for (var x = -radius; x <= radius; x++)
        {
          if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
            continue;

          var candidateCell = blockedCell + new Vector2Int(x, y);
          if (!TryGetIndex(candidateCell, out var candidateIndex)
              || _blocked[candidateIndex]
              || _costs[candidateIndex] == float.PositiveInfinity)
            continue;

          var candidatePosition = new Vector3(
            _offset.x + (candidateCell.x + 0.5f) * _cellSize,
            position.y,
            _offset.y + (candidateCell.y + 0.5f) * _cellSize);
          var direction = candidatePosition - position;
          var distanceSq = direction.sqrMagnitude;
          var cost = _costs[candidateIndex];
          if (distanceSq > bestDistanceSq
              || (Mathf.Approximately(distanceSq, bestDistanceSq) && cost >= bestCost))
            continue;

          bestDistanceSq = distanceSq;
          bestCost = cost;
          bestDirection = direction;
        }

        if (bestDirection.sqrMagnitude > 0.0001f)
          return bestDirection.normalized;
      }

      return Vector3.zero;
    }

    internal Vector2 GetCellDirection(int x, int y) =>
      _directions[x + y * _width];

    internal bool IsCellBlocked(int x, int y) =>
      _blocked[x + y * _width];

    internal bool IsCellReachable(int x, int y) =>
      _costs[x + y * _width] < float.PositiveInfinity;

    internal bool IsWalkable(Vector3 position)
    {
      if (!_hasField)
        return false;

      var cell = PositionToCell(position, _offset, _cellSize);
      if (!TryGetIndex(cell, out var index))
        return false;

      return !_blocked[index] && _costs[index] < float.PositiveInfinity;
    }

    private void Recalculate(
      Vector3 targetPosition,
      IReadOnlyList<MovementAgent> agents,
      IReadOnlyList<FlowMapNoGoZone> noGoZones,
      int noGoZoneRevision,
      float clearance,
      float cellSize,
      float padding,
      int maxCellCount)
    {
      var min = new Vector2(targetPosition.x, targetPosition.z);
      var max = min;

      for (var i = 0; i < agents.Count; i++)
      {
        var agent = agents[i];
        if (agent == null || !agent.isActiveAndEnabled)
          continue;

        var point = new Vector2(agent.Position.x, agent.Position.z);
        min = Vector2.Min(min, point);
        max = Vector2.Max(max, point);
      }

      min -= Vector2.one * padding;
      max += Vector2.one * padding;

      _cellSize = cellSize;
      _clearance = clearance;
      _noGoZoneRevision = noGoZoneRevision;
      _offset = new Vector2(
        Mathf.Floor(min.x / cellSize) * cellSize,
        Mathf.Floor(min.y / cellSize) * cellSize);
      _width = Mathf.Max(1, Mathf.CeilToInt((max.x - _offset.x) / cellSize) + 1);
      _height = Mathf.Max(1, Mathf.CeilToInt((max.y - _offset.y) / cellSize) + 1);
      _targetCell = PositionToCell(targetPosition, _offset, cellSize);

      var cellCount = (long)_width * _height;
      if (cellCount > Mathf.Max(1, maxCellCount))
      {
        _hasField = false;
        _overflowed = true;
        return;
      }

      _overflowed = false;
      EnsureCapacity((int)cellCount);
      for (var i = 0; i < cellCount; i++)
      {
        _directions[i] = Vector2.zero;
        _costs[i] = float.PositiveInfinity;
        _blocked[i] = false;
      }

      MarkBlockedCells(noGoZones);

      if (!TryGetIndex(_targetCell, out var targetIndex))
      {
        _hasField = false;
        return;
      }

      _blocked[targetIndex] = false;
      _candidates.Clear();
      _costs[targetIndex] = 0f;
      _candidates.Push(0f, targetIndex);

      while (_candidates.Count > 0)
      {
        _candidates.Pop(out var currentCost, out var currentIndex);
        if (currentCost > _costs[currentIndex])
          continue;

        var currentCell = IndexToCell(currentIndex);
        for (var i = 0; i < NeighborOffsets.Length; i++)
        {
          var neighborCell = currentCell + NeighborOffsets[i];
          if (!TryGetIndex(neighborCell, out var neighborIndex))
            continue;

          if (_blocked[neighborIndex] || CutsBlockedCorner(currentCell, NeighborOffsets[i]))
            continue;

          var nextCost = currentCost + (i < 4 ? 1f : 1.41421356f);
          if (nextCost >= _costs[neighborIndex])
            continue;

          _costs[neighborIndex] = nextCost;
          _directions[neighborIndex] = -NeighborOffsets[i];
          _candidates.Push(nextCost, neighborIndex);
        }
      }

      _hasField = true;
    }

    private void MarkBlockedCells(IReadOnlyList<FlowMapNoGoZone> noGoZones)
    {
      for (var i = 0; i < noGoZones.Count; i++)
      {
        var zone = noGoZones[i];
        var collider = zone != null ? zone.Collider : null;
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
          continue;

        var bounds = collider.bounds;
        var minCell = PositionToCell(
          bounds.min - new Vector3(_clearance, 0f, _clearance),
          _offset,
          _cellSize);
        var maxCell = PositionToCell(
          bounds.max + new Vector3(_clearance, 0f, _clearance),
          _offset,
          _cellSize);
        var minX = Mathf.Max(0, minCell.x);
        var minY = Mathf.Max(0, minCell.y);
        var maxX = Mathf.Min(_width - 1, maxCell.x);
        var maxY = Mathf.Min(_height - 1, maxCell.y);

        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
          var cellCenter = new Vector2(
            _offset.x + (x + 0.5f) * _cellSize,
            _offset.y + (y + 0.5f) * _cellSize);
          if (zone.OverlapsCircle(cellCenter, _clearance))
            _blocked[x + y * _width] = true;
        }
      }
    }

    private bool CutsBlockedCorner(Vector2Int cell, Vector2Int offset)
    {
      if (offset.x == 0 || offset.y == 0)
        return false;

      return IsBlocked(cell + new Vector2Int(offset.x, 0))
             || IsBlocked(cell + new Vector2Int(0, offset.y));
    }

    private bool IsBlocked(Vector2Int cell) =>
      TryGetIndex(cell, out var index) && _blocked[index];

    private bool ContainsAllActiveAgents(IReadOnlyList<MovementAgent> agents)
    {
      for (var i = 0; i < agents.Count; i++)
      {
        var agent = agents[i];
        if (agent == null || !agent.isActiveAndEnabled)
          continue;

        if (!TryGetIndex(PositionToCell(agent.Position, _offset, _cellSize), out _))
          return false;
      }

      return true;
    }

    private void EnsureCapacity(int count)
    {
      if (_directions.Length == count)
        return;

      _directions = new Vector2[count];
      _costs = new float[count];
      _blocked = new bool[count];
    }

    private bool TryGetIndex(Vector2Int cell, out int index)
    {
      if (cell.x < 0 || cell.y < 0 || cell.x >= _width || cell.y >= _height)
      {
        index = -1;
        return false;
      }

      index = cell.x + cell.y * _width;
      return true;
    }

    private Vector2Int IndexToCell(int index) =>
      new(index % _width, index / _width);

    private static Vector2Int PositionToCell(Vector3 position, Vector2 offset, float cellSize) =>
      new(
        Mathf.FloorToInt((position.x - offset.x) / cellSize),
        Mathf.FloorToInt((position.z - offset.y) / cellSize));

    private sealed class MinHeap
    {
      private readonly List<Entry> _entries = new(256);

      public int Count => _entries.Count;

      public void Clear() =>
        _entries.Clear();

      public void Push(float cost, int index)
      {
        _entries.Add(new Entry(cost, index));
        var child = _entries.Count - 1;

        while (child > 0)
        {
          var parent = (child - 1) / 2;
          if (_entries[parent].Cost <= cost)
            break;

          _entries[child] = _entries[parent];
          child = parent;
        }

        _entries[child] = new Entry(cost, index);
      }

      public void Pop(out float cost, out int index)
      {
        var root = _entries[0];
        var last = _entries[^1];
        _entries.RemoveAt(_entries.Count - 1);

        if (_entries.Count > 0)
        {
          var parent = 0;
          while (true)
          {
            var left = parent * 2 + 1;
            if (left >= _entries.Count)
              break;

            var right = left + 1;
            var child = right < _entries.Count && _entries[right].Cost < _entries[left].Cost
              ? right
              : left;

            if (_entries[child].Cost >= last.Cost)
              break;

            _entries[parent] = _entries[child];
            parent = child;
          }

          _entries[parent] = last;
        }

        cost = root.Cost;
        index = root.Index;
      }

      private readonly struct Entry
      {
        public readonly float Cost;
        public readonly int Index;

        public Entry(float cost, int index)
        {
          Cost = cost;
          Index = index;
        }
      }
    }
  }
}
