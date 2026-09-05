using System;
using System.Collections.Generic;
using Destruction;
using UnityEngine;

namespace Balance
{
  [Serializable]
  public struct DestructibleMaxHealthEntry
  {
    public DestructibleObjectType type;
    [Min(0)] public int maxHealth;
  }

  [CreateAssetMenu(fileName = "BattleBalanceConfig", menuName = "Game/Battle Balance Config")]
  public sealed class BattleBalanceConfig : ScriptableObject
  {
    [Header("Timing")]
    [SerializeField, Min(0f)] private float _battleDuration = 90f;

    [Header("Special Object Respawn")]
    [Tooltip("Beyond this distance from the player, a non-Timer special respawns somewhere between "
      + "the timer and the player; within it, the special spawns in a band between " + nameof(_specialBetweenMinDistance)
      + " and this distance from the player instead, so it never lands right on top of them.")]
    [SerializeField, Min(0f)] private float _specialBetweenMaxDistance = 50f;
    [Tooltip("Lower bound of the near-player band; see " + nameof(_specialBetweenMaxDistance) + ".")]
    [SerializeField, Min(0f)] private float _specialBetweenMinDistance = 40f;

    [Header("Duck (Mob)")]
    [SerializeField, Min(0)] private int _duckMaxHealth = 2;
    [SerializeField, Min(0)] private int _duckAttackDamage = 1;
    [SerializeField, Min(0f)] private float _duckAttackDistance = 3f;
    [SerializeField, Min(0f)] private float _duckRepositionDistance = 40f;

    [Header("Weapons")]
    [SerializeField, Min(0f)] private float _meleeAttackRadius = 2f;

    [Tooltip("Chance of dropping N coins on a duck kill, indexed by coin count. Must sum to 1.")]
    [SerializeField] private float[] _duckCoinDropChances = { 0.6f, 0.3f, 0.1f };

    [Header("Building Coin Drop")]
    [SerializeField, Min(0)] private int _buildingCoinDropMin = 1;
    [SerializeField, Min(0)] private int _buildingCoinDropMax = 3;
    [SerializeField, Range(0f, 1f)] private float _buildingCoinDropChance = 0.5f;
    [SerializeField, Min(0f)] private float _buildingCoinDropDistance = 2f;

    [Header("Destructible Objects")]
    [SerializeField] private List<DestructibleMaxHealthEntry> _destructibleMaxHealth = new()
    {
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.House, maxHealth = 7 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.TimerDigit, maxHealth = 15 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.TimerDivider, maxHealth = 10 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.Arrow, maxHealth = 12 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.HealthBar, maxHealth = 48 },
    };

    public float BattleDuration => _battleDuration;
    public float SpecialBetweenMaxDistance => _specialBetweenMaxDistance;
    public float SpecialBetweenMinDistance => _specialBetweenMinDistance;
    public int DuckMaxHealth => _duckMaxHealth;
    public int DuckAttackDamage => _duckAttackDamage;
    public float DuckAttackDistance => _duckAttackDistance;
    public float DuckRepositionDistance => _duckRepositionDistance;
    public float MeleeAttackRadius => _meleeAttackRadius;
    public float BuildingCoinDropDistance => _buildingCoinDropDistance;

    public int GetDestructibleMaxHealth(DestructibleObjectType type)
    {
      foreach (var entry in _destructibleMaxHealth)
        if (entry.type == type)
          return entry.maxHealth;

      throw new KeyNotFoundException($"No max health configured for destructible type '{type}'.");
    }

    public int RollDuckCoinDrop()
    {
      var roll = UnityEngine.Random.value;
      var accumulated = 0f;
      for (var coins = 0; coins < _duckCoinDropChances.Length; coins++)
      {
        accumulated += _duckCoinDropChances[coins];
        if (roll < accumulated)
          return coins;
      }

      return _duckCoinDropChances.Length - 1;
    }

    public int RollBuildingCoinDrop()
    {
      if (UnityEngine.Random.value >= _buildingCoinDropChance)
        return 0;

      return UnityEngine.Random.Range(_buildingCoinDropMin, _buildingCoinDropMax + 1);
    }

    private void OnValidate()
    {
      _battleDuration = Mathf.Max(0f, _battleDuration);
      _specialBetweenMaxDistance = Mathf.Max(0f, _specialBetweenMaxDistance);
      _specialBetweenMinDistance = Mathf.Max(0f, _specialBetweenMinDistance);
      _duckMaxHealth = Mathf.Max(0, _duckMaxHealth);
      _duckAttackDamage = Mathf.Max(0, _duckAttackDamage);
      _duckAttackDistance = Mathf.Max(0f, _duckAttackDistance);
      _duckRepositionDistance = Mathf.Max(0f, _duckRepositionDistance);
      _meleeAttackRadius = Mathf.Max(0f, _meleeAttackRadius);
      _buildingCoinDropMin = Mathf.Max(0, _buildingCoinDropMin);
      _buildingCoinDropMax = Mathf.Max(_buildingCoinDropMin, _buildingCoinDropMax);
      _buildingCoinDropChance = Mathf.Clamp01(_buildingCoinDropChance);
      _buildingCoinDropDistance = Mathf.Max(0f, _buildingCoinDropDistance);

      for (var i = 0; i < _destructibleMaxHealth.Count; i++)
      {
        var entry = _destructibleMaxHealth[i];
        entry.maxHealth = Mathf.Max(0, entry.maxHealth);
        _destructibleMaxHealth[i] = entry;
      }
    }
  }
}
