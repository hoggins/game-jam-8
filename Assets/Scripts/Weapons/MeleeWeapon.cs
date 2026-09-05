using System.Collections.Generic;
using Balance;
using Combat;
using Destruction;
using Movement;
using UnityEngine;
using VContainer;

namespace Weapons
{
  [DisallowMultipleComponent]
  public sealed class MeleeWeapon : Weapon
  {
    private const string BattleBalanceResourcePath = "BattleBalanceConfig";

    [SerializeField, Min(0f)] private float _attackRadius = 5f;
    [SerializeField, Range(0f, 360f)] private float _attackConeAngle = 90f;
    [SerializeField, Min(1f)] private float _attackScaleMultiplier = 1.2f;
    [SerializeField] private bool _alternateAttackFx;
    [SerializeField] private bool _damageOnlyProps;
    [SerializeField] private GameObject _hitFxPrefab;
    [SerializeField, Min(0)] private int _hitFxPrewarmCount = 8;

    [Inject] private MovementUpdater _movementUpdater;
    [Inject] private BattleBalanceConfig _battleBalance;

    private readonly List<MovementAgent> _targets = new(16);
    private readonly HashSet<Mob> _hitMobs = new();
    private readonly Collider[] _destructibleHits = new Collider[16];

    private static BattleBalanceConfig _resourcesBattleBalance;

    private int _damagableLayerMask;
    private bool _nextAttackFxMirrored = true;

    // Falls back to a direct Resources load so the radius still shows correctly for OnValidate
    // and the gizmo, which run in the editor outside of play mode where DI has not injected
    // _battleBalance yet.
    private float BaseAttackRadius =>
      (_battleBalance != null
        ? _battleBalance
        : _resourcesBattleBalance = _resourcesBattleBalance != null
          ? _resourcesBattleBalance
          : Resources.Load<BattleBalanceConfig>(BattleBalanceResourcePath))
      ?.MeleeAttackRadius ?? 0f;

    private float AttackScaleFactor => CharacterScaleFactor * _attackScaleMultiplier;
    private float AttackRadius => (_damageOnlyProps ? _attackRadius : BaseAttackRadius) * AttackScaleFactor;

    protected override float AttackFxScaleMultiplier => _attackScaleMultiplier;

    protected override void SpawnAttackFx()
    {
      SpawnAttackFx(_alternateAttackFx && _nextAttackFxMirrored);
      if (_alternateAttackFx)
        _nextAttackFxMirrored = !_nextAttackFxMirrored;
    }

    protected override void Awake()
    {
      base.Awake();
      PrewarmFx(_hitFxPrefab, _hitFxPrewarmCount);
      _damagableLayerMask = LayerMask.GetMask(DestructibleLayers.Damagable);
    }

    protected override void Attack(int damage)
    {
      _hitMobs.Clear();

      if (!_damageOnlyProps && _movementUpdater != null)
      {
        _movementUpdater.QueryCircle(
          transform.position,
          AttackRadius,
          MovementLayer.Mob,
          _targets);

        for (var i = 0; i < _targets.Count; i++)
        {
          var target = _targets[i];
          if (target == null
              || !target.isActiveAndEnabled
              || !IsInsideAttackCone(target.transform.position))
            continue;

          DamageTarget(target, damage);
        }
      }

      if (!_damageOnlyProps)
      {
        var attachedMobs = Mob.AttachedMobs;
        for (var i = 0; i < attachedMobs.Count; i++)
        {
          var mob = attachedMobs[i];
          if (mob == null
              || !mob.IsAttached
              || !mob.IsAlive
              || !_hitMobs.Add(mob))
            continue;

          mob.TakeDamage(damage);
          SpawnHitFx(mob.transform.position, mob.transform.rotation);
        }
      }

      var hitCount = Physics.OverlapSphereNonAlloc(
        transform.position,
        AttackRadius,
        _destructibleHits,
        _damagableLayerMask);

      for (var i = 0; i < hitCount; i++)
      {
        var target = _destructibleHits[i].gameObject;
        if (IsInsideAttackCone(target.transform.position))
          DamageDestructible(target, damage);
      }
    }

