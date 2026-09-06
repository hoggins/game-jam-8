using System;
using Balance;
using App;
using Combat;
using Telemetry;
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
    private const int MinPartsPerFallout = 1;
    private const int MaxPartsPerFallout = 1;

    [SerializeField] private DestructibleObjectType _objectType;
    [SerializeField, Range(0f, 1f)]
    private float _minPartFalloutHealthPercentage = MinPartFalloutHealthPercentage;
    [SerializeField, Range(0f, 1f)]
    private float _maxPartFalloutHealthPercentage = MaxPartFalloutHealthPercentage;
    [SerializeField, Min(1)] private int _minPartsPerFallout = MinPartsPerFallout;
    [SerializeField, Min(1)] private int _maxPartsPerFallout = MaxPartsPerFallout;

    [Inject] private BattleBalanceConfig _battleBalance;
    [Inject] private EconomyTelemetryService _telemetry;

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
      _maxHealth = _objectType == DestructibleObjectType.House
        ? _battleBalance.GetHouseMaxHealth(_destructibleObject.HouseDifficultyLevel)
        : _battleBalance.GetDestructibleMaxHealth(_objectType);
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

      if (_objectType == DestructibleObjectType.House)
        _telemetry?.RecordBuildingHit();

      CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

      if (_hitFx != null)
        _hitFx.PlayHit();

      var damageTaken = _maxHealth - CurrentHealth;
      while (CurrentHealth > 0
        && damageTaken >= _nextPartFalloutDamage
        && FallOutRandomParts(origin))
      {
        _nextPartFalloutDamage += RollPartFalloutDamage();
      }

      if (CurrentHealth == 0)
      {
        _isDestroyed = true;

        _destructibleObject.Break(origin);
      }
    }

    private float RollPartFalloutDamage() =>
      _maxHealth * UnityEngine.Random.Range(
        _minPartFalloutHealthPercentage,
        _maxPartFalloutHealthPercentage);

    private bool FallOutRandomParts(Vector3 origin)
    {
      var partCount = UnityEngine.Random.Range(_minPartsPerFallout, _maxPartsPerFallout + 1);
      var fellOutPart = false;
      for (var i = 0; i < partCount; i++)
      {
        if (!_destructibleObject.FallOutRandomPart(origin))
          break;

        fellOutPart = true;
      }

      return fellOutPart;
    }

    private void OnValidate()
    {
      _minPartFalloutHealthPercentage = Mathf.Clamp01(_minPartFalloutHealthPercentage);
      _maxPartFalloutHealthPercentage = Mathf.Clamp(
        _maxPartFalloutHealthPercentage,
        _minPartFalloutHealthPercentage,
        1f);
      _minPartsPerFallout = Mathf.Max(1, _minPartsPerFallout);
      _maxPartsPerFallout = Mathf.Max(_minPartsPerFallout, _maxPartsPerFallout);
    }
  }
}
