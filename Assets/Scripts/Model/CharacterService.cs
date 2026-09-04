using System;
using Balance;

namespace Model
{
  [UnityEngine.Scripting.Preserve]
  public class CharacterService
  {
    private readonly Storage _storage;

    public event Action Died;
    public event Action ProgressionChanged;
    public event Action<int> CoinsChanged;

    public int AttackPower => _storage.AttackPower;
    public int MaxHealth => _storage.MaxHealth;
    public int CurrentCoins => _storage.CurrentCoins;
    public bool CanUpgradeAttackPower => _storage.CurrentCoins >= ProgressionBalance.AttackPowerUpgradeCost;
    public bool CanUpgradeMaxHealth => _storage.CurrentCoins >= ProgressionBalance.MaxHealthUpgradeCost;

    public int CurrentHealth { get; private set; }

    public CharacterService(Storage storage)
    {
      _storage = storage;
    }

    public void StartBattle()
    {
      CurrentHealth = _storage.MaxHealth;
    }

    public void TakeDamage(int damage)
    {
      if (damage < 0)
        throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage cannot be negative.");

      CurrentHealth = Math.Max(0, CurrentHealth - damage);
      if (CurrentHealth > 0)
        return;

      Die();
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
      CurrentHealth = 0;
      Died?.Invoke();
    }
  }
}