    private void DamageTarget(MovementAgent target, int damage)
    {
      var mob = target.GetComponent<Mob>();
      if (mob != null)
      {
        if (!mob.IsAlive || !_hitMobs.Add(mob))
          return;

        mob.TakeDamage(damage);
        SpawnHitFx(target.transform.position, target.transform.rotation);
        return;
      }

      var components = target.GetComponents<MonoBehaviour>();
      for (var i = 0; i < components.Length; i++)
      {
        if (components[i] is not IDamageable damageable || !damageable.IsAlive)
          continue;

        damageable.TakeDamage(damage);
        SpawnHitFx(target.transform.position, target.transform.rotation);
        break;
      }
    }

    private void DamageDestructible(GameObject target, int damage)
    {
      if (_damageOnlyProps
          && (target.GetComponent<DestructibleHealth>()?.ObjectType != DestructibleObjectType.Prop))
        return;

      var components = target.GetComponents<MonoBehaviour>();
      for (var i = 0; i < components.Length; i++)
      {
        if (components[i] is not IDamageable damageable || !damageable.IsAlive)
          continue;

        if (damageable is IImpactDamageable impactDamageable)
          impactDamageable.TakeDamage(damage, transform.position);
        else
          damageable.TakeDamage(damage);

        SpawnHitFx(target.transform.position, target.transform.rotation);
        break;
      }
    }

    private void SpawnHitFx(Vector3 position, Quaternion rotation)
    {
      var hitFx = SpawnFx(_hitFxPrefab, position, rotation);
      if (hitFx != null && _hitFxPrefab != null)
        hitFx.transform.localScale = _hitFxPrefab.transform.localScale * AttackScaleFactor;
    }

    private void OnValidate()
    {
      _attackRadius = Mathf.Max(0f, _attackRadius);
      _attackConeAngle = Mathf.Clamp(_attackConeAngle, 0f, 360f);
      _attackScaleMultiplier = Mathf.Max(1f, _attackScaleMultiplier);
      _hitFxPrewarmCount = Mathf.Max(0, _hitFxPrewarmCount);
    }

    private bool IsInsideAttackCone(Vector3 targetPosition)
    {
      var offset = targetPosition - transform.position;
      offset.y = 0f;
      if (offset.sqrMagnitude <= Mathf.Epsilon)
        return true;

      var forward = GetAttackForward();
      var minimumDot = Mathf.Cos(_attackConeAngle * 0.5f * Mathf.Deg2Rad);
      return Vector3.Dot(forward, offset.normalized) >= minimumDot;
    }

    private Vector3 GetAttackForward()
    {
      var forward = transform.forward;
      forward.y = 0f;
      return forward.sqrMagnitude > Mathf.Epsilon ? forward.normalized : Vector3.forward;
    }

    private void OnDrawGizmosSelected()
    {
      var attackRadius = AttackRadius;
      if (attackRadius <= 0f)
        return;

      var origin = transform.position + Vector3.up * 0.05f;
      var forward = GetAttackForward();
      var halfAngle = _attackConeAngle * 0.5f;
      var segmentCount = Mathf.Max(4, Mathf.CeilToInt(_attackConeAngle / 10f));
      var previousPoint = origin
        + (Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward) * attackRadius;

      Gizmos.color = new Color(1f, 0.65f, 0.1f, 0.9f);
      Gizmos.DrawLine(origin, previousPoint);

      for (var i = 1; i <= segmentCount; i++)
      {
        var angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segmentCount);
        var point = origin
          + (Quaternion.AngleAxis(angle, Vector3.up) * forward) * attackRadius;
        Gizmos.DrawLine(previousPoint, point);
        previousPoint = point;
      }

      Gizmos.DrawLine(origin, previousPoint);
      Gizmos.DrawLine(origin, origin + forward * attackRadius);
    }
  }
}
