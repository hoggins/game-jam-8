using System;
using Balance;

namespace Model
{
  public enum UpgradeStat
  {
    Attack,
    Health,
    Speed,
    Gun,
    Timer,
  }

  [UnityEngine.Scripting.Preserve]
  public class CharacterService : IBattleStarted
  {
    private readonly Storage _storage;
    private readonly ProgressionBalanceConfig _progressionBalance;
    private readonly BattleBalanceConfig _battleBalance;
    private bool _isVictoryProtected;

    public event Action Died;
    public event Action Damaged;
    public event Action<int> HealthChanged;
    public event Action HealthDestroyed;
    public event Action DuckKilled;
    public event Action BuildingDestroyed;
    public event Action ProgressionChanged;
    public event Action<int> CoinsChanged;
    public event Action<int> MobCoinsEarned;
    public event Action<int> BuildingCoinsEarned;
    public event Action<int, bool> DamageTaken;
    public event Action MobConnected;
    public event Action<UpgradeStat, int> UpgradePurchased;

    /// Raised with the seconds just bought, so a battle in progress can extend its running clock.
    public event Action<int> TimerBonusAdded;

    public int AttackPower => _storage.AttackPower;
    public int MaxHealth => _storage.MaxHealth;
    public int Speed => _storage.Speed;
    public int GunPower => _storage.GunPower;

    /// Scale applied to the character and its size-dependent attacks.
    public float CharacterScaleFactor =>
      1f + Math.Max(0, MaxHealthLevel - 1) * _progressionBalance.MaxHealthScalePerLevel;

    /// Seconds this character adds to the battle clock.
    public int Timer => _storage.Timer;
    public int CurrentCoins => _storage.CurrentCoins;
    public bool IsAlive => CurrentHealth > 0;
    public bool IsInvincible { get; private set; }

    /// Levels are 1-based: a stat sitting at its starting value is level 1, and no stat can pass
    /// <see cref="ProgressionBalanceConfig.MaxUpgradeLevel"/>.
    public int AttackPowerLevel => GetLevel(_storage.AttackPower, _progressionBalance.StartingAttackPower, _progressionBalance.AttackPowerUpgradeAmount);
    public int MaxHealthLevel => GetLevel(_storage.MaxHealth, _progressionBalance.StartingMaxHealth, _progressionBalance.MaxHealthUpgradeAmount);
    public int SpeedLevel => GetLevel(_storage.Speed, _progressionBalance.StartingSpeed, _progressionBalance.SpeedUpgradeAmount);
    public int GunPowerLevel => GetLevel(_storage.GunPower, _progressionBalance.StartingGunPower, _progressionBalance.GunPowerUpgradeAmount);
    public int TimerLevel => GetLevel(_storage.Timer, _progressionBalance.StartingTimer, _progressionBalance.TimerUpgradeAmount);

    public bool IsAttackPowerMaxLevel => AttackPowerLevel >= _progressionBalance.MaxUpgradeLevel;
    public bool IsMaxHealthMaxLevel => MaxHealthLevel >= _progressionBalance.MaxUpgradeLevel;
    public bool IsSpeedMaxLevel => SpeedLevel >= _progressionBalance.MaxUpgradeLevel;
    public bool IsGunPowerMaxLevel => GunPowerLevel >= _progressionBalance.MaxUpgradeLevel;
    public bool IsTimerMaxLevel => TimerLevel >= _progressionBalance.MaxUpgradeLevel;

    public bool CanUpgradeAttackPower => !IsAttackPowerMaxLevel && _storage.CurrentCoins >= _progressionBalance.AttackPowerUpgradeCost;
    public bool CanUpgradeMaxHealth => !IsMaxHealthMaxLevel && _storage.CurrentCoins >= _progressionBalance.MaxHealthUpgradeCost;
    public bool CanUpgradeSpeed => !IsSpeedMaxLevel && _storage.CurrentCoins >= _progressionBalance.SpeedUpgradeCost;
    public bool CanUpgradeGunPower => !IsGunPowerMaxLevel && _storage.CurrentCoins >= _progressionBalance.GunPowerUpgradeCost;
    public bool CanUpgradeTimer => !IsTimerMaxLevel && _storage.CurrentCoins >= _progressionBalance.TimerUpgradeCost;

