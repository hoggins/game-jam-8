using System;
using Balance;

namespace Model
{
  [UnityEngine.Scripting.Preserve]
  public class CharacterService : IBattleStarted
  {
    private readonly Storage _storage;

    public event Action Died;
    public event Action<int> HealthChanged;
    public event Action HealthDestroyed;
    public event Action ProgressionChanged;
    public event Action<int> CoinsChanged;

    /// Raised with the seconds just bought, so a battle in progress can extend its running clock.
    public event Action<int> TimerBonusAdded;

    public int AttackPower => _storage.AttackPower;
    public int MaxHealth => _storage.MaxHealth;
    public int Speed => _storage.Speed;
    public int GunPower => _storage.GunPower;

    /// Seconds this character adds to the battle clock.
    public int Timer => _storage.Timer;
    public int CurrentCoins => _storage.CurrentCoins;
    public bool IsAlive => CurrentHealth > 0;
    public bool IsInvincible { get; private set; }

    /// Levels are 1-based: a stat sitting at its starting value is level 1, and no stat can pass
    /// <see cref="ProgressionBalance.MaxUpgradeLevel"/>.
    public int AttackPowerLevel => GetLevel(_storage.AttackPower, ProgressionBalance.StartingAttackPower, ProgressionBalance.AttackPowerUpgradeAmount);
    public int MaxHealthLevel => GetLevel(_storage.MaxHealth, ProgressionBalance.StartingMaxHealth, ProgressionBalance.MaxHealthUpgradeAmount);
    public int SpeedLevel => GetLevel(_storage.Speed, ProgressionBalance.StartingSpeed, ProgressionBalance.SpeedUpgradeAmount);
    public int GunPowerLevel => GetLevel(_storage.GunPower, ProgressionBalance.StartingGunPower, ProgressionBalance.GunPowerUpgradeAmount);
    public int TimerLevel => GetLevel(_storage.Timer, ProgressionBalance.StartingTimer, ProgressionBalance.TimerUpgradeAmount);

    public bool IsAttackPowerMaxLevel => AttackPowerLevel >= ProgressionBalance.MaxUpgradeLevel;
    public bool IsMaxHealthMaxLevel => MaxHealthLevel >= ProgressionBalance.MaxUpgradeLevel;
    public bool IsSpeedMaxLevel => SpeedLevel >= ProgressionBalance.MaxUpgradeLevel;
    public bool IsGunPowerMaxLevel => GunPowerLevel >= ProgressionBalance.MaxUpgradeLevel;
    public bool IsTimerMaxLevel => TimerLevel >= ProgressionBalance.MaxUpgradeLevel;

    public bool CanUpgradeAttackPower => !IsAttackPowerMaxLevel && _storage.CurrentCoins >= ProgressionBalance.AttackPowerUpgradeCost;
    public bool CanUpgradeMaxHealth => !IsMaxHealthMaxLevel && _storage.CurrentCoins >= ProgressionBalance.MaxHealthUpgradeCost;
    public bool CanUpgradeSpeed => !IsSpeedMaxLevel && _storage.CurrentCoins >= ProgressionBalance.SpeedUpgradeCost;
    public bool CanUpgradeGunPower => !IsGunPowerMaxLevel && _storage.CurrentCoins >= ProgressionBalance.GunPowerUpgradeCost;
    public bool CanUpgradeTimer => !IsTimerMaxLevel && _storage.CurrentCoins >= ProgressionBalance.TimerUpgradeCost;

    public int CurrentHealth { get; private set; }

    public CharacterService(Storage storage)
    {
      _storage = storage;
    }

    /// Clamped at both ends: a saved value below the current starting value (a lowered balance
    /// number against an existing save) must not read as a negative level.
    private static int GetLevel(int value, int startingValue, int upgradeAmount)
    {
      if (upgradeAmount <= 0)
        return 1;

      var level = (value - startingValue) / upgradeAmount + 1;
      return Math.Clamp(level, 1, ProgressionBalance.MaxUpgradeLevel);
    }

    void IBattleStarted.OnBattleStarted()
    {
      IsInvincible = false;
      CurrentHealth = _storage.MaxHealth;
      HealthChanged?.Invoke(CurrentHealth);
    }

    public void TakeDamage(int damage)
    {
      if (damage < 0)
        throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage cannot be negative.");

      if (IsInvincible || !IsAlive)
        return;

      CurrentHealth = Math.Max(0, CurrentHealth - damage);
      HealthChanged?.Invoke(CurrentHealth);
      if (CurrentHealth > 0)
        return;

      Died?.Invoke();
    }

    public void DestroyHealth()
    {
      if (IsInvincible)
        return;

      IsInvincible = true;
      HealthDestroyed?.Invoke();
    }

