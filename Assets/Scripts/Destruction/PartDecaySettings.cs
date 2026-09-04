using System;
using UnityEngine;

namespace Destruction
{
  [Serializable]
  public class PartDecaySettings
  {
    [Tooltip("Downward sinking speed once decay starts, in units per second.")]
    [Min(0f)] public float fallSpeed = 1f;

    [Tooltip("How far below the sampled ground height a part must sink before it is removed.")]
    public float sinkDepth = 0.5f;

    /// <summary>
    /// Scales these base values for one part, given its bounds volume (in cubic units).
    /// Bigger parts (e.g. walls) decay faster than small ones (e.g. decor): they sink at a higher speed.
    /// </summary>
    public PartDecaySettings ForVolume(float volume)
    {
      var scale = Mathf.Max(volume, 0.001f);
      return new PartDecaySettings
      {
        fallSpeed = fallSpeed * scale,
        sinkDepth = sinkDepth,
      };
    }
  }
}
