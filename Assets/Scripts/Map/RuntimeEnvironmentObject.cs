using Destruction;
using UnityEngine;

namespace Map
{
  public readonly struct RuntimeEnvironmentObject
  {
    public readonly int Id;
    public readonly Vector2Int Cell;
    public readonly Vector2Int Size;
    public readonly DestructibleObject Destructible;

    public RuntimeEnvironmentObject(int id, Vector2Int cell, Vector2Int size, DestructibleObject destructible)
    {
      Id = id;
      Cell = cell;
      Size = size;
      Destructible = destructible;
    }
  }
}
