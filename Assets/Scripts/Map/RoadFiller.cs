using System.Collections.Generic;
using UnityEngine;

namespace Map
{
  public readonly struct RoadPlacement
  {
    public readonly RoadPiece Piece;
    public readonly Vector2Int Cell;
    public readonly Vector2 CellOffset;
    public readonly float RotationDegrees;

    public RoadPlacement(
      RoadPiece piece,
      Vector2Int cell,
      float rotationDegrees,
      Vector2 cellOffset = default)
    {
      Piece = piece;
      Cell = cell;
      CellOffset = cellOffset;
      RotationDegrees = rotationDegrees;
    }
  }

  public static class RoadFiller
  {
    // Ground/S pieces cover one source cell. Ground/L pieces cover a two-cell-wide
    // road footprint, so the kind is selected from the footprint rather than randomly
    // for every rasterized source cell.
    private const RoadKind NarrowRoadKind = RoadKind.NoLine;
    private const RoadKind WideRoadKind = RoadKind.Line;

    private static readonly Vector2Int[] Offsets =
    {
      new(0, 1),
      new(1, 0),
      new(0, -1),
      new(-1, 0),
    };

    private static readonly RoadConnections[] Directions =
    {
      RoadConnections.North,
      RoadConnections.East,
      RoadConnections.South,
      RoadConnections.West,
    };

    private readonly struct RoadInfo
    {
      public readonly RoadCellData Data;
      public readonly RoadConnections Connections;
      public readonly int Degree;

      public RoadInfo(RoadCellData data, RoadConnections connections)
      {
        Data = data;
        Connections = connections;
        Degree = CountConnections(connections);
      }
    }

    private sealed class RoadNode
    {
      public readonly Vector2Int Anchor;
      public readonly Vector2 CellOffset;
      public readonly RoadWidth Width;
      public readonly bool Wide;
      public readonly List<Vector2Int> Cells;
      public RoadConnections Connections;

      public RoadNode(
        Vector2Int anchor,
        Vector2 cellOffset,
        RoadWidth width,
        bool wide,
        List<Vector2Int> cells)
      {
        Anchor = anchor;
        CellOffset = cellOffset;
        Width = width;
        Wide = wide;
        Cells = cells;
      }

      public int Degree => CountConnections(Connections);
    }

    private readonly struct WideJunctionCandidate
    {
      public readonly Vector2Int Anchor;
      public readonly RoadWidth Width;
      public readonly RoadConnections Connections;
      public readonly int FourCellCount;

      public WideJunctionCandidate(
        Vector2Int anchor,
        RoadWidth width,
        RoadConnections connections,
        int fourCellCount)
      {
        Anchor = anchor;
        Width = width;
        Connections = connections;
        FourCellCount = fourCellCount;
      }
    }

    private readonly struct WidePairCandidate
    {
      public readonly Vector2Int Anchor;
      public readonly Vector2Int Other;
      public readonly Vector2 CellOffset;
      public readonly RoadWidth Width;
      public readonly bool AcrossX;

      public WidePairCandidate(
        Vector2Int anchor,
        Vector2Int other,
        Vector2 cellOffset,
        RoadWidth width,
        bool acrossX)
      {
        Anchor = anchor;
        Other = other;
        CellOffset = cellOffset;
        Width = width;
        AcrossX = acrossX;
      }
    }

    public static List<RoadPlacement> Fill(MapData mapData, RoadSet roadSet, int seed = 0)
    {
      _ = seed;
      var placements = new List<RoadPlacement>();
      if (mapData == null || roadSet == null)
        return placements;

      var roadCells = new Dictionary<Vector2Int, RoadCellData>();
      foreach (var road in mapData.RoadCells)
      {
        // MapData.SetRoad keeps cells unique. Keep the first serialized entry as a safe
        // fallback for hand-edited assets instead of producing duplicate placements.
        if (!roadCells.ContainsKey(road.cell))
          roadCells.Add(road.cell, road);
      }

      if (roadCells.Count == 0)
        return placements;

      var cells = new List<Vector2Int>(roadCells.Keys);
      cells.Sort(CompareCells);

      var infos = new Dictionary<Vector2Int, RoadInfo>(cells.Count);
      foreach (var cell in cells)
        infos.Add(cell, new RoadInfo(roadCells[cell], GetConnections(cell, roadCells)));

      var nodes = BuildNodes(cells, roadCells, infos);
      nodes.Sort(CompareNodes);
      var neighbours = BuildNodeConnections(nodes, roadCells);
      var warnings = new HashSet<string>();
      var segmentKinds = new Dictionary<int, RoadKind>();
      var visited = new bool[nodes.Count];

      // Logical segments are collected after wide source cells have been collapsed. This
      // keeps the selected kind through degree-2 cells and corners without treating the two
      // raster cells making up a wide straight as separate junctions.
      for (var i = 0; i < nodes.Count; i++)
      {
        if (visited[i] || nodes[i].Degree >= 3)
          continue;

        var segment = CollectSegment(i, nodes, neighbours, visited);
        var kind = ChooseSegmentKind(roadSet, segment, nodes, warnings);
        foreach (var segmentNode in segment)
          segmentKinds[segmentNode] = kind;
      }

      for (var i = 0; i < nodes.Count; i++)
      {
        var node = nodes[i];
        var kind = node.Wide
          ? WideRoadKind
          : NarrowRoadKind;

        if (!node.Wide && node.Degree < 3 && segmentKinds.TryGetValue(i, out var selectedKind))
          kind = selectedKind;

        // A wide node must use a wide prefab and a narrow node must use a one-cell prefab.
        // There is deliberately no cross-footprint fallback.
        if (!TryFindPiece(roadSet, node.Connections, node.Width, kind, out var piece,
              out var rotationDegrees))
        {
          WarnMissing(warnings, node.Anchor, node.Connections, node.Width, kind, node.Degree >= 3);
          continue;
        }

        placements.Add(new RoadPlacement(piece, node.Anchor, rotationDegrees, node.CellOffset));
      }

      return placements;
    }

