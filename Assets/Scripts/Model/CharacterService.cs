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

    public int AttackPower => _storage.AttackPower;
    public int MaxHealth => _storage.MaxHealth;
    public int CurrentCoins => _storage.CurrentCoins;
    public bool IsAlive => CurrentHealth > 0;
    public bool IsInvincible { get; private set; }
    public bool CanUpgradeAttackPower => _storage.CurrentCoins >= ProgressionBalance.AttackPowerUpgradeCost;
    public bool CanUpgradeMaxHealth => _storage.CurrentCoins >= ProgressionBalance.MaxHealthUpgradeCost;

    public int CurrentHealth { get; private set; }

    public CharacterService(Storage storage)
    {
      _storage = storage;
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
      if (_storage.CurrentCoins < ProgressionBalance.AttackPowerUpgradeCost)
        throw new InvalidOperationException($"Not enough coins to upgrade attack power. Current coins: {_storage.CurrentCoins}, required: {ProgressionBalance.AttackPowerUpgradeCost}");

      _storage.CurrentCoins -= ProgressionBalance.AttackPowerUpgradeCost;
      _storage.AttackPower += ProgressionBalance.AttackPowerUpgradeAmount;
      ProgressionChanged?.Invoke();
      CoinsChanged?.Invoke(_storage.CurrentCoins);
    }

    public void UpgradeMaxHealth()
    {
      if (_storage.CurrentCoins < ProgressionBalance.MaxHealthUpgradeCost)
        throw new InvalidOperationException($"Not enough coins to upgrade max health. Current coins: {_storage.CurrentCoins}, required: {ProgressionBalance.MaxHealthUpgradeCost}");

      _storage.CurrentCoins -= ProgressionBalance.MaxHealthUpgradeCost;
      _storage.MaxHealth += ProgressionBalance.MaxHealthUpgradeAmount;
      ProgressionChanged?.Invoke();
      CoinsChanged?.Invoke(_storage.CurrentCoins);
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