    public int CurrentHealth { get; private set; }

    public CharacterService(Storage storage, ProgressionBalanceConfig progressionBalance, BattleBalanceConfig battleBalance)
    {
      _storage = storage;
      _progressionBalance = progressionBalance;
      _battleBalance = battleBalance;
    }

    /// Clamped at both ends: a saved value below the current starting value (a lowered balance
    /// number against an existing save) must not read as a negative level.
    private int GetLevel(int value, int startingValue, int upgradeAmount)
    {
      if (upgradeAmount <= 0)
        return 1;

      var level = (value - startingValue) / upgradeAmount + 1;
      return Math.Clamp(level, 1, _progressionBalance.MaxUpgradeLevel);
    }

    void IBattleStarted.OnBattleStarted()
    {
      _isVictoryProtected = false;
      IsInvincible = false;
      CurrentHealth = _storage.MaxHealth;
      HealthChanged?.Invoke(CurrentHealth);
    }

    internal void BeginVictoryProtection() =>
      _isVictoryProtected = true;

    internal void EndVictoryProtection() =>
      _isVictoryProtected = false;

    public void TakeDamage(int damage) =>
      TakeDamage(damage, false);

    public void TakeDamage(int damage, bool fromMob)
    {
      if (damage < 0)
        throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage cannot be negative.");

      if (IsInvincible || _isVictoryProtected || !IsAlive)
        return;

      if (fromMob)
        MobConnected?.Invoke();

      var previousHealth = CurrentHealth;
      CurrentHealth = Math.Max(0, CurrentHealth - damage);
      HealthChanged?.Invoke(CurrentHealth);
      if (CurrentHealth < previousHealth)
      {
        Damaged?.Invoke();
        DamageTaken?.Invoke(previousHealth - CurrentHealth, fromMob);
      }
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
      DuckKilled?.Invoke();

      var droppedCoins = _battleBalance.RollDuckCoinDrop();
      if (droppedCoins > 0)
      {
        AddCoins(droppedCoins);
        MobCoinsEarned?.Invoke(droppedCoins);
      }

      return droppedCoins;
    }

    /// Credits the building destruction and its coin drop immediately, and returns how many coins
    /// dropped so the caller can spawn that many visual pickups.
    public int RegisterBuildingDestroyed()
    {
      _storage.BuildingsDestroyed += 1;
      BuildingDestroyed?.Invoke();

      var droppedCoins = _battleBalance.RollBuildingCoinDrop();
      if (droppedCoins > 0)
      {
        AddCoins(droppedCoins);
        BuildingCoinsEarned?.Invoke(droppedCoins);
      }

      return droppedCoins;
    }

    public void AddCoins(int count)
    {
      _storage.CurrentCoins += count;
      CoinsChanged?.Invoke(_storage.CurrentCoins);
    }

    /// Wipes saved progression and refreshes the values currently shown by battle and metagame UI.
    public void ResetProgression()
    {
      _storage.Reset();
      CurrentHealth = Math.Min(CurrentHealth, MaxHealth);
      ProgressionChanged?.Invoke();
      CoinsChanged?.Invoke(CurrentCoins);
      HealthChanged?.Invoke(CurrentHealth);
    }

    public void UpgradeAttackPower()
    {
      if (IsAttackPowerMaxLevel)
        throw new InvalidOperationException($"AttackPower is already at max level {_progressionBalance.MaxUpgradeLevel}.");

      if (_storage.CurrentCoins < _progressionBalance.AttackPowerUpgradeCost)
        throw new InvalidOperationException($"Not enough coins to upgrade attack power. Current coins: {_storage.CurrentCoins}, required: {_progressionBalance.AttackPowerUpgradeCost}");

      _storage.CurrentCoins -= _progressionBalance.AttackPowerUpgradeCost;
      _storage.AttackPower += _progressionBalance.AttackPowerUpgradeAmount;
      ProgressionChanged?.Invoke();
      CoinsChanged?.Invoke(_storage.CurrentCoins);
      UpgradePurchased?.Invoke(UpgradeStat.Attack, _progressionBalance.AttackPowerUpgradeCost);
    }