    private static List<RoadNode> BuildNodes(
      IReadOnlyList<Vector2Int> cells,
      IReadOnlyDictionary<Vector2Int, RoadCellData> roadCells,
      IReadOnlyDictionary<Vector2Int, RoadInfo> infos)
    {
      var nodes = new List<RoadNode>();
      var consumed = new HashSet<Vector2Int>();
      var junctionCandidates = FindWideJunctionCandidates(cells, roadCells, infos);
      junctionCandidates.Sort(CompareJunctionCandidates);

      foreach (var candidate in junctionCandidates)
      {
        var footprint = GetBlockCells(candidate.Anchor);
        if (ContainsAny(footprint, consumed))
          continue;

        foreach (var cell in footprint)
          consumed.Add(cell);

        nodes.Add(new RoadNode(
          candidate.Anchor,
          new Vector2(0.5f, 0.5f),
          candidate.Width,
          true,
          new List<Vector2Int>(footprint)));
      }

      var pairCandidates = FindWidePairCandidates(cells, roadCells, infos, consumed);
      pairCandidates.Sort(ComparePairCandidates);
      foreach (var candidate in pairCandidates)
      {
        if (consumed.Contains(candidate.Anchor) || consumed.Contains(candidate.Other))
          continue;

        consumed.Add(candidate.Anchor);
        consumed.Add(candidate.Other);
        nodes.Add(new RoadNode(
          candidate.Anchor,
          candidate.CellOffset,
          candidate.Width,
          true,
          new List<Vector2Int> { candidate.Anchor, candidate.Other }));
      }

      foreach (var cell in cells)
      {
        if (consumed.Contains(cell))
          continue;

        nodes.Add(new RoadNode(
          cell,
          Vector2.zero,
          infos[cell].Data.width,
          false,
          new List<Vector2Int> { cell }));
      }

      return nodes;
    }

    private static List<HashSet<int>> BuildNodeConnections(
      IReadOnlyList<RoadNode> nodes,
      IReadOnlyDictionary<Vector2Int, RoadCellData> roadCells)
    {
      var ownerByCell = new Dictionary<Vector2Int, int>();
      for (var i = 0; i < nodes.Count; i++)
        foreach (var cell in nodes[i].Cells)
          ownerByCell[cell] = i;

      var neighbours = new List<HashSet<int>>(nodes.Count);
      for (var i = 0; i < nodes.Count; i++)
        neighbours.Add(new HashSet<int>());

      for (var i = 0; i < nodes.Count; i++)
      {
        var node = nodes[i];
        node.Connections = RoadConnections.None;

        foreach (var cell in node.Cells)
          for (var directionIndex = 0; directionIndex < Offsets.Length; directionIndex++)
          {
            var neighbourCell = cell + Offsets[directionIndex];
            if (!roadCells.ContainsKey(neighbourCell) ||
                !ownerByCell.TryGetValue(neighbourCell, out var neighbour) ||
                neighbour == i)
              continue;

            node.Connections |= Directions[directionIndex];
            neighbours[i].Add(neighbour);
          }
      }

      return neighbours;
    }

