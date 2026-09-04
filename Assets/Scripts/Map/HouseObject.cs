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
  }
}
