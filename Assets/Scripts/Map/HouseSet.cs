using System.Collections.Generic;
using UnityEngine;

namespace Map
{
  [CreateAssetMenu(fileName = "HouseSet", menuName = "Map/House Set")]
  public class HouseSet : ScriptableObject
  {
    [SerializeField] private List<HouseObject> houses = new();

    public IReadOnlyList<HouseObject> Houses => houses;
  }
}
