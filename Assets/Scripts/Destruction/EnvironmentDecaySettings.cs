using UnityEngine;

namespace Destruction
{
  [CreateAssetMenu(fileName = "EnvironmentDecaySettings", menuName = "Destruction/Environment Decay Settings")]
  public class EnvironmentDecaySettings : ScriptableObject
  {
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Min(0f)] private float groundRaycastHeight = 50f;
    [SerializeField, Min(0f)] private float groundRaycastDistance = 200f;

    public LayerMask GroundLayer => groundLayer;
    public float GroundRaycastHeight => groundRaycastHeight;
    public float GroundRaycastDistance => groundRaycastDistance;
  }
}
