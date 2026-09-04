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

    public event Action StatisticsChanged;

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
      set
      {
        if (SetValue(ref _ducksKilled, value, DucksKilledKey))
          StatisticsChanged?.Invoke();
      }
    }

    public int BuildingsDestroyed
    {
      get => _buildingsDestroyed;
      set
      {
        if (SetValue(ref _buildingsDestroyed, value, BuildingsDestroyedKey))
          StatisticsChanged?.Invoke();
      }
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

    private bool SetValue(ref int field, int value, string key)
    {
      if (field == value)
        return false;

      field = value;
      if (_isInitialized)
        PlayerPrefs.SetInt(key, value);

      return true;
    }
  }
}
