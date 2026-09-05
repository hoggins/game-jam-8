using System;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
  public enum RoadWidth
  {
    OneWay,
    TwoWay,
  }

  public enum RoadKind
  {
    NoLine,
    Line,
  }

  [Flags]
  public enum RoadConnections
  {
    None = 0,
    North = 1 << 0,
    East = 1 << 1,
    South = 1 << 2,
    West = 1 << 3,
  }

  public enum RoadPieceShape
  {
    Isolated,
    DeadEnd,
    Straight,
    Corner,
    TJunction,
    Cross,
  }

  [Serializable]
  public class RoadConnectionArm
  {
    [Tooltip("Single direction this arm faces (North/East/South/West), not a combination.")]
    public RoadConnections direction = RoadConnections.North;

    public RoadWidth width = RoadWidth.TwoWay;
  }

  [Serializable]
  public class RoadPiece
  {
    public string name = "Road";
    public GameObject prefab;
    public RoadPieceShape shape;
    public RoadKind kind = RoadKind.NoLine;

    [Tooltip("The arms this prefab is authored to satisfy at 0 degrees rotation, each with the " +
             "road width it expects on that side. Fill needs to rotate this in 90-degree steps " +
             "to match a cell's actual neighbours, including their widths - so a 2-way/2-way/1-way " +
             "T-junction is a different piece from an all-2-way one.")]
    public List<RoadConnectionArm> connections = new();

    public bool enabled = true;
  }

  [CreateAssetMenu(fileName = "RoadSet", menuName = "Map/Road Set")]
  public class RoadSet : ScriptableObject
  {
    [SerializeField] private List<RoadPiece> pieces = new();

    public IReadOnlyList<RoadPiece> Pieces => pieces;
  }
}
