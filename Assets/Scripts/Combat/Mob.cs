using System;
using UnityEngine;

namespace Combat
{
  [DisallowMultipleComponent]
  public sealed class Mob : MonoBehaviour, IDamageable
  {
    [SerializeField, Min(1)] private int _maxHealth = 1;

    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;

    private void Awake()
    {
      CurrentHealth = _maxHealth;
    }

    public void TakeDamage(int damage)
    {
      if (damage < 0)
        throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage cannot be negative.");

      if (!IsAlive)
        return;

      CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
      if (CurrentHealth == 0)
        gameObject.SetActive(false);
    }

    private void OnValidate()
    {
      _maxHealth = Mathf.Max(1, _maxHealth);
    }
  }
}
