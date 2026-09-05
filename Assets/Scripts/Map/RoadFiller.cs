using System.Collections.Generic;
using UnityEngine;

namespace Map
{
  public readonly struct RoadPlacement
  {
    public readonly RoadPiece Piece;
    public readonly Vector2Int Cell;
    public readonly float RotationDegrees;

    public RoadPlacement(RoadPiece piece, Vector2Int cell, float rotationDegrees)
    {
      Piece = piece;
      Cell = cell;
      RotationDegrees = rotationDegrees;
    }
  }

  public static class RoadFiller
  {
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

    public static List<RoadPlacement> Fill(MapData mapData, RoadSet roadSet)
    {
      var placements = new List<RoadPlacement>();
      if (mapData == null || roadSet == null)
        return placements;

      var roadCells = new HashSet<Vector2Int>();
      foreach (var road in mapData.RoadCells)
        roadCells.Add(road.cell);

      foreach (var road in mapData.RoadCells)
      {
        var mask = RoadConnections.None;
        for (var i = 0; i < Offsets.Length; i++)
          if (roadCells.Contains(road.cell + Offsets[i]))
            mask |= Directions[i];

        if (TryFindPiece(roadSet, mask, road.width, out var piece, out var rotationDegrees))
          placements.Add(new RoadPlacement(piece, road.cell, rotationDegrees));
      }

      return placements;
    }

    /// <summary>
    /// Finds the piece whose (rotated) connections match the target shape, preferring one whose
    /// arms are all authored for the cell's actual width (e.g. a dedicated one-way corner) and
    /// falling back to any shape match when no width-specific piece exists, so shapes without a
    /// width variant yet keep working exactly as before.
    /// </summary>
    private static bool TryFindPiece(
      RoadSet roadSet, RoadConnections targetMask, RoadWidth targetWidth, out RoadPiece piece, out float rotationDegrees)
    {
      RoadPiece fallbackPiece = null;
      var fallbackRotation = 0f;

      foreach (var candidate in roadSet.Pieces)
      {
        if (!candidate.enabled || candidate.prefab == null)
          continue;

        var baseMask = RoadConnections.None;
        var widthMatches = true;
        foreach (var arm in candidate.connections)
        {
          baseMask |= arm.direction;
          if (arm.width != targetWidth)
            widthMatches = false;
        }

        for (var steps = 0; steps < 4; steps++)
        {
          if (Rotate(baseMask, steps) != targetMask)
            continue;

          if (widthMatches)
          {
            piece = candidate;
            rotationDegrees = steps * 90f;
            return true;
          }

          if (fallbackPiece == null)
          {
            fallbackPiece = candidate;
            fallbackRotation = steps * 90f;
          }

          break;
        }
      }

      if (fallbackPiece != null)
      {
        piece = fallbackPiece;
        rotationDegrees = fallbackRotation;
        return true;
      }

      piece = null;
      rotationDegrees = 0f;
      return false;
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
