using UnityEngine;
using UnityEngine.InputSystem;

namespace Movement
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(MovementAgent))]
  [RequireComponent(typeof(PlayerAnimator))]
  public sealed class PlayerMovement : MonoBehaviour, IMovementController
  {
    [Header("Input")]
    [SerializeField] private InputActionReference _moveAction;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float _speed = 6f;
    [SerializeField, Min(0f)] private float _radius = 0.5f;
    [SerializeField, Min(0f)] private float _rotationSpeed = 12f;

    [Header("Flocking")]
    [SerializeField, Min(0f)] private float _avoidancePower = 2.5f;
    [SerializeField] private MovementLayer _collidesWith = MovementLayer.Mob;

    private bool _enabledMoveAction;

    private void Awake()
    {
      if (GetComponent<PlayerAnimator>() == null)
        gameObject.AddComponent<PlayerAnimator>();
    }

    float IMovementController.Speed => _speed;
    float IMovementController.Radius => _radius;
    float IMovementController.AvoidancePower => _avoidancePower;
    float IMovementController.VelocitySmoothing => 0f;
    float IMovementController.RotationSpeed => _rotationSpeed;
    MovementLayer IMovementController.Layer => MovementLayer.Player;
    MovementLayer IMovementController.CollidesWith => _collidesWith;

    private void OnEnable()
    {
      var action = _moveAction != null ? _moveAction.action : null;
      if (action == null)
      {
        Debug.LogError($"Assign the Player/Move input action to {nameof(PlayerMovement)} on {name}.", this);
        return;
      }

      _enabledMoveAction = !action.enabled;
      if (_enabledMoveAction)
        action.Enable();
    }

    private void OnDisable()
    {
      if (_enabledMoveAction && _moveAction != null)
        _moveAction.action.Disable();

      _enabledMoveAction = false;
    }

    Vector3 IMovementController.GetDesiredVelocity(in MovementContext context)
    {
      var action = _moveAction != null ? _moveAction.action : null;
      if (action == null)
        return Vector3.zero;

      var input = Vector2.ClampMagnitude(action.ReadValue<Vector2>(), 1f);
      return new Vector3(input.x, 0f, input.y) * _speed;
    }

    private void OnValidate()
    {
      _speed = Mathf.Max(0f, _speed);
      _radius = Mathf.Max(0f, _radius);
      _rotationSpeed = Mathf.Max(0f, _rotationSpeed);
      _avoidancePower = Mathf.Max(0f, _avoidancePower);
    }
  }
}
