using System.Collections.Generic;
using UnityEngine;

namespace Movement
{
  [DisallowMultipleComponent]
  public sealed class MovementAgent : MonoBehaviour
  {
    private static readonly List<MovementAgent> ActiveAgentsInternal = new();

    internal static IReadOnlyList<MovementAgent> ActiveAgents => ActiveAgentsInternal;

    internal IMovementController Controller { get; private set; }
    internal Vector3 Position { get; set; }
    internal Vector3 Velocity { get; set; }
    internal Vector3 SmoothedVelocity { get; set; }
    internal Vector3 DesiredVelocity { get; set; }
    internal Quaternion Rotation { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() =>
      ActiveAgentsInternal.Clear();

    private void Awake()
    {
      Controller = GetComponent<IMovementController>();
      Position = transform.position;
      Rotation = transform.rotation;

      if (Controller == null)
        Debug.LogError($"{name} needs a PlayerMovement or MobMovement component.", this);
    }

    private void OnEnable()
    {
      if (!ActiveAgentsInternal.Contains(this))
        ActiveAgentsInternal.Add(this);
    }

    private void OnDisable() =>
      ActiveAgentsInternal.Remove(this);

    internal void ReadTransform()
    {
      Position = transform.position;
      Rotation = transform.rotation;
    }

    internal void ApplyResult()
    {
      transform.SetPositionAndRotation(Position, Rotation);
    }
  }
}
