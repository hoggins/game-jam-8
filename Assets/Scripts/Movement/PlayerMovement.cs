using App;
using Balance;
using Destruction;
using Model;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

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
    [SerializeField, Min(0f)] private float _radius = 0.5f;
    [SerializeField, Min(0f)] private float _rotationSpeed = 12f;

    [Header("Flocking")]
    [SerializeField, Min(0f)] private float _avoidancePower = 2.5f;
    [SerializeField] private MovementLayer _collidesWith = MovementLayer.Mob;

    [Header("Ground Damage Trail")]
    [SerializeField] private bool _leaveDamageTrail = true;
    [SerializeField, Min(0f)] private float _damageTrailRadius = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _damageTrailIntensityPerSecond = 0.05f;
    [SerializeField, Range(0f, 1f)] private float _damageTrailSmoothness = 1f;
    [SerializeField] private Color _damageTrailColor = Color.white;

    [Inject] private CharacterService _characterService;
    [Inject] private ProgressionBalanceConfig _progressionBalance;

    private bool _enabledMoveAction;
    private Vector3 _previousPosition;
    private bool _hasPreviousPosition;

    /// Movement speed is a character stat; the service is missing only before the
    /// container exists, so fall back to the starting value then.
    private float Speed => _characterService?.Speed ?? _progressionBalance.StartingSpeed;

    private void Awake()
    {
      this.AsInjected();

      if (GetComponent<PlayerAnimator>() == null)
        gameObject.AddComponent<PlayerAnimator>();
    }

    float IMovementController.Speed => Speed;
    float IMovementController.Radius => _radius * (_characterService?.CharacterScaleFactor ?? 1f);
    float IMovementController.AvoidancePower => _avoidancePower;
    float IMovementController.VelocitySmoothing => 0f;
    float IMovementController.RotationSpeed => _rotationSpeed;
    MovementLayer IMovementController.Layer => MovementLayer.Player;
    MovementLayer IMovementController.CollidesWith => _collidesWith;

    private void OnEnable()
    {
      _previousPosition = transform.position;
      _hasPreviousPosition = true;

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
      _hasPreviousPosition = false;
    }

    private void Update()
    {
      if (!_hasPreviousPosition)
      {
        _previousPosition = transform.position;
        _hasPreviousPosition = true;
        return;
      }

      var currentPosition = transform.position;
      var movement = currentPosition - _previousPosition;
      movement.y = 0f;

      if (_leaveDamageTrail
        && movement.sqrMagnitude > 0.000001f
        && _damageTrailIntensityPerSecond > 0f)
      {
        GroundDamageMask.Instance?.ApplyCircleDamage(
          currentPosition,
          _damageTrailRadius,
          _damageTrailColor,
          _damageTrailIntensityPerSecond * Time.deltaTime,
          _damageTrailSmoothness);
      }

      _previousPosition = currentPosition;
    }

    Vector3 IMovementController.GetDesiredVelocity(in MovementContext context)
    {
      context.Agent.DesiredFacingDirection = Vector3.zero;

      var action = _moveAction != null ? _moveAction.action : null;
      if (action == null)
        return Vector3.zero;

      var input = Vector2.ClampMagnitude(action.ReadValue<Vector2>(), 1f);
      var camera = Camera.main;
      if (camera == null)
      {
        var fallbackMovement = new Vector3(input.x, 0f, input.y);
        context.Agent.DesiredFacingDirection = fallbackMovement;
        return fallbackMovement * Speed;
      }

      var cameraForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
      if (cameraForward.sqrMagnitude < 0.000001f)
        cameraForward = Vector3.ProjectOnPlane(camera.transform.up, Vector3.up);

      cameraForward.Normalize();
      var cameraRight = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
      var movement = cameraRight * input.x + cameraForward * input.y;
      context.Agent.DesiredFacingDirection = movement;
      return movement * Speed;
    }

    private void OnValidate()
    {
      _radius = Mathf.Max(0f, _radius);
      _rotationSpeed = Mathf.Max(0f, _rotationSpeed);
      _avoidancePower = Mathf.Max(0f, _avoidancePower);
      _damageTrailRadius = Mathf.Max(0f, _damageTrailRadius);
      _damageTrailIntensityPerSecond = Mathf.Clamp01(_damageTrailIntensityPerSecond);
      _damageTrailSmoothness = Mathf.Clamp01(_damageTrailSmoothness);
    }
  }
}
