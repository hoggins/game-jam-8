using System;
using UnityEngine;

namespace Destruction
{
  [Serializable]
  public class PartDecaySettings
  {
    [Tooltip("Base downward sinking speed once decay starts, in units per second, before the per-part multiplier.")]
    [Min(0f)] public float baseFallSpeed = 1f;

    [Tooltip("Per-part multiplier applied to the base fall speed. Derived from the part's volume so bigger parts decay faster, capped for very large parts.")]
    [Min(0f)] public float speedMultiplier = 1f;

    public float FallSpeed => baseFallSpeed * speedMultiplier;

    /// <summary>
    /// Derives this part's multiplier from its bounds volume (in cubic units), capped so very large
    /// parts (e.g. whole walls) don't decay unreasonably fast.
    /// </summary>
    public PartDecaySettings ForVolume(float volume, float maxMultiplier)
    {
      return new PartDecaySettings
      {
        baseFallSpeed = baseFallSpeed,
        speedMultiplier = Mathf.Clamp(volume, 0.001f, maxMultiplier),
      };
    }
  }
}
