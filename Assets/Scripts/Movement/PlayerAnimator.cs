using App.Common.Utils;
using UnityEngine;
using Weapons;

namespace Movement
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(MovementAgent))]
  public sealed class PlayerAnimator : MonoBehaviour
  {
    private static readonly int LocomotionParameter = Animator.StringToHash("Locomotion");
    private static readonly int AttackParameter = Animator.StringToHash("Attack");

    [SerializeField] private Animator _animator;
    [SerializeField] private MeleeWeapon _meleeWeapon;
    [SerializeField, Min(0f)] private float _locomotionSmoothment = 10f;

    private MovementAgent _movementAgent;

    private void Awake()
    {
      _movementAgent = GetComponent<MovementAgent>();
      _animator ??= GetComponentInChildren<Animator>(true);
      _meleeWeapon ??= GetComponentInChildren<MeleeWeapon>(true);
    }

    private void OnEnable()
    {
      if (_meleeWeapon != null)
        _meleeWeapon.AttackPerformed += OnAttackPerformed;
    }

    private void OnDisable()
    {
      if (_meleeWeapon != null)
        _meleeWeapon.AttackPerformed -= OnAttackPerformed;
    }

    private void Update()
    {
      if (_animator == null || _movementAgent == null)
        return;

      var maxSpeed = _movementAgent.Controller?.Speed ?? 0f;
      var targetLocomotion = maxSpeed > 0f
        ? _movementAgent.SmoothedVelocity.magnitude / maxSpeed
        : 0f;
      targetLocomotion = Mathf.Clamp01(targetLocomotion);

      var locomotion = _locomotionSmoothment > 0f
        ? StableInterpolation.Lerp(
          _animator.GetFloat(LocomotionParameter),
          targetLocomotion,
          Time.deltaTime * _locomotionSmoothment)
        : targetLocomotion;
      _animator.SetFloat(LocomotionParameter, locomotion);
    }

    private void OnAttackPerformed() =>
      _animator?.SetTrigger(AttackParameter);

    private void OnValidate() =>
      _locomotionSmoothment = Mathf.Max(0f, _locomotionSmoothment);
  }
}
