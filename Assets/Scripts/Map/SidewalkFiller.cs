using System.Collections.Generic;
using UnityEngine;

namespace Map
{
  public readonly struct SidewalkPlacement
  {
    public readonly SidewalkPiece Piece;
    public readonly Vector2Int Cell;
    public readonly float RotationDegrees;

    public SidewalkPlacement(SidewalkPiece piece, Vector2Int cell, float rotationDegrees)
    {
      Piece = piece;
      Cell = cell;
      RotationDegrees = rotationDegrees;
    }
  }

  public static class SidewalkFiller
  {
    // Prefer the first direction when a sidewalk touches more than one road. This keeps the
    // result deterministic while still making the usual single-road sidewalk face the road.
    private static readonly Vector2Int[] RoadOffsets =
    {
      Vector2Int.up,
      Vector2Int.right,
      Vector2Int.down,
      Vector2Int.left,
    };

    public static List<SidewalkPlacement> Fill(MapData mapData, SidewalkSet sidewalkSet, int seed = 0)
    {
      var placements = new List<SidewalkPlacement>();
      if (mapData == null || sidewalkSet == null)
        return placements;

      var pieces = new List<SidewalkPiece>();
      var totalWeight = 0f;
      foreach (var candidate in sidewalkSet.Pieces)
      {
        if (!candidate.enabled || candidate.prefab == null)
          continue;

        pieces.Add(candidate);
        totalWeight += candidate.weight;
      }

      if (pieces.Count == 0 || totalWeight <= 0f)
        return placements;

      var random = new System.Random(seed);

      foreach (var cell in mapData.SidewalkCells)
      {
        var roll = (float)random.NextDouble() * totalWeight;
        var accumulated = 0f;
        var chosen = pieces[^1];
        foreach (var candidate in pieces)
        {
          accumulated += candidate.weight;
          if (roll >= accumulated)
            continue;

          chosen = candidate;
          break;
        }

        var rotationDegrees = GetRoadFacingRotation(mapData, cell);

        placements.Add(new SidewalkPlacement(chosen, cell, rotationDegrees));
      }

      return placements;
    }

    private static float GetRoadFacingRotation(MapData mapData, Vector2Int sidewalkCell)
    {
      foreach (var offset in RoadOffsets)
      {
        if (!mapData.IsRoad(sidewalkCell + offset))
          continue;

        // Grid +Y is world +Z, so a road to the east requires a +90 degree yaw, etc.
        return Mathf.Atan2(offset.x, offset.y) * Mathf.Rad2Deg;
      }

      return 0f;
    }
  }
}
