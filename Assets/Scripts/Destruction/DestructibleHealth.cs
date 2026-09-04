using System;
using Combat;
using UnityEngine;

namespace Destruction
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(DestructibleObject))]
  public sealed class DestructibleHealth : MonoBehaviour, IImpactDamageable
  {
    [SerializeField, Min(1)] private int _maxHealth = 1;

    private DestructibleObject _destructibleObject;
    private bool _isDestroyed;

    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0 && !_isDestroyed;

    private void Awake()
    {
      _destructibleObject = GetComponent<DestructibleObject>();
      CurrentHealth = _maxHealth;
    }

    public void TakeDamage(int damage) =>
      TakeDamage(damage, transform.position);

    public void TakeDamage(int damage, Vector3 origin)
    {
      if (damage < 0)
        throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage cannot be negative.");

      if (!IsAlive)
        return;

      CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
      if (CurrentHealth == 0)
      {
        _isDestroyed = true;
        _destructibleObject.Break(origin);
      }
    }

    private void OnValidate() =>
      _maxHealth = Mathf.Max(1, _maxHealth);
  }
}
