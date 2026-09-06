using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace Movement
{
  internal sealed class FlowMap
  {
    private const float NoGoZoneIndexCellSize = 16f;

    private static readonly ProfilerMarker RecalculateMarker =
      new("FlowMap.Recalculate");
    private static readonly ProfilerMarker MarkBlockedCellsMarker =
      new("FlowMap.MarkBlockedCells");

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
    private readonly Dictionary<Vector2Int, List<FlowMapNoGoZone>> _noGoZonesByCell = new();
    private readonly HashSet<FlowMapNoGoZone> _candidateNoGoZones = new();

    private Vector2[] _directions = System.Array.Empty<Vector2>();
    private float[] _costs = System.Array.Empty<float>();
    private bool[] _blocked = System.Array.Empty<bool>();
    private Vector2 _offset;
    private Vector2Int _targetCell;
    private float _cellSize;
    private float _radius;
    private float _padding;
    private int _width;
    private int _height;
    private bool _hasField;
    private bool _overflowed;
    private int _noGoZoneRevision = -1;
    private int _indexedNoGoZoneRevision = -1;
    private float _clearance;

    internal bool HasField => _hasField;
    internal Vector2 Offset => _offset;
    internal Vector2Int TargetCell => _targetCell;
    internal float CellSize => _cellSize;
    internal int Width => _width;
    internal int Height => _height;

    public void Update(
      Vector3 targetPosition,
      IReadOnlyList<FlowMapNoGoZone> noGoZones,
      int noGoZoneRevision,
      float clearance,
      float cellSize,
      float radius,
      float padding,
      int targetCellDeviation,
      int maxCellCount)
    {
      cellSize = Mathf.Max(0.01f, cellSize);
      radius = Mathf.Max(0f, radius);
      padding = Mathf.Max(0f, padding);
      clearance = Mathf.Max(0f, clearance);

      var targetCell = PositionToCell(targetPosition, _offset, cellSize);
      var targetMoved = (!_hasField && !_overflowed)
                        || _cellSize != cellSize
                        || _radius != radius
                        || _padding != padding
                        || _clearance != clearance
                        || _noGoZoneRevision != noGoZoneRevision
                        || Mathf.Abs(targetCell.x - _targetCell.x) > targetCellDeviation
                        || Mathf.Abs(targetCell.y - _targetCell.y) > targetCellDeviation;

      if (!targetMoved)
        return;

      Recalculate(
        targetPosition,
        noGoZones,
        noGoZoneRevision,
        clearance,
        cellSize,
        radius,
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

    /// Cheap array lookup used to gate the expensive Physics.Overlap/SphereCast wall-collision
    /// queries: those queries only ever hit the box colliders that mark cells blocked here, so an
    /// agent whose whole move fits inside unblocked cells cannot touch a wall and can skip physics
    /// entirely. Returns true (assume blocked, so the caller stays on the safe physics path) when
    /// the field isn't built yet or the checked area falls outside the current field bounds.
    internal bool HasBlockedCellNear(Vector3 position, float radius)
    {
      if (!_hasField)
        return true;

      var minCell = PositionToCell(position - new Vector3(radius, 0f, radius), _offset, _cellSize);
      var maxCell = PositionToCell(position + new Vector3(radius, 0f, radius), _offset, _cellSize);

      // Agents outside the local field use direct-target fallback. Keep them on the safe physics
      // path until the field is rebuilt around the player; otherwise they could pass through a wall.
      if (minCell.x < 0 || minCell.y < 0 || maxCell.x >= _width || maxCell.y >= _height)
        return true;

      for (var y = minCell.y; y <= maxCell.y; y++)
      for (var x = minCell.x; x <= maxCell.x; x++)
        if (TryGetIndex(new Vector2Int(x, y), out var index) && _blocked[index])
          return true;

      return false;
    }

    internal bool IsWalkable(Vector3 position)
    {
      if (!_hasField)
        return false;

      var cell = PositionToCell(position, _offset, _cellSize);
      if (!TryGetIndex(cell, out var index))
        return false;

      return !_blocked[index] && _costs[index] < float.PositiveInfinity;
    }

    internal bool IsInside(Vector3 position)
    {
      if (!_hasField)
        return false;

      var cell = PositionToCell(position, _offset, _cellSize);
      return TryGetIndex(cell, out _);
    }

    private void Recalculate(
      Vector3 targetPosition,
      IReadOnlyList<FlowMapNoGoZone> noGoZones,
      int noGoZoneRevision,
      float clearance,
      float cellSize,
      float radius,
      float padding,
      int maxCellCount)
    {
      RecalculateMarker.Begin();
      try
      {
        var center = new Vector2(targetPosition.x, targetPosition.z);
        var halfSize = radius + padding;
        var extent = Vector2.one * halfSize;
        var min = center - extent;
        var max = center + extent;

        // Keep the field local to the target. Agents outside this window use direct-target fallback
        // instead of expanding the grid and making every spawn or teleport trigger a rebuild.
        _cellSize = cellSize;
        _radius = radius;
        _padding = padding;
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

        EnsureNoGoZoneIndex(noGoZones, noGoZoneRevision);
        MarkBlockedCells();

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
      finally
      {
        RecalculateMarker.End();
      }
    }

    private void MarkBlockedCells()
    {
      MarkBlockedCellsMarker.Begin();
      try
      {
        var fieldMinX = _offset.x - _clearance;
        var fieldMinY = _offset.y - _clearance;
        var fieldMaxX = _offset.x + _width * _cellSize + _clearance;
        var fieldMaxY = _offset.y + _height * _cellSize + _clearance;

        _candidateNoGoZones.Clear();
        var minIndexCell = GetNoGoZoneIndexCell(new Vector2(fieldMinX, fieldMinY));
        var maxIndexCell = GetNoGoZoneIndexCell(new Vector2(fieldMaxX, fieldMaxY));
        for (var y = minIndexCell.y; y <= maxIndexCell.y; y++)
        for (var x = minIndexCell.x; x <= maxIndexCell.x; x++)
        {
          if (!_noGoZonesByCell.TryGetValue(new Vector2Int(x, y), out var zones))
            continue;

          for (var i = 0; i < zones.Count; i++)
            _candidateNoGoZones.Add(zones[i]);
        }

        foreach (var zone in _candidateNoGoZones)
        {
          var collider = zone != null ? zone.Collider : null;
          if (zone == null
              || zone.IgnoreMobs
              || collider == null
              || !collider.enabled
              || !collider.gameObject.activeInHierarchy)
            continue;

          var bounds = zone.WorldBounds;
          if (bounds.max.x < fieldMinX
              || bounds.min.x > fieldMaxX
              || bounds.max.z < fieldMinY
              || bounds.min.z > fieldMaxY)
            continue;

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
      finally
      {
        MarkBlockedCellsMarker.End();
      }
    }

    private void EnsureNoGoZoneIndex(
      IReadOnlyList<FlowMapNoGoZone> noGoZones,
      int noGoZoneRevision)
    {
      if (_indexedNoGoZoneRevision == noGoZoneRevision)
        return;

      _noGoZonesByCell.Clear();
      for (var i = 0; i < noGoZones.Count; i++)
      {
        var zone = noGoZones[i];
        var collider = zone != null ? zone.Collider : null;
        if (zone == null
            || zone.IgnoreMobs
            || collider == null
            || !collider.enabled
            || !collider.gameObject.activeInHierarchy)
          continue;

        var bounds = zone.WorldBounds;
        var minCell = GetNoGoZoneIndexCell(new Vector2(bounds.min.x, bounds.min.z));
        var maxCell = GetNoGoZoneIndexCell(new Vector2(bounds.max.x, bounds.max.z));
        for (var y = minCell.y; y <= maxCell.y; y++)
        for (var x = minCell.x; x <= maxCell.x; x++)
        {
          var cell = new Vector2Int(x, y);
          if (!_noGoZonesByCell.TryGetValue(cell, out var zones))
          {
            zones = new List<FlowMapNoGoZone>(4);
            _noGoZonesByCell.Add(cell, zones);
          }

          zones.Add(zone);
        }
      }

      _indexedNoGoZoneRevision = noGoZoneRevision;
    }

    private static Vector2Int GetNoGoZoneIndexCell(Vector2 position) =>
      new(
        Mathf.FloorToInt(position.x / NoGoZoneIndexCellSize),
        Mathf.FloorToInt(position.y / NoGoZoneIndexCellSize));

    private bool CutsBlockedCorner(Vector2Int cell, Vector2Int offset)
    {
      if (offset.x == 0 || offset.y == 0)
        return false;

      return IsBlocked(cell + new Vector2Int(offset.x, 0))
             || IsBlocked(cell + new Vector2Int(0, offset.y));
    }

    private bool IsBlocked(Vector2Int cell) =>
      TryGetIndex(cell, out var index) && _blocked[index];

    private void EnsureCapacity(int count)
    {
      if (_directions.Length >= count)
        return;

      var capacity = Mathf.NextPowerOfTwo(count);
      _directions = new Vector2[capacity];
      _costs = new float[capacity];
      _blocked = new bool[capacity];
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
