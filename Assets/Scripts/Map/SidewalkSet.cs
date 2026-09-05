using System;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
  [Serializable]
  public class SidewalkPiece
  {
    public string name = "Sidewalk";
    public GameObject prefab;

    [Tooltip("If true, this prefab is authored facing a road and needs to be rotated at fill " +
             "time to face whichever neighbouring road cell it is next to.")]
    public bool facesRoad = true;

    [Min(0f)] public float weight = 1f;
    public bool enabled = true;
  }

  [CreateAssetMenu(fileName = "SidewalkSet", menuName = "Map/Sidewalk Set")]
  public class SidewalkSet : ScriptableObject
  {
    [SerializeField] private List<SidewalkPiece> pieces = new();

    public IReadOnlyList<SidewalkPiece> Pieces => pieces;
  }
}
