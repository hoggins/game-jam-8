using App;
using UnityEngine;
using VContainer;

namespace Movement
{
  [DisallowMultipleComponent]
  public sealed class MovementAgent : MonoBehaviour
  {
    [Inject] private MovementUpdater _movementUpdater;

    internal IMovementController Controller { get; private set; }
    internal Vector3 Position { get; set; }
    internal Vector3 Velocity { get; set; }
    internal Vector3 SmoothedVelocity { get; set; }
    internal Vector3 DesiredVelocity { get; set; }
    internal Quaternion Rotation { get; set; }

    private void Awake()
    {
      this.AsInjected();
      Controller = GetComponent<IMovementController>();
      Position = transform.position;
      Rotation = transform.rotation;

      if (Controller == null)
        Debug.LogError($"{name} needs a PlayerMovement or MobMovement component.", this);
    }

    private void OnEnable() =>
      _movementUpdater.Register(this);

    private void OnDisable() =>
      _movementUpdater.Unregister(this);

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
