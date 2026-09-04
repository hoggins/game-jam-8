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

    public LayerMask GroundLayer => groundLayer;
    public float GroundRaycastHeight => groundRaycastHeight;
    public float GroundRaycastDistance => groundRaycastDistance;
    public float IdleGraceTime => idleGraceTime;
    public float IdleLinearSpeedThreshold => idleLinearSpeedThreshold;
    public float IdleAngularSpeedThreshold => idleAngularSpeedThreshold;
  }
}
