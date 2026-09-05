using System;
using App;
using Balance;
using Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Metagame.Stats
{
  public enum CharacterStatType
  {
    AttackPower,
    MaxHealth,
    Speed,
    GunPower,
    Timer,
  }

  public class ProgressionItemUi : MonoBehaviour
  {
    [SerializeField] private CharacterStatType _statType;
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _valueText;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private TMP_Text _levelText;

    [Inject] private readonly CharacterService _characterService;

    private void Awake()
    {
      this.AsInjected();
    }

    private void OnEnable()
    {
      _upgradeButton.onClick.AddListener(UpgradeClick);
      _characterService.ProgressionChanged += Refresh;
      Refresh();
    }

    private void OnDisable()
    {
      _upgradeButton.onClick.RemoveListener(UpgradeClick);
      _characterService.ProgressionChanged -= Refresh;
    }

    private void UpgradeClick()
    {
      switch (_statType)
      {
        case CharacterStatType.AttackPower:
          _characterService.UpgradeAttackPower();
          break;
        case CharacterStatType.MaxHealth:
          _characterService.UpgradeMaxHealth();
          break;
        case CharacterStatType.Speed:
          _characterService.UpgradeSpeed();
          break;
        case CharacterStatType.GunPower:
          _characterService.UpgradeGunPower();
          break;
        case CharacterStatType.Timer:
          _characterService.UpgradeTimer();
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(_statType), _statType, "Unhandled character stat.");
      }
    }

    private void Refresh()
    {
      _nameText.text = GetName();
      var bonus = GetBonus();
      _valueText.text = bonus > 0 ? $"+{bonus}" : "NO BONUS";
      _priceText.text = $"{GetUpgradeCost()} COINS";
      _levelText.text = GetIsMaxLevel() ? "MAX LVL" : $"LVL {GetLevel()}";
      _upgradeButton.interactable = GetCanUpgrade();
    }

    private string GetName() =>
      _statType switch
      {
        CharacterStatType.AttackPower => "attack",
        CharacterStatType.MaxHealth => "max hp",
        CharacterStatType.Speed => "speed",
        CharacterStatType.GunPower => "gun",
        CharacterStatType.Timer => "timer",
        _ => string.Empty,
      };

    /// How much the stat gained over its starting value.
    private int GetBonus() =>
      GetValue() - GetStartingValue();

    private int GetLevel() =>
      _statType switch
      {
        CharacterStatType.AttackPower => _characterService.AttackPowerLevel,
        CharacterStatType.MaxHealth => _characterService.MaxHealthLevel,
        CharacterStatType.Speed => _characterService.SpeedLevel,
        CharacterStatType.GunPower => _characterService.GunPowerLevel,
        CharacterStatType.Timer => _characterService.TimerLevel,
        _ => 1,
      };

    private bool GetIsMaxLevel() =>
      _statType switch
      {
        CharacterStatType.AttackPower => _characterService.IsAttackPowerMaxLevel,
        CharacterStatType.MaxHealth => _characterService.IsMaxHealthMaxLevel,
        CharacterStatType.Speed => _characterService.IsSpeedMaxLevel,
        CharacterStatType.GunPower => _characterService.IsGunPowerMaxLevel,
        CharacterStatType.Timer => _characterService.IsTimerMaxLevel,
        _ => false,
      };

    private int GetValue() =>
      _statType switch
      {
        CharacterStatType.AttackPower => _characterService.AttackPower,
        CharacterStatType.MaxHealth => _characterService.MaxHealth,
        CharacterStatType.Speed => _characterService.Speed,
        CharacterStatType.GunPower => _characterService.GunPower,
        CharacterStatType.Timer => _characterService.Timer,
        _ => 0,
      };

    private int GetStartingValue() =>
      _statType switch
      {
        CharacterStatType.AttackPower => ProgressionBalance.StartingAttackPower,
        CharacterStatType.MaxHealth => ProgressionBalance.StartingMaxHealth,
        CharacterStatType.Speed => ProgressionBalance.StartingSpeed,
        CharacterStatType.GunPower => ProgressionBalance.StartingGunPower,
        CharacterStatType.Timer => ProgressionBalance.StartingTimer,
        _ => 0,
      };

    private int GetUpgradeCost() =>
      _statType switch
      {
        CharacterStatType.AttackPower => ProgressionBalance.AttackPowerUpgradeCost,
        CharacterStatType.MaxHealth => ProgressionBalance.MaxHealthUpgradeCost,
        CharacterStatType.Speed => ProgressionBalance.SpeedUpgradeCost,
        CharacterStatType.GunPower => ProgressionBalance.GunPowerUpgradeCost,
        CharacterStatType.Timer => ProgressionBalance.TimerUpgradeCost,
        _ => 0,
      };

    private bool GetCanUpgrade() =>
      _statType switch
      {
        CharacterStatType.AttackPower => _characterService.CanUpgradeAttackPower,
        CharacterStatType.MaxHealth => _characterService.CanUpgradeMaxHealth,
        CharacterStatType.Speed => _characterService.CanUpgradeSpeed,
        CharacterStatType.GunPower => _characterService.CanUpgradeGunPower,
        CharacterStatType.Timer => _characterService.CanUpgradeTimer,
        _ => false,
      };
  }
}
