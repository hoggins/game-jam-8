using Destruction;
using UnityEngine;

namespace Map
{
  public readonly struct RuntimeEnvironmentObject
  {
    public readonly int Id;
    public readonly Vector2Int Cell;
    public readonly Vector2Int Size;
    public readonly ImpulseDestructible Destructible;

    public RuntimeEnvironmentObject(int id, Vector2Int cell, Vector2Int size, ImpulseDestructible destructible)
    {
      Id = id;
      Cell = cell;
      Size = size;
      Destructible = destructible;
    }
  }
}
