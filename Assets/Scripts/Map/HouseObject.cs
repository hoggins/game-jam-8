using System;
using UnityEngine;

namespace Map
{
  [Serializable]
  public class HouseObject
  {
    public string name = "House";
    public GameObject prefab;
    public Vector2Int size = Vector2Int.one;
    public bool enabled = true;

    [Tooltip("Placed exactly once per map, before the regular fill, at a random cell that fits it.")]
    public bool unique;

    [Range(1, HouseSet.DifficultyLevelCount)]
    public int difficultyLevel = 1;
  }
}
