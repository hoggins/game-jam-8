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
        default:
          throw new ArgumentOutOfRangeException(nameof(_statType), _statType, "Unhandled character stat.");
      }
    }

    private void Refresh()
    {
      _nameText.text = GetName();
      _valueText.text = $"+{GetBonus()}";
      _priceText.text = $"{GetUpgradeCost()} COINS";
      _levelText.text = $"LVL {GetLevel()}";
      _upgradeButton.interactable = GetCanUpgrade();
    }

    private string GetName() =>
      _statType switch
      {
        CharacterStatType.AttackPower => "attack",
        CharacterStatType.MaxHealth => "max hp",
        CharacterStatType.Speed => "speed",
        CharacterStatType.GunPower => "gun",
        _ => string.Empty,
      };

    /// How much the stat gained over its starting value.
    private int GetBonus() =>
      GetValue() - GetStartingValue();

    /// Upgrades bought so far, displayed as a level starting from 1.
    private int GetLevel()
    {
      var upgradeAmount = GetUpgradeAmount();
      return upgradeAmount <= 0 ? 1 : GetBonus() / upgradeAmount + 1;
    }

    private int GetValue() =>
      _statType switch
      {
        CharacterStatType.AttackPower => _characterService.AttackPower,
        CharacterStatType.MaxHealth => _characterService.MaxHealth,
        CharacterStatType.Speed => _characterService.Speed,
        CharacterStatType.GunPower => _characterService.GunPower,
        _ => 0,
      };

    private int GetStartingValue() =>
      _statType switch
      {
        CharacterStatType.AttackPower => ProgressionBalance.StartingAttackPower,
        CharacterStatType.MaxHealth => ProgressionBalance.StartingMaxHealth,
        CharacterStatType.Speed => ProgressionBalance.StartingSpeed,
        CharacterStatType.GunPower => ProgressionBalance.StartingGunPower,
        _ => 0,
      };

    private int GetUpgradeAmount() =>
      _statType switch
      {
        CharacterStatType.AttackPower => ProgressionBalance.AttackPowerUpgradeAmount,
        CharacterStatType.MaxHealth => ProgressionBalance.MaxHealthUpgradeAmount,
        CharacterStatType.Speed => ProgressionBalance.SpeedUpgradeAmount,
        CharacterStatType.GunPower => ProgressionBalance.GunPowerUpgradeAmount,
        _ => 0,
      };

    private int GetUpgradeCost() =>
      _statType switch
      {
        CharacterStatType.AttackPower => ProgressionBalance.AttackPowerUpgradeCost,
        CharacterStatType.MaxHealth => ProgressionBalance.MaxHealthUpgradeCost,
        CharacterStatType.Speed => ProgressionBalance.SpeedUpgradeCost,
        CharacterStatType.GunPower => ProgressionBalance.GunPowerUpgradeCost,
        _ => 0,
      };

    private bool GetCanUpgrade() =>
      _statType switch
      {
        CharacterStatType.AttackPower => _characterService.CanUpgradeAttackPower,
        CharacterStatType.MaxHealth => _characterService.CanUpgradeMaxHealth,
        CharacterStatType.Speed => _characterService.CanUpgradeSpeed,
        CharacterStatType.GunPower => _characterService.CanUpgradeGunPower,
        _ => false,
      };
  }
}
