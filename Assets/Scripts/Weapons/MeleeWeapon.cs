using System.Collections.Generic;
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
    [SerializeField, Min(0f)] private float _attackRadius = 2f;
    [SerializeField, Range(0f, 360f)] private float _attackConeAngle = 90f;
    [SerializeField] private GameObject _hitFxPrefab;
    [SerializeField, Min(0)] private int _hitFxPrewarmCount = 8;

    [Inject] private MovementUpdater _movementUpdater;

    private readonly List<MovementAgent> _targets = new(16);
    private readonly HashSet<Mob> _hitMobs = new();
    private readonly Collider[] _destructibleHits = new Collider[16];

    private int _damagableLayerMask;

    protected override void Awake()
    {
      base.Awake();
      PrewarmFx(_hitFxPrefab, _hitFxPrewarmCount);
      _damagableLayerMask = LayerMask.GetMask(DestructibleLayers.Damagable);
    }

    protected override void Attack(int damage)
    {
      _hitMobs.Clear();

      if (_movementUpdater != null)
      {
        _movementUpdater.QueryCircle(
          transform.position,
          _attackRadius,
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
        SpawnFx(_hitFxPrefab, mob.transform.position, mob.transform.rotation);
      }

      var hitCount = Physics.OverlapSphereNonAlloc(
        transform.position,
        _attackRadius,
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
        SpawnFx(_hitFxPrefab, target.transform.position, target.transform.rotation);
        return;
      }

      var components = target.GetComponents<MonoBehaviour>();
      for (var i = 0; i < components.Length; i++)
      {
        if (components[i] is not IDamageable damageable || !damageable.IsAlive)
          continue;

        damageable.TakeDamage(damage);
        SpawnFx(_hitFxPrefab, target.transform.position, target.transform.rotation);
        break;
      }
    }

    private void DamageDestructible(GameObject target, int damage)
    {
      var components = target.GetComponents<MonoBehaviour>();
      for (var i = 0; i < components.Length; i++)
      {
        if (components[i] is not IDamageable damageable || !damageable.IsAlive)
          continue;

        if (damageable is IImpactDamageable impactDamageable)
          impactDamageable.TakeDamage(damage, transform.position);
        else
          damageable.TakeDamage(damage);

        SpawnFx(_hitFxPrefab, target.transform.position, target.transform.rotation);
        break;
      }
    }

    private void OnValidate()
    {
      _attackRadius = Mathf.Max(0f, _attackRadius);
      _attackConeAngle = Mathf.Clamp(_attackConeAngle, 0f, 360f);
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
      if (_attackRadius <= 0f)
        return;

      var origin = transform.position + Vector3.up * 0.05f;
      var forward = GetAttackForward();
      var halfAngle = _attackConeAngle * 0.5f;
      var segmentCount = Mathf.Max(4, Mathf.CeilToInt(_attackConeAngle / 10f));
      var previousPoint = origin
        + (Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward) * _attackRadius;

      Gizmos.color = new Color(1f, 0.65f, 0.1f, 0.9f);
      Gizmos.DrawLine(origin, previousPoint);

      for (var i = 1; i <= segmentCount; i++)
      {
        var angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segmentCount);
        var point = origin
          + (Quaternion.AngleAxis(angle, Vector3.up) * forward) * _attackRadius;
        Gizmos.DrawLine(previousPoint, point);
        previousPoint = point;
      }

      Gizmos.DrawLine(origin, previousPoint);
      Gizmos.DrawLine(origin, origin + forward * _attackRadius);
    }
  }
}
