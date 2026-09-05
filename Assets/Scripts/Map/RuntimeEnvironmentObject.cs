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

    /// World-space footprint, used for overlap checks against objects that aren't grid-placed
    /// (e.g. a special object dropped at an arbitrary position/rotation).
    public readonly Vector3 WorldCenter;
    public readonly Vector2 WorldHalfExtents;

    public RuntimeEnvironmentObject(
      int id, Vector2Int cell, Vector2Int size, DestructibleObject destructible,
      Vector3 worldCenter, Vector2 worldHalfExtents)
    {
      Id = id;
      Cell = cell;
      Size = size;
      Destructible = destructible;
      WorldCenter = worldCenter;
      WorldHalfExtents = worldHalfExtents;
    }
  }
}
