using System;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
  [Serializable]
  public struct SpecialSpawnDistance
  {
    [Min(0f)] public float minDistance;
    [Min(0f)] public float maxDistance;
  }

  [Serializable]
  public struct SpecialSpawnRange
  {
    public SpecialHouses type;

    [Tooltip("Distance range from the player this type may land at when the level first fills. For the absolute zig-zag Timer route, the seeded value from this range is the first route leg.")]
    public SpecialSpawnDistance initial;

    [Tooltip("Distance range for each successive runtime respawn, in order. For the absolute zig-zag Timer route these are world-unit leg lengths; the predefined curve uses its fixed checkpoint distance instead.")]
    public SpecialSpawnDistance[] respawns;
  }

  /// Per-type distance configuration for a special object (e.g. the battle timer): how close to the
  /// player it may land at level start, and how that range grows across successive runtime respawns
  /// (see <see cref="MapEnvironmentSpawner.TrySpawnSpecial"/>).
  [CreateAssetMenu(fileName = "SpecialSpawnSettings", menuName = "Map/Special Spawn Settings")]
  public sealed class SpecialSpawnSettings : ScriptableObject
  {
    [SerializeField] private List<SpecialSpawnRange> ranges = new();

    public bool TryGetInitialDistance(SpecialHouses type, out float minDistance, out float maxDistance)
    {
      foreach (var range in ranges)
        if (range.type == type)
        {
          minDistance = range.initial.minDistance;
          maxDistance = range.initial.maxDistance;
          return true;
        }

      minDistance = 0f;
      maxDistance = 0f;
      return false;
    }

    /// <param name="respawnIndex">0-based: 0 for the first runtime respawn, 1 for the second, etc.
    /// Clamped to the last configured entry once the list runs out.</param>
    public bool TryGetRespawnDistance(SpecialHouses type, int respawnIndex, out float minDistance, out float maxDistance)
    {
      foreach (var range in ranges)
        if (range.type == type)
        {
          if (range.respawns == null || range.respawns.Length == 0)
          {
            minDistance = 0f;
            maxDistance = 0f;
            return false;
          }

          var index = Mathf.Clamp(respawnIndex, 0, range.respawns.Length - 1);
          minDistance = range.respawns[index].minDistance;
          maxDistance = range.respawns[index].maxDistance;
          return true;
        }

      minDistance = 0f;
      maxDistance = 0f;
      return false;
    }

    private void OnValidate()
    {
      for (var i = 0; i < ranges.Count; i++)
      {
        var range = ranges[i];
        range.initial = Clamped(range.initial);
        if (range.respawns != null)
          for (var j = 0; j < range.respawns.Length; j++)
            range.respawns[j] = Clamped(range.respawns[j]);

        ranges[i] = range;
      }
    }

    private static SpecialSpawnDistance Clamped(SpecialSpawnDistance distance)
    {
      distance.minDistance = Mathf.Max(0f, distance.minDistance);
      distance.maxDistance = Mathf.Max(distance.minDistance, distance.maxDistance);
      return distance;
    }
  }
}
