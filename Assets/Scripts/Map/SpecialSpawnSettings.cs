using System;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
  [Serializable]
  public struct SpecialSpawnRange
  {
    public SpecialHouses type;

    [Tooltip("Max distance from the player this type may land at when the level first fills (placed on the grid, not via TrySpawnSpecial).")]
    [Min(0f)] public float initialMaxDistance;

    [Tooltip("Max distance for each successive runtime respawn, in order (min distance is always 0). Once exhausted, the last value repeats for every further respawn.")]
    public float[] respawnMaxDistances;
  }

  /// Per-type distance configuration for a special object (e.g. the battle timer): how close to the
  /// player it may land at level start, and how that range grows across successive runtime respawns
  /// (see <see cref="MapEnvironmentSpawner.TrySpawnSpecial"/>).
  [CreateAssetMenu(fileName = "SpecialSpawnSettings", menuName = "Map/Special Spawn Settings")]
  public sealed class SpecialSpawnSettings : ScriptableObject
  {
    [SerializeField] private List<SpecialSpawnRange> ranges = new();

    public bool TryGetInitialMaxDistance(SpecialHouses type, out float maxDistance)
    {
      foreach (var range in ranges)
        if (range.type == type)
        {
          maxDistance = range.initialMaxDistance;
          return true;
        }

      maxDistance = 0f;
      return false;
    }

    /// <param name="respawnIndex">0-based: 0 for the first runtime respawn, 1 for the second, etc.
    /// Clamped to the last configured entry once the list runs out.</param>
    public bool TryGetRespawnMaxDistance(SpecialHouses type, int respawnIndex, out float maxDistance)
    {
      foreach (var range in ranges)
        if (range.type == type)
        {
          if (range.respawnMaxDistances == null || range.respawnMaxDistances.Length == 0)
          {
            maxDistance = 0f;
            return false;
          }

          var index = Mathf.Clamp(respawnIndex, 0, range.respawnMaxDistances.Length - 1);
          maxDistance = range.respawnMaxDistances[index];
          return true;
        }

      maxDistance = 0f;
      return false;
    }

    private void OnValidate()
    {
      for (var i = 0; i < ranges.Count; i++)
      {
        var range = ranges[i];
        range.initialMaxDistance = Mathf.Max(0f, range.initialMaxDistance);
        if (range.respawnMaxDistances != null)
          for (var j = 0; j < range.respawnMaxDistances.Length; j++)
            range.respawnMaxDistances[j] = Mathf.Max(0f, range.respawnMaxDistances[j]);

        ranges[i] = range;
      }
    }
  }
}
