using System.Collections.Generic;
using Combat;
using Movement;
using UnityEngine;
using VContainer;

namespace Weapons
{
  [DisallowMultipleComponent]
  public sealed class MeleeWeapon : Weapon
  {
    [SerializeField, Min(0f)] private float _attackRadius = 2f;

    [Inject] private MovementUpdater _movementUpdater;

    private readonly List<MovementAgent> _targets = new(16);

    protected override void Attack(int damage)
    {
      if (_movementUpdater == null)
        return;

      _movementUpdater.QueryCircle(
        transform.position,
        _attackRadius,
        MovementLayer.Mob,
        _targets);

      for (var i = 0; i < _targets.Count; i++)
      {
        var target = _targets[i];
        if (target == null)
          continue;

        var components = target.GetComponents<MonoBehaviour>();
        for (var j = 0; j < components.Length; j++)
        {
          if (components[j] is not IDamageable damageable || !damageable.IsAlive)
            continue;

          damageable.TakeDamage(damage);
          break;
        }
      }
    }

    private void OnValidate()
    {
      _attackRadius = Mathf.Max(0f, _attackRadius);
    }
  }
}