    private static List<int> CollectSegment(
      int start,
      IReadOnlyList<RoadNode> nodes,
      IReadOnlyList<HashSet<int>> neighbours,
      bool[] visited)
    {
      var segment = new List<int>();
      var pending = new Stack<int>();
      var startNode = nodes[start];

      visited[start] = true;
      pending.Push(start);

      while (pending.Count > 0)
      {
        var nodeIndex = pending.Pop();
        segment.Add(nodeIndex);

        foreach (var neighbour in neighbours[nodeIndex])
        {
          var neighbourNode = nodes[neighbour];
          if (visited[neighbour] ||
              neighbourNode.Degree >= 3 ||
              neighbourNode.Wide != startNode.Wide ||
              neighbourNode.Width != startNode.Width)
            continue;

          visited[neighbour] = true;
          pending.Push(neighbour);
        }
      }

      segment.Sort((left, right) => CompareNodes(nodes[left], nodes[right]));
      return segment;
    }

    private static RoadKind ChooseSegmentKind(
      RoadSet roadSet,
      IReadOnlyList<int> segment,
      IReadOnlyList<RoadNode> nodes,
      HashSet<string> warnings)
    {
      var kind = nodes[segment[0]].Wide
        ? WideRoadKind
        : NarrowRoadKind;

      foreach (var nodeIndex in segment)
      {
        var node = nodes[nodeIndex];
        if (TryFindPiece(roadSet, node.Connections, node.Width, kind, out _, out _))
          continue;

        WarnMissing(warnings, node.Anchor, node.Connections, node.Width, kind, false);
        break;
      }

      return kind;
    }

    private static List<WideJunctionCandidate> FindWideJunctionCandidates(
      IReadOnlyList<Vector2Int> cells,
      IReadOnlyDictionary<Vector2Int, RoadCellData> roadCells,
      IReadOnlyDictionary<Vector2Int, RoadInfo> infos)
    {
      var candidates = new List<WideJunctionCandidate>();

      foreach (var anchor in cells)
      {
        var footprint = GetBlockCells(anchor);
        var width = infos[anchor].Data.width;
        var fourCellCount = 0;
        var valid = true;

        foreach (var cell in footprint)
        {
          if (!infos.TryGetValue(cell, out var info) ||
              info.Degree < 3 ||
              info.Data.width != width)
          {
            valid = false;
            break;
          }

          if (info.Degree == 4)
            fourCellCount++;
        }

        if (!valid)
          continue;

        var support = new int[Offsets.Length];
        var externalConnections = GetExternalConnections(footprint, roadCells, support);
        var externalDegree = CountConnections(externalConnections);
        if (externalDegree < 2 ||
            externalConnections == (RoadConnections.North | RoadConnections.South) ||
            externalConnections == (RoadConnections.East | RoadConnections.West) ||
            !HasFullSupport(externalConnections, support))
          continue;

        candidates.Add(new WideJunctionCandidate(
          anchor,
          width,
          externalConnections,
          fourCellCount));
      }

      return candidates;
    }

    private static List<WidePairCandidate> FindWidePairCandidates(
      IReadOnlyList<Vector2Int> cells,
      IReadOnlyDictionary<Vector2Int, RoadCellData> roadCells,
      IReadOnlyDictionary<Vector2Int, RoadInfo> infos,
      HashSet<Vector2Int> consumed)
    {
      var candidates = new List<WidePairCandidate>();

      foreach (var anchor in cells)
      {
        if (consumed.Contains(anchor) || infos[anchor].Degree < 3)
          continue;

        AddWidePairCandidate(
          candidates,
          anchor,
          anchor + Vector2Int.right,
          new Vector2(0.5f, 0f),
          RoadConnections.North | RoadConnections.South,
          true,
          roadCells,
          infos,
          consumed);

        AddWidePairCandidate(
          candidates,
          anchor,
          anchor + Vector2Int.up,
          new Vector2(0f, 0.5f),
          RoadConnections.East | RoadConnections.West,
          false,
          roadCells,
          infos,
          consumed);
      }

      return candidates;
    }

    private static void AddWidePairCandidate(
      List<WidePairCandidate> candidates,
      Vector2Int anchor,
      Vector2Int other,
      Vector2 cellOffset,
      RoadConnections expectedConnections,
      bool acrossX,
      IReadOnlyDictionary<Vector2Int, RoadCellData> roadCells,
      IReadOnlyDictionary<Vector2Int, RoadInfo> infos,
      HashSet<Vector2Int> consumed)
    {
      if (consumed.Contains(other) ||
          !infos.TryGetValue(other, out var otherInfo) ||
          otherInfo.Degree < 3 ||
          otherInfo.Data.width != infos[anchor].Data.width)
        return;

      var footprint = new HashSet<Vector2Int> { anchor, other };
      var support = new int[Offsets.Length];
      var externalConnections = GetExternalConnections(footprint, roadCells, support);
      if (externalConnections != expectedConnections || !HasFullSupport(externalConnections, support))
        return;

      candidates.Add(new WidePairCandidate(
        anchor,
        other,
        cellOffset,
        infos[anchor].Data.width,
        acrossX));
    }

