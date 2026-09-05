using UnityEngine;

namespace Destruction
{
  [CreateAssetMenu(fileName = "EnvironmentDecaySettings", menuName = "Destruction/Environment Decay Settings")]
  public class EnvironmentDecaySettings : ScriptableObject
  {
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Min(0f)] private float groundRaycastHeight = 50f;
    [SerializeField, Min(0f)] private float groundRaycastDistance = 200f;

    [Tooltip("How far below the sampled ground height a part must sink before it is removed.")]
    [SerializeField] private float sinkDepth = 0.5f;

    [Tooltip("Time after a part detaches before it starts sinking, regardless of whether it has settled.")]
    [SerializeField, Min(0f)] private float decayStartDelay = 1f;

    [Tooltip("Cap on the per-part volume-derived fall speed multiplier, so very large parts don't decay unreasonably fast.")]
    [SerializeField, Min(0f)] private float maxFallSpeedMultiplier = 5f;

    public LayerMask GroundLayer => groundLayer;
    public float GroundRaycastHeight => groundRaycastHeight;
    public float GroundRaycastDistance => groundRaycastDistance;
    public float SinkDepth => sinkDepth;
    public float DecayStartDelay => decayStartDelay;
    public float MaxFallSpeedMultiplier => maxFallSpeedMultiplier;
  }
}
