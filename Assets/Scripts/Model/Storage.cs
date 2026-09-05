using System;
using Balance;
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
    private const string SpeedKey = "Player.Speed";
    private const string GunPowerKey = "Player.GunPower";
    private const string TimerKey = "Player.Timer";

    private static readonly string[] AllKeys =
    {
      AttackPowerKey, MaxHealthKey, DucksKilledKey, BuildingsDestroyedKey,
      CurrentCoinsKey, SpeedKey, GunPowerKey, TimerKey,
    };

    private int _attackPower;
    private int _maxHealth;
    private int _ducksKilled;
    private int _buildingsDestroyed;
    private int _currentCoins;
    private int _speed;
    private int _gunPower;
    private int _timer;
    private bool _isInitialized;

    private readonly ProgressionBalanceConfig _progressionBalance;

    public Storage(ProgressionBalanceConfig progressionBalance)
    {
      _progressionBalance = progressionBalance;
    }

    public event Action StatisticsChanged;

    /// True when at least one player value has ever been written to disk.
    public bool HasSavedProgress
    {
      get
      {
        foreach (var key in AllKeys)
        {
          if (PlayerPrefs.HasKey(key))
            return true;
        }

        return false;
      }
    }

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

    public int Speed
    {
      get => _speed;
      set => SetValue(ref _speed, value, SpeedKey);
    }

    public int GunPower
    {
      get => _gunPower;
      set => SetValue(ref _gunPower, value, GunPowerKey);
    }

    public int Timer
    {
      get => _timer;
      set => SetValue(ref _timer, value, TimerKey);
    }

    /// Wipes every stored player value and reloads the balance defaults.
    public void Reset()
    {
      foreach (var key in AllKeys)
        PlayerPrefs.DeleteKey(key);

      PlayerPrefs.Save();
      Load();
      StatisticsChanged?.Invoke();
    }

    void IInitializable.Initialize()
    {
      Load();
      _isInitialized = true;
    }

    private void Load()
    {
      _attackPower = PlayerPrefs.GetInt(AttackPowerKey, _progressionBalance.StartingAttackPower);
      _maxHealth = PlayerPrefs.GetInt(MaxHealthKey, _progressionBalance.StartingMaxHealth);
      _ducksKilled = PlayerPrefs.GetInt(DucksKilledKey, 0);
      _buildingsDestroyed = PlayerPrefs.GetInt(BuildingsDestroyedKey, 0);
      _currentCoins = PlayerPrefs.GetInt(CurrentCoinsKey, 0);
      _speed = PlayerPrefs.GetInt(SpeedKey, _progressionBalance.StartingSpeed);
      _gunPower = PlayerPrefs.GetInt(GunPowerKey, _progressionBalance.StartingGunPower);
      _timer = PlayerPrefs.GetInt(TimerKey, _progressionBalance.StartingTimer);
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
