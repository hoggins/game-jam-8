using UnityEngine;

namespace Destruction
{
  [CreateAssetMenu(fileName = "EnvironmentDecaySettings", menuName = "Destruction/Environment Decay Settings")]
  public class EnvironmentDecaySettings : ScriptableObject
  {
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Min(0f)] private float groundRaycastHeight = 50f;
    [SerializeField, Min(0f)] private float groundRaycastDistance = 200f;

    [Tooltip("A part must stay below both idle speed thresholds for this many seconds before decay starts.")]
    [SerializeField, Min(0f)] private float idleGraceTime = 0.25f;

    [Tooltip("Linear speed (units/s) below which a part is considered to have stopped moving.")]
    [SerializeField, Min(0f)] private float idleLinearSpeedThreshold = 0.05f;

    [Tooltip("Angular speed (rad/s) below which a part is considered to have stopped spinning.")]
    [SerializeField, Min(0f)] private float idleAngularSpeedThreshold = 0.05f;

    [Tooltip("How far below the sampled ground height a part must sink before it is removed.")]
    [SerializeField] private float sinkDepth = 0.5f;

    [Tooltip("Cap on the per-part volume-derived fall speed multiplier, so very large parts don't decay unreasonably fast.")]
    [SerializeField, Min(0f)] private float maxFallSpeedMultiplier = 5f;

    public LayerMask GroundLayer => groundLayer;
    public float GroundRaycastHeight => groundRaycastHeight;
    public float GroundRaycastDistance => groundRaycastDistance;
    public float IdleGraceTime => idleGraceTime;
    public float IdleLinearSpeedThreshold => idleLinearSpeedThreshold;
    public float IdleAngularSpeedThreshold => idleAngularSpeedThreshold;
    public float SinkDepth => sinkDepth;
    public float MaxFallSpeedMultiplier => maxFallSpeedMultiplier;
  }
}
