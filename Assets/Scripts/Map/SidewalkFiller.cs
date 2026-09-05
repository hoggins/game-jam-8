using System.Collections.Generic;
using UnityEngine;

namespace Map
{
  public readonly struct SidewalkPlacement
  {
    public readonly SidewalkPiece Piece;
    public readonly Vector2Int Cell;

    public SidewalkPlacement(SidewalkPiece piece, Vector2Int cell)
    {
      Piece = piece;
      Cell = cell;
    }
  }

  public static class SidewalkFiller
  {
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

        placements.Add(new SidewalkPlacement(chosen, cell));
      }

      return placements;
    }
  }
}
