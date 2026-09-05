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

  [Serializable]
  public struct HouseDifficultyMaxHealthEntry
  {
    [Min(1)] public int difficultyLevel;
    [Min(0)] public int maxHealth;
  }

  [CreateAssetMenu(fileName = "BattleBalanceConfig", menuName = "Game/Battle Balance Config")]
  public sealed class BattleBalanceConfig : ScriptableObject
  {
    [Header("Timing")]
    [SerializeField, Min(0f)] private float _battleDuration = 90f;
    [SerializeField, Min(0f)] private float _winGraceDuration = 2f;

    [Header("Special Object Respawn")]
    [Tooltip("Beyond this distance from the player, a non-Timer special respawns somewhere between "
      + "the timer and the player; within it, the special spawns in a band between " + nameof(_specialBetweenMinDistance)
      + " and this distance from the player instead, so it never lands right on top of them.")]
    [SerializeField, Min(0f)] private float _specialBetweenMaxDistance = 50f;
    [Tooltip("Lower bound of the near-player band; see " + nameof(_specialBetweenMaxDistance) + ".")]
    [SerializeField, Min(0f)] private float _specialBetweenMinDistance = 40f;

    [Header("Special Object Fence")]
    [Tooltip("When the timer is at least this far from the player after a respawn, the other live specials form a line between them with the health bar in the centre.")]
    [SerializeField, Min(0f)] private float _specialFenceStartDistance = 35f;
    [Tooltip("Distance to leave between the respawned timer and the nearest special in the fence.")]
    [SerializeField, Min(0f)] private float _specialFenceTimerOffset = 14f;
    [Tooltip("Distance to leave between the player and the nearest special in the fence.")]
    [SerializeField, Min(0f)] private float _specialFencePlayerOffset = 8f;

    [Header("Duck (Mob)")]
    [SerializeField, Min(0)] private int _duckMaxHealth = 2;
    [SerializeField, Min(0)] private int _duckAttackDamage = 1;
    [SerializeField, Min(0f)] private float _duckAttackDistance = 3f;
    [SerializeField, Min(0f)] private float _duckRepositionDistance = 40f;
    [SerializeField, Min(0)] private int _maxLiveMobs = 0;

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
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.House, maxHealth = 50 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.TimerDigit, maxHealth = 15 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.TimerDivider, maxHealth = 10 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.Arrow, maxHealth = 12 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.HealthBar, maxHealth = 48 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.Upgrade, maxHealth = 50 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.Goal, maxHealth = 1250 },
    };

    [SerializeField] private List<HouseDifficultyMaxHealthEntry> _houseMaxHealthByDifficulty = new()
    {
      new HouseDifficultyMaxHealthEntry { difficultyLevel = 1, maxHealth = 50 },
      new HouseDifficultyMaxHealthEntry { difficultyLevel = 2, maxHealth = 250 },
      new HouseDifficultyMaxHealthEntry { difficultyLevel = 3, maxHealth = 1250 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.Prop, maxHealth = 1 },
    };

    public float BattleDuration => _battleDuration;
    public float WinGraceDuration => _winGraceDuration;
    public float SpecialBetweenMaxDistance => _specialBetweenMaxDistance;
    public float SpecialBetweenMinDistance => _specialBetweenMinDistance;
    public float SpecialFenceStartDistance => _specialFenceStartDistance;
    public float SpecialFenceTimerOffset => _specialFenceTimerOffset;
    public float SpecialFencePlayerOffset => _specialFencePlayerOffset;
    public int DuckMaxHealth => _duckMaxHealth;
    public int DuckAttackDamage => _duckAttackDamage;
    public float DuckAttackDistance => _duckAttackDistance;
    public float DuckRepositionDistance => _duckRepositionDistance;
    public int MaxLiveMobs => _maxLiveMobs;
    public float MeleeAttackRadius => _meleeAttackRadius;
    public float BuildingCoinDropDistance => _buildingCoinDropDistance;

    public int GetDestructibleMaxHealth(DestructibleObjectType type)
    {
      foreach (var entry in _destructibleMaxHealth)
        if (entry.type == type)
          return entry.maxHealth;

      throw new KeyNotFoundException($"No max health configured for destructible type '{type}'.");
    }

    public int GetHouseMaxHealth(int difficultyLevel)
    {
      foreach (var entry in _houseMaxHealthByDifficulty)
        if (entry.difficultyLevel == difficultyLevel)
          return entry.maxHealth;

      throw new KeyNotFoundException($"No max health configured for house difficulty level '{difficultyLevel}'.");
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
      _winGraceDuration = Mathf.Max(0f, _winGraceDuration);
      _specialBetweenMaxDistance = Mathf.Max(0f, _specialBetweenMaxDistance);
      _specialBetweenMinDistance = Mathf.Max(0f, _specialBetweenMinDistance);
      _specialFenceStartDistance = Mathf.Max(0f, _specialFenceStartDistance);
      _specialFenceTimerOffset = Mathf.Max(0f, _specialFenceTimerOffset);
      _specialFencePlayerOffset = Mathf.Max(0f, _specialFencePlayerOffset);
      _duckMaxHealth = Mathf.Max(0, _duckMaxHealth);
      _duckAttackDamage = Mathf.Max(0, _duckAttackDamage);
      _duckAttackDistance = Mathf.Max(0f, _duckAttackDistance);
      _duckRepositionDistance = Mathf.Max(0f, _duckRepositionDistance);
      _maxLiveMobs = Mathf.Max(0, _maxLiveMobs);
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

      for (var i = 0; i < _houseMaxHealthByDifficulty.Count; i++)
      {
        var entry = _houseMaxHealthByDifficulty[i];
        entry.difficultyLevel = Mathf.Max(1, entry.difficultyLevel);
        entry.maxHealth = Mathf.Max(0, entry.maxHealth);
        _houseMaxHealthByDifficulty[i] = entry;
      }
    }
  }
}
