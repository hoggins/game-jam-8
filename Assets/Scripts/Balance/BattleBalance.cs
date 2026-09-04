using System.Collections.Generic;
using Destruction;

namespace Balance
{
  public static class BattleBalance
  {
    public static float BattleDuration => 100000f;
    public static int DuckMaxHealth => 2;
    public static int DuckAttackDamage => 1;
    public static float DuckAttackDistance => 3f;

    private static readonly Dictionary<DestructibleObjectType, int> DestructibleMaxHealth = new()
    {
      [DestructibleObjectType.House] = 7
    };

    public static int GetDestructibleMaxHealth(DestructibleObjectType type) =>
      DestructibleMaxHealth[type];

    /// Chance of dropping N coins on a duck kill, indexed by coin count. Must sum to 1.
    private static readonly float[] DuckCoinDropChances = { 0.6f, 0.3f, 0.1f };

    public static int RollDuckCoinDrop()
    {
      var roll = UnityEngine.Random.value;
      var accumulated = 0f;
      for (var coins = 0; coins < DuckCoinDropChances.Length; coins++)
      {
        accumulated += DuckCoinDropChances[coins];
        if (roll < accumulated)
          return coins;
      }

      return DuckCoinDropChances.Length - 1;
    }
  }
}
