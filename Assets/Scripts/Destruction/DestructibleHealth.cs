using System;
using Balance;
using App;
using Combat;
using Model;
using UnityEngine;
using VContainer;

namespace Destruction
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(DestructibleObject))]
  public sealed class DestructibleHealth : MonoBehaviour, IImpactDamageable
  {
    private const float MinPartFalloutHealthPercentage = 0.25f;
    private const float MaxPartFalloutHealthPercentage = 0.35f;

    [SerializeField] private DestructibleObjectType _objectType;

    [Inject] private CharacterService _characterService;
    [Inject] private BattleBalanceConfig _battleBalance;

    private DestructibleObject _destructibleObject;
    private HitFx _hitFx;
    private int _maxHealth;
    private float _nextPartFalloutDamage;
    private bool _isDestroyed;

    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0 && !_isDestroyed;
    public DestructibleObjectType ObjectType => _objectType;

    private void Awake()
    {
      this.AsInjected();
      _destructibleObject = GetComponent<DestructibleObject>();
      _hitFx = GetComponent<HitFx>();
      _maxHealth = _battleBalance.GetDestructibleMaxHealth(_objectType);
      CurrentHealth = _maxHealth;
      _nextPartFalloutDamage = RollPartFalloutDamage();
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

      if (_hitFx != null)
        _hitFx.PlayHit();

      var damageTaken = _maxHealth - CurrentHealth;
      while (CurrentHealth > 0
        && damageTaken >= _nextPartFalloutDamage
        && _destructibleObject.FallOutRandomPart(origin))
      {
        _nextPartFalloutDamage += RollPartFalloutDamage();
      }

      if (CurrentHealth == 0)
      {
        _isDestroyed = true;

        // Only actual buildings count towards the run statistic; the battle timer's digits are
        // destructible too but are not buildings.
        if (_objectType == DestructibleObjectType.House)
          _characterService?.RegisterBuildingDestroyed();

        _destructibleObject.Break(origin);
      }
    }

    private float RollPartFalloutDamage() =>
      _maxHealth * UnityEngine.Random.Range(
        MinPartFalloutHealthPercentage,
        MaxPartFalloutHealthPercentage);
  }
}