    private static HashSet<Vector2Int> GetBlockCells(Vector2Int anchor) => new()
    {
      anchor,
      anchor + Vector2Int.right,
      anchor + Vector2Int.up,
      anchor + Vector2Int.right + Vector2Int.up,
    };

    private static RoadConnections GetExternalConnections(
      HashSet<Vector2Int> footprint,
      IReadOnlyDictionary<Vector2Int, RoadCellData> roadCells,
      int[] support)
    {
      var connections = RoadConnections.None;
      foreach (var cell in footprint)
        for (var directionIndex = 0; directionIndex < Offsets.Length; directionIndex++)
        {
          var neighbour = cell + Offsets[directionIndex];
          if (footprint.Contains(neighbour) || !roadCells.ContainsKey(neighbour))
            continue;

          support[directionIndex]++;
          connections |= Directions[directionIndex];
        }

      return connections;
    }

    private static bool HasFullSupport(RoadConnections connections, IReadOnlyList<int> support)
    {
      for (var i = 0; i < Directions.Length; i++)
        if ((connections & Directions[i]) != 0 && support[i] < 2)
          return false;

      return true;
    }

    private static bool ContainsAny(HashSet<Vector2Int> cells, HashSet<Vector2Int> other)
    {
      foreach (var cell in cells)
        if (other.Contains(cell))
          return true;

      return false;
    }

    private static bool TryFindPiece(
      RoadSet roadSet,
      RoadConnections targetMask,
      RoadWidth targetWidth,
      RoadKind requiredKind,
      out RoadPiece piece,
      out float rotationDegrees)
    {
      if (roadSet?.Pieces != null)
        foreach (var candidate in roadSet.Pieces)
        {
          if (candidate == null || !candidate.enabled || candidate.prefab == null ||
              candidate.kind != requiredKind)
            continue;

          var baseMask = RoadConnections.None;
          var widthMatches = true;
          if (candidate.connections != null)
          {
            foreach (var arm in candidate.connections)
            {
              if (arm == null)
                continue;

              baseMask |= arm.direction;
              if (arm.width != targetWidth)
                widthMatches = false;
            }
          }

          if (!widthMatches)
            continue;

          for (var steps = 0; steps < 4; steps++)
          {
            if (Rotate(baseMask, steps) != targetMask)
              continue;

            piece = candidate;
            rotationDegrees = steps * 90f;
            return true;
          }
        }

      piece = null;
      rotationDegrees = 0f;
      return false;
    }

    private static RoadConnections GetConnections(
      Vector2Int cell, IReadOnlyDictionary<Vector2Int, RoadCellData> roadCells)
    {
      var connections = RoadConnections.None;
      for (var i = 0; i < Offsets.Length; i++)
        if (roadCells.ContainsKey(cell + Offsets[i]))
          connections |= Directions[i];

      return connections;
    }

    private static int CountConnections(RoadConnections connections)
    {
      var count = 0;
      for (var i = 0; i < Directions.Length; i++)
        if ((connections & Directions[i]) != 0)
          count++;

      return count;
    }

    private static int CompareCells(Vector2Int a, Vector2Int b) =>
      a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x);

    private static int CompareNodes(RoadNode a, RoadNode b) =>
      CompareCells(a.Anchor, b.Anchor);

    private static int CompareJunctionCandidates(
      WideJunctionCandidate a, WideJunctionCandidate b)
    {
      var comparison = CountConnections(b.Connections).CompareTo(CountConnections(a.Connections));
      if (comparison != 0)
        return comparison;

      comparison = b.FourCellCount.CompareTo(a.FourCellCount);
      return comparison != 0 ? comparison : CompareCells(a.Anchor, b.Anchor);
    }

    private static int ComparePairCandidates(WidePairCandidate a, WidePairCandidate b)
    {
      var comparison = CompareCells(a.Anchor, b.Anchor);
      if (comparison != 0)
        return comparison;

      return a.AcrossX == b.AcrossX ? 0 : (a.AcrossX ? -1 : 1);
    }

    private static void WarnMissing(
      HashSet<string> warnings,
      Vector2Int cell,
      RoadConnections targetMask,
      RoadWidth targetWidth,
      RoadKind kind,
      bool junction)
    {
      var key = $"{kind}:{targetWidth}:{targetMask}:{junction}";
      if (!warnings.Add(key))
        return;

      var location = junction ? "junction" : "ordinary segment";
      Debug.LogWarning(
        $"RoadFiller: no exact {kind} / {targetWidth} piece for {location} at {cell} ({targetMask}).");
    }

    private static RoadConnections Rotate(RoadConnections mask, int steps)
    {
      var result = RoadConnections.None;
      for (var i = 0; i < Directions.Length; i++)
        if ((mask & Directions[i]) != 0)
          result |= Directions[(i + steps) % Directions.Length];
      return result;
    }
  }
}
