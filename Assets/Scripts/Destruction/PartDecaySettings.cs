using System;
using UnityEngine;

namespace Destruction
{
  [Serializable]
  public class PartDecaySettings
  {
    [Tooltip("Seconds after the impulse before broken parts start sinking away.")]
    [Min(0f)] public float destructionDelay = 1f;

    [Tooltip("Downward sinking speed once decay starts, in units per second.")]
    [Min(0f)] public float fallSpeed = 1f;

    [Tooltip("How far below the sampled ground height a part must sink before it is removed.")]
    public float sinkDepth = 0.5f;
  }
}
