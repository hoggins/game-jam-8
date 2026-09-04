using System;
using UnityEngine;
using UnityEngine.Scripting;
using VContainer.Unity;

namespace Model
{
  [Preserve]
  public class Storage : IInitializable, IDisposable
  {
    private const string AttackPowerKey = "Player.AttackPower";
    private const string MaxHealthKey = "Player.MaxHealth";
    private const string DucksKilledKey = "Player.DucksKilled";
    private const string BuildingsDestroyedKey = "Player.BuildingsDestroyed";
    private const string CurrentCoinsKey = "Player.CurrentCoins";

    private int _attackPower;
    private int _maxHealth;
    private int _ducksKilled;
    private int _buildingsDestroyed;
    private int _currentCoins;
    private bool _isInitialized;

    public int AttackPower
    {
      get => _attackPower;
      set => SetValue(ref _attackPower, value, AttackPowerKey);
    }

    public int MaxHealth
    {
      get => _maxHealth;
      set => SetValue(ref _maxHealth, value, MaxHealthKey);
    }

    public int DucksKilled
    {
      get => _ducksKilled;
      set => SetValue(ref _ducksKilled, value, DucksKilledKey);
    }

    public int BuildingsDestroyed
    {
      get => _buildingsDestroyed;
      set => SetValue(ref _buildingsDestroyed, value, BuildingsDestroyedKey);
    }

    public int CurrentCoins
    {
      get => _currentCoins;
      set => SetValue(ref _currentCoins, value, CurrentCoinsKey);
    }

    void IInitializable.Initialize()
    {
      _attackPower = PlayerPrefs.GetInt(AttackPowerKey, 1);
      _maxHealth = PlayerPrefs.GetInt(MaxHealthKey, 1);
      _ducksKilled = PlayerPrefs.GetInt(DucksKilledKey, 0);
      _buildingsDestroyed = PlayerPrefs.GetInt(BuildingsDestroyedKey, 0);
      _currentCoins = PlayerPrefs.GetInt(CurrentCoinsKey, 0);
      _isInitialized = true;
    }

    void IDisposable.Dispose()
    {
      PlayerPrefs.Save();
      _isInitialized = false;
    }

    private void SetValue(ref int field, int value, string key)
    {
      if (field == value)
        return;

      field = value;
      if (_isInitialized)
        PlayerPrefs.SetInt(key, value);
    }
  }
}