    /// Credits the kill and its coin drop immediately, and returns how many coins
    /// dropped so the caller can spawn that many visual pickups.
    public int RegisterDuckKill()
    {
      _storage.DucksKilled += 1;

      var droppedCoins = BattleBalance.RollDuckCoinDrop();
      if (droppedCoins > 0)
        AddCoins(droppedCoins);

      return droppedCoins;
    }

    public void RegisterBuildingDestroyed()
    {
      _storage.BuildingsDestroyed += 1;
    }

    public void AddCoins(int count)
    {
      _storage.CurrentCoins += count;
      CoinsChanged?.Invoke(_storage.CurrentCoins);
    }

    public void UpgradeAttackPower()
    {
      if (IsAttackPowerMaxLevel)
        throw new InvalidOperationException($"AttackPower is already at max level {ProgressionBalance.MaxUpgradeLevel}.");

      if (_storage.CurrentCoins < ProgressionBalance.AttackPowerUpgradeCost)
        throw new InvalidOperationException($"Not enough coins to upgrade attack power. Current coins: {_storage.CurrentCoins}, required: {ProgressionBalance.AttackPowerUpgradeCost}");

      _storage.CurrentCoins -= ProgressionBalance.AttackPowerUpgradeCost;
      _storage.AttackPower += ProgressionBalance.AttackPowerUpgradeAmount;
      ProgressionChanged?.Invoke();
      CoinsChanged?.Invoke(_storage.CurrentCoins);
    }

    public void UpgradeMaxHealth()
    {
      if (IsMaxHealthMaxLevel)
        throw new InvalidOperationException($"MaxHealth is already at max level {ProgressionBalance.MaxUpgradeLevel}.");

      if (_storage.CurrentCoins < ProgressionBalance.MaxHealthUpgradeCost)
        throw new InvalidOperationException($"Not enough coins to upgrade max health. Current coins: {_storage.CurrentCoins}, required: {ProgressionBalance.MaxHealthUpgradeCost}");

      _storage.CurrentCoins -= ProgressionBalance.MaxHealthUpgradeCost;
      _storage.MaxHealth += ProgressionBalance.MaxHealthUpgradeAmount;
      ProgressionChanged?.Invoke();
      CoinsChanged?.Invoke(_storage.CurrentCoins);
    }

    public void UpgradeSpeed()
    {
      if (IsSpeedMaxLevel)
        throw new InvalidOperationException($"Speed is already at max level {ProgressionBalance.MaxUpgradeLevel}.");

      if (_storage.CurrentCoins < ProgressionBalance.SpeedUpgradeCost)
        throw new InvalidOperationException($"Not enough coins to upgrade speed. Current coins: {_storage.CurrentCoins}, required: {ProgressionBalance.SpeedUpgradeCost}");

      _storage.CurrentCoins -= ProgressionBalance.SpeedUpgradeCost;
      _storage.Speed += ProgressionBalance.SpeedUpgradeAmount;
      ProgressionChanged?.Invoke();
      CoinsChanged?.Invoke(_storage.CurrentCoins);
    }

    public void UpgradeGunPower()
    {
      if (IsGunPowerMaxLevel)
        throw new InvalidOperationException($"GunPower is already at max level {ProgressionBalance.MaxUpgradeLevel}.");

      if (_storage.CurrentCoins < ProgressionBalance.GunPowerUpgradeCost)
        throw new InvalidOperationException($"Not enough coins to upgrade gun power. Current coins: {_storage.CurrentCoins}, required: {ProgressionBalance.GunPowerUpgradeCost}");

      _storage.CurrentCoins -= ProgressionBalance.GunPowerUpgradeCost;
      _storage.GunPower += ProgressionBalance.GunPowerUpgradeAmount;
      ProgressionChanged?.Invoke();
      CoinsChanged?.Invoke(_storage.CurrentCoins);
    }

    public void UpgradeTimer()
    {
      if (IsTimerMaxLevel)
        throw new InvalidOperationException($"Timer is already at max level {ProgressionBalance.MaxUpgradeLevel}.");

      if (_storage.CurrentCoins < ProgressionBalance.TimerUpgradeCost)
        throw new InvalidOperationException($"Not enough coins to upgrade timer. Current coins: {_storage.CurrentCoins}, required: {ProgressionBalance.TimerUpgradeCost}");

      _storage.CurrentCoins -= ProgressionBalance.TimerUpgradeCost;
      _storage.Timer += ProgressionBalance.TimerUpgradeAmount;
      ProgressionChanged?.Invoke();
      CoinsChanged?.Invoke(_storage.CurrentCoins);
      TimerBonusAdded?.Invoke(ProgressionBalance.TimerUpgradeAmount);
    }

    public void Die()
    {
      if (!IsAlive)
        return;

      CurrentHealth = 0;
      HealthChanged?.Invoke(CurrentHealth);
      Died?.Invoke();
    }
  }
}
