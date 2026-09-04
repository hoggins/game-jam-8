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
          if (target == null || !target.isActiveAndEnabled)
            continue;

          DamageTarget(target, damage);
        }
      }

      var attachedMobs = Object.FindObjectsByType<Mob>(
        FindObjectsInactive.Exclude,
        FindObjectsSortMode.None);
      for (var i = 0; i < attachedMobs.Length; i++)
      {
        var mob = attachedMobs[i];
        if (mob == null || !mob.IsAttached || !mob.IsAlive || !_hitMobs.Add(mob))
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
        DamageDestructible(_destructibleHits[i].gameObject, damage);
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
      _hitFxPrewarmCount = Mathf.Max(0, _hitFxPrewarmCount);
    }
  }
}
