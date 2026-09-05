namespace Balance
{
  public static class ProgressionBalance
  {
    public static int StartingAttackPower => 1;
    public static int StartingMaxHealth => 300000;
    public static int StartingSpeed => 6;
    public static int StartingGunPower => 1;

    /// Seconds added to the battle clock on top of <see cref="BattleBalance.BattleDuration"/>.
    public static int StartingTimer => 0;

    public static int AttackPowerUpgradeCost => 100;
    public static int AttackPowerUpgradeAmount => 1;
    public static int MaxHealthUpgradeCost => 100;
    public static int MaxHealthUpgradeAmount => 1;
    public static int SpeedUpgradeCost => 100;
    public static int SpeedUpgradeAmount => 1;
    public static int GunPowerUpgradeCost => 100;
    public static int GunPowerUpgradeAmount => 1;
    public static int TimerUpgradeCost => 100;
    public static int TimerUpgradeAmount => 1;

    /// Highest level any stat can reach. Levels are displayed from 1, so this allows
    /// <c>MaxUpgradeLevel - 1</c> purchases.
    public static int MaxUpgradeLevel => 30;

    /// The player grows by this fraction of its authored size per max-health level.
    public static float MaxHealthScalePerLevel => 0.02f;
  }
}
