using System;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
  [Serializable]
  public class HouseLevelWeight
  {
    [Range(1, HouseSet.DifficultyLevelCount)] public int level = 1;
    [Min(0f)] public float weight = 1f;
  }

  [Serializable]
  public class HouseDifficultyRange
  {
    [Min(0f)] public float minDistance;
    [Min(0f)] public float maxDistance = 1000f;
    public List<HouseLevelWeight> levelWeights = new();
  }

  [CreateAssetMenu(fileName = "HouseSet", menuName = "Map/House Set")]
  public class HouseSet : ScriptableObject
  {
    public const int DifficultyLevelCount = 5;
    public const float RouteT1End = 0.20f;
    public const float RouteT2End = 0.60f;

    [SerializeField] private List<HouseObject> houses = new();
    [SerializeField] private List<SpecialHouseObject> specials = new();
    [SerializeField] private List<HouseDifficultyRange> difficultyRanges = new();

    public IReadOnlyList<HouseObject> Houses => houses;
    public IReadOnlyList<SpecialHouseObject> Specials => specials;
    public IReadOnlyList<HouseDifficultyRange> DifficultyRanges => difficultyRanges;

    public int PickDifficultyLevelByRouteProgress(float normalizedProgress)
    {
      if (normalizedProgress < RouteT1End)
        return 1;

      return normalizedProgress < RouteT2End ? 2 : 3;
    }

    public int PickDifficultyLevel(float distance, System.Random random)
    {
      HouseDifficultyRange range = null;
      foreach (var candidate in difficultyRanges)
        if (distance >= candidate.minDistance && distance < candidate.maxDistance)
        {
          range = candidate;
          break;
        }

      if (range == null || range.levelWeights.Count == 0)
        return 1;

      var totalWeight = 0f;
      foreach (var levelWeight in range.levelWeights)
        totalWeight += levelWeight.weight;

      if (totalWeight <= 0f)
        return range.levelWeights[0].level;

      var roll = (float)random.NextDouble() * totalWeight;
      var accumulated = 0f;
      foreach (var levelWeight in range.levelWeights)
      {
        accumulated += levelWeight.weight;
        if (roll < accumulated)
          return levelWeight.level;
      }

      return range.levelWeights[^1].level;
    }
  }
}
