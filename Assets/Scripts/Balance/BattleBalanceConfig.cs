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
    [SerializeField, Min(0f)] private float _battleDuration = 30f;
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
    [SerializeField, Min(0f)] private float _specialFenceStartDistance = 50f;
    [Tooltip("Distance to leave between the respawned timer and the nearest special in the fence.")]
    [SerializeField, Min(0f)] private float _specialFenceTimerOffset = 14f;
    [Tooltip("Distance to leave between the player and the nearest special in the fence.")]
    [SerializeField, Min(0f)] private float _specialFencePlayerOffset = 8f;

    [Header("Timer Route")]
    [SerializeField, Min(0f)] private float _timerTravelSpeedFactor = 0.8f;
    [SerializeField, Min(0f)] private float _timerPathOverhead = 1.45f;
    [SerializeField, Min(0f)] private float _timerWuPerBuilding = 38f;
    [SerializeField, Min(0f)] private float _timerSecondsPerBuilding = 1.5f;
    [Tooltip("Primary dial for the time budget slack applied to timer hops.")]
    [SerializeField, Min(0f)] private float _timerSlack = 1.15f;
    [SerializeField, Min(0f)] private float _timerMinSeconds = 12f;
    [SerializeField, Min(0f)] private float _timerRouteLateralAmplitude = 60f;
    [SerializeField, Range(0f, 1f)] private float _timerRouteForwardFraction = 0.9f;
    [SerializeField, Min(0f)] private float _timerRouteOscillations = 1.5f;
    [SerializeField, Min(0f)] private float _timerRouteMaxTurnAngle = 45f;
    [SerializeField, Min(0)] private int _timerRouteMinCheckpointsPerTier = 2;
    [Tooltip("Random placement radius around each absolute route waypoint. Set to 0 for exact waypoint placement.")]
    [SerializeField, Min(0f)] private float _timerRoutePlacementJitter = 4f;

    [Header("Duck (Mob)")]
    [SerializeField, Min(0)] private int _duckMaxHealth = 2;
    [SerializeField, Min(0)] private int _duckAttackDamage = 1;
    [SerializeField, Min(0f)] private float _duckAttackDistance = 3f;
    [SerializeField, Min(0f)] private float _duckRepositionDistance = 40f;
    [SerializeField, Min(0)] private int _maxLiveMobs = 0;

    [Header("Weapons")]
    [SerializeField, Min(0f)] private float _meleeAttackRadius = 2f;

    [Tooltip("Chance of dropping N coins on a duck kill, indexed by coin count. Must sum to 1.")]
    [SerializeField] private float[] _duckCoinDropChances = { 0.8f, 0.15f, 0.05f };

    [Header("Building Coin Drop")]
    [SerializeField, Min(0)] private int _buildingCoinDropMin = 8;
    [SerializeField, Min(0)] private int _buildingCoinDropMax = 14;
    [SerializeField, Range(0f, 1f)] private float _buildingCoinDropChance = 0.5f;
    [SerializeField, Min(0f)] private float _buildingCoinDropDistance = 2f;

    [Header("Destructible Objects")]
    [SerializeField] private List<DestructibleMaxHealthEntry> _destructibleMaxHealth = new()
    {
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.House, maxHealth = 50 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.TimerDigit, maxHealth = 15 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.TimerDivider, maxHealth = 10 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.Arrow, maxHealth = 12 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.HealthBar, maxHealth = 10 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.Upgrade, maxHealth = 36 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.Goal, maxHealth = 1080 },
      new DestructibleMaxHealthEntry { type = DestructibleObjectType.Prop, maxHealth = 1 },
    };

    [SerializeField] private List<HouseDifficultyMaxHealthEntry> _houseMaxHealthByDifficulty = new()
    {
      new HouseDifficultyMaxHealthEntry { difficultyLevel = 1, maxHealth = 36 },
      new HouseDifficultyMaxHealthEntry { difficultyLevel = 2, maxHealth = 108 },
      new HouseDifficultyMaxHealthEntry { difficultyLevel = 3, maxHealth = 232 },
    };

    public float BattleDuration => _battleDuration;
    public float WinGraceDuration => _winGraceDuration;
    public float SpecialBetweenMaxDistance => _specialBetweenMaxDistance;
    public float SpecialBetweenMinDistance => _specialBetweenMinDistance;
    public float SpecialFenceStartDistance => _specialFenceStartDistance;
    public float SpecialFenceTimerOffset => _specialFenceTimerOffset;
    public float SpecialFencePlayerOffset => _specialFencePlayerOffset;
    public float TimerTravelSpeedFactor => _timerTravelSpeedFactor;
    public float TimerPathOverhead => _timerPathOverhead;
    public float TimerWuPerBuilding => _timerWuPerBuilding;
    public float TimerSecondsPerBuilding => _timerSecondsPerBuilding;
    public float TimerSlack => _timerSlack;
    public float TimerMinSeconds => _timerMinSeconds;
    public float TimerRouteLateralAmplitude => _timerRouteLateralAmplitude;
    public float TimerRouteForwardFraction => _timerRouteForwardFraction;
    public float TimerRouteOscillations => _timerRouteOscillations;
    public float TimerRouteMaxTurnAngle => _timerRouteMaxTurnAngle;
    public int TimerRouteMinCheckpointsPerTier => _timerRouteMinCheckpointsPerTier;
    public float TimerRoutePlacementJitter => _timerRoutePlacementJitter;
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
      _timerTravelSpeedFactor = Mathf.Max(0f, _timerTravelSpeedFactor);
      _timerPathOverhead = Mathf.Max(0f, _timerPathOverhead);
      _timerWuPerBuilding = Mathf.Max(0f, _timerWuPerBuilding);
      _timerSecondsPerBuilding = Mathf.Max(0f, _timerSecondsPerBuilding);
      _timerSlack = Mathf.Max(0f, _timerSlack);
      _timerMinSeconds = Mathf.Max(0f, _timerMinSeconds);
      _timerRouteLateralAmplitude = Mathf.Max(0f, _timerRouteLateralAmplitude);
      _timerRouteForwardFraction = Mathf.Clamp01(_timerRouteForwardFraction);
      _timerRouteOscillations = Mathf.Max(0f, _timerRouteOscillations);
      _timerRouteMaxTurnAngle = Mathf.Max(0f, _timerRouteMaxTurnAngle);
      _timerRouteMinCheckpointsPerTier = Mathf.Max(0, _timerRouteMinCheckpointsPerTier);
      _timerRoutePlacementJitter = Mathf.Max(0f, _timerRoutePlacementJitter);
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
