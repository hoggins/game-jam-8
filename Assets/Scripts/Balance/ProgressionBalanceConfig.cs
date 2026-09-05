using UnityEngine;

namespace Balance
{
  [CreateAssetMenu(fileName = "ProgressionBalanceConfig", menuName = "Game/Progression Balance Config")]
  public sealed class ProgressionBalanceConfig : ScriptableObject
  {
    [Header("Starting Stats")]
    [SerializeField, Min(0)] private int _startingAttackPower = 1;
    [SerializeField, Min(0)] private int _startingMaxHealth = 12;
    [SerializeField, Min(0)] private int _startingSpeed = 6;
    [SerializeField, Min(0)] private int _startingGunPower = 1;
    [Tooltip("Seconds added to the battle clock on top of BattleBalanceConfig.BattleDuration.")]
    [SerializeField, Min(0)] private int _startingTimer;

    [Header("Upgrade Cost")]
    [SerializeField, Min(0)] private int _attackPowerUpgradeCost = 100;
    [SerializeField, Min(0)] private int _maxHealthUpgradeCost = 100;
    [SerializeField, Min(0)] private int _speedUpgradeCost = 100;
    [SerializeField, Min(0)] private int _gunPowerUpgradeCost = 100;
    [SerializeField, Min(0)] private int _timerUpgradeCost = 100;

    [Header("Upgrade Amount")]
    [SerializeField, Min(0)] private int _attackPowerUpgradeAmount = 1;
    [SerializeField, Min(0)] private int _maxHealthUpgradeAmount = 1;
    [SerializeField, Min(0)] private int _speedUpgradeAmount = 1;
    [SerializeField, Min(0)] private int _gunPowerUpgradeAmount = 1;
    [SerializeField, Min(0)] private int _timerUpgradeAmount = 1;

    [Header("Level Cap")]
    [Tooltip("Highest level any stat can reach. Levels are displayed from 1, so this allows "
      + "MaxUpgradeLevel - 1 purchases.")]
    [SerializeField, Min(1)] private int _maxUpgradeLevel = 30;

    [Tooltip("The player grows by this fraction of its authored size per max-health level.")]
    [SerializeField, Min(0f)] private float _maxHealthScalePerLevel = 0.02f;

    public int StartingAttackPower => _startingAttackPower;
    public int StartingMaxHealth => _startingMaxHealth;
    public int StartingSpeed => _startingSpeed;
    public int StartingGunPower => _startingGunPower;
    public int StartingTimer => _startingTimer;

    public int AttackPowerUpgradeCost => _attackPowerUpgradeCost;
    public int AttackPowerUpgradeAmount => _attackPowerUpgradeAmount;
    public int MaxHealthUpgradeCost => _maxHealthUpgradeCost;
    public int MaxHealthUpgradeAmount => _maxHealthUpgradeAmount;
    public int SpeedUpgradeCost => _speedUpgradeCost;
    public int SpeedUpgradeAmount => _speedUpgradeAmount;
    public int GunPowerUpgradeCost => _gunPowerUpgradeCost;
    public int GunPowerUpgradeAmount => _gunPowerUpgradeAmount;
    public int TimerUpgradeCost => _timerUpgradeCost;
    public int TimerUpgradeAmount => _timerUpgradeAmount;

    public int MaxUpgradeLevel => _maxUpgradeLevel;
    public float MaxHealthScalePerLevel => _maxHealthScalePerLevel;

    private void OnValidate()
    {
      _startingAttackPower = Mathf.Max(0, _startingAttackPower);
      _startingMaxHealth = Mathf.Max(0, _startingMaxHealth);
      _startingSpeed = Mathf.Max(0, _startingSpeed);
      _startingGunPower = Mathf.Max(0, _startingGunPower);
      _startingTimer = Mathf.Max(0, _startingTimer);

      _attackPowerUpgradeCost = Mathf.Max(0, _attackPowerUpgradeCost);
      _maxHealthUpgradeCost = Mathf.Max(0, _maxHealthUpgradeCost);
      _speedUpgradeCost = Mathf.Max(0, _speedUpgradeCost);
      _gunPowerUpgradeCost = Mathf.Max(0, _gunPowerUpgradeCost);
      _timerUpgradeCost = Mathf.Max(0, _timerUpgradeCost);

      _attackPowerUpgradeAmount = Mathf.Max(0, _attackPowerUpgradeAmount);
      _maxHealthUpgradeAmount = Mathf.Max(0, _maxHealthUpgradeAmount);
      _speedUpgradeAmount = Mathf.Max(0, _speedUpgradeAmount);
      _gunPowerUpgradeAmount = Mathf.Max(0, _gunPowerUpgradeAmount);
      _timerUpgradeAmount = Mathf.Max(0, _timerUpgradeAmount);

      _maxUpgradeLevel = Mathf.Max(1, _maxUpgradeLevel);
      _maxHealthScalePerLevel = Mathf.Max(0f, _maxHealthScalePerLevel);
    }
  }
}
