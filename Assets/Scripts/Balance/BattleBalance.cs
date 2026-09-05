using System.Collections.Generic;
using Destruction;

namespace Balance
{
  public static class BattleBalance
  {
    /// Beat held on 00:00, with damage switched off, before the timeout defeat lands.
    public static float TimerExpiredDefeatDelay => 2f;
    public static float BattleDuration => 90;

    /// Beyond this distance from the player, a non-Timer special respawns somewhere between the
    /// timer and the player; within it, the special spawns in a band between
    /// <see cref="SpecialBetweenMinDistance"/> and this distance from the player instead, so it
    /// never lands right on top of them.
    public static float SpecialBetweenMaxDistance => 50f;

    /// Lower bound of that near-player band; see <see cref="SpecialBetweenMaxDistance"/>.
    public static float SpecialBetweenMinDistance => 40f;
    public static int DuckMaxHealth => 2;
    public static int DuckAttackDamage => 1;
    public static float DuckAttackDistance => 3f;
    public static float DuckRepositionDistance => 40f;

    private static readonly Dictionary<DestructibleObjectType, int> DestructibleMaxHealth = new()
    {
      [DestructibleObjectType.House] = 7,
      [DestructibleObjectType.TimerDigit] = 15,
      [DestructibleObjectType.TimerDivider] = 10,
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
