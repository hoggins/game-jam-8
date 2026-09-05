using System;
using UnityEngine;

namespace Map
{
  [Serializable]
  public class SpecialHouseObject
  {
    public SpecialHouses type;
    public GameObject prefab;
    public Vector2Int size = Vector2Int.one;
    public bool enabled = true;
  }
}