    public void UpgradeMaxHealth()
    {
      if (IsMaxHealthMaxLevel)
        throw new InvalidOperationException($"MaxHealth is already at max level {_progressionBalance.MaxUpgradeLevel}.");

      if (_storage.CurrentCoins < _progressionBalance.MaxHealthUpgradeCost)
        throw new InvalidOperationException($"Not enough coins to upgrade max health. Current coins: {_storage.CurrentCoins}, required: {_progressionBalance.MaxHealthUpgradeCost}");

      _storage.CurrentCoins -= _progressionBalance.MaxHealthUpgradeCost;
      var healthIncrease = _progressionBalance.MaxHealthUpgradeAmount;
      _storage.MaxHealth += healthIncrease;
      CurrentHealth = Math.Min(_storage.MaxHealth, CurrentHealth + healthIncrease);
      HealthChanged?.Invoke(CurrentHealth);
      ProgressionChanged?.Invoke();
      CoinsChanged?.Invoke(_storage.CurrentCoins);
      UpgradePurchased?.Invoke(UpgradeStat.Health, _progressionBalance.MaxHealthUpgradeCost);
    }

    public void UpgradeSpeed()
    {
      if (IsSpeedMaxLevel)
        throw new InvalidOperationException($"Speed is already at max level {_progressionBalance.MaxUpgradeLevel}.");

      if (_storage.CurrentCoins < _progressionBalance.SpeedUpgradeCost)
        throw new InvalidOperationException($"Not enough coins to upgrade speed. Current coins: {_storage.CurrentCoins}, required: {_progressionBalance.SpeedUpgradeCost}");

      _storage.CurrentCoins -= _progressionBalance.SpeedUpgradeCost;
      _storage.Speed += _progressionBalance.SpeedUpgradeAmount;
      ProgressionChanged?.Invoke();
      CoinsChanged?.Invoke(_storage.CurrentCoins);
      UpgradePurchased?.Invoke(UpgradeStat.Speed, _progressionBalance.SpeedUpgradeCost);
    }

    public void UpgradeGunPower()
    {
      if (IsGunPowerMaxLevel)
        throw new InvalidOperationException($"GunPower is already at max level {_progressionBalance.MaxUpgradeLevel}.");

      if (_storage.CurrentCoins < _progressionBalance.GunPowerUpgradeCost)
        throw new InvalidOperationException($"Not enough coins to upgrade gun power. Current coins: {_storage.CurrentCoins}, required: {_progressionBalance.GunPowerUpgradeCost}");

      _storage.CurrentCoins -= _progressionBalance.GunPowerUpgradeCost;
      _storage.GunPower += _progressionBalance.GunPowerUpgradeAmount;
      ProgressionChanged?.Invoke();
      CoinsChanged?.Invoke(_storage.CurrentCoins);
      UpgradePurchased?.Invoke(UpgradeStat.Gun, _progressionBalance.GunPowerUpgradeCost);
    }

    public void UpgradeTimer()
    {
      if (IsTimerMaxLevel)
        throw new InvalidOperationException($"Timer is already at max level {_progressionBalance.MaxUpgradeLevel}.");

      if (_storage.CurrentCoins < _progressionBalance.TimerUpgradeCost)
        throw new InvalidOperationException($"Not enough coins to upgrade timer. Current coins: {_storage.CurrentCoins}, required: {_progressionBalance.TimerUpgradeCost}");

      _storage.CurrentCoins -= _progressionBalance.TimerUpgradeCost;
      _storage.Timer += _progressionBalance.TimerUpgradeAmount;
      ProgressionChanged?.Invoke();
      CoinsChanged?.Invoke(_storage.CurrentCoins);
      TimerBonusAdded?.Invoke(_progressionBalance.TimerUpgradeAmount);
      UpgradePurchased?.Invoke(UpgradeStat.Timer, _progressionBalance.TimerUpgradeCost);
    }

    public void Die()
    {
      if (_isVictoryProtected || !IsAlive)
        return;

      CurrentHealth = 0;
      HealthChanged?.Invoke(CurrentHealth);
      Died?.Invoke();
    }
  }
}
