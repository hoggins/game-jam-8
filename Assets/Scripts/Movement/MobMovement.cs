using Combat;
using UnityEngine;

namespace Movement
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(MovementAgent), typeof(Mob))]
  public sealed class MobMovement : MonoBehaviour, IMovementController
  {
    [Header("Movement")]
    [SerializeField, Min(0f)] private float _speed = 4f;
    [SerializeField, Min(0f)] private float _radius = 0.5f;
    [SerializeField, Min(0f)] private float _velocitySmoothing = 3f;
    [SerializeField, Min(0f)] private float _rotationSpeed = 10f;

    [Header("Flocking")]
    [SerializeField, Min(0f)] private float _avoidancePower = 1f;
    [SerializeField] private MovementLayer _collidesWith = MovementLayer.Player | MovementLayer.Mob;

    float IMovementController.Speed => _speed;
    float IMovementController.Radius => _radius;
    float IMovementController.AvoidancePower => _avoidancePower;
    float IMovementController.VelocitySmoothing => _velocitySmoothing;
    float IMovementController.RotationSpeed => _rotationSpeed;
    MovementLayer IMovementController.Layer => MovementLayer.Mob;
    MovementLayer IMovementController.CollidesWith => _collidesWith;

    internal float Radius => _radius;

    Vector3 IMovementController.GetDesiredVelocity(in MovementContext context) =>
      context.GetFlowDirection() * _speed;

    private void OnValidate()
    {
      _speed = Mathf.Max(0f, _speed);
      _radius = Mathf.Max(0f, _radius);
      _velocitySmoothing = Mathf.Max(0f, _velocitySmoothing);
      _rotationSpeed = Mathf.Max(0f, _rotationSpeed);
      _avoidancePower = Mathf.Max(0f, _avoidancePower);
    }
  }
}
