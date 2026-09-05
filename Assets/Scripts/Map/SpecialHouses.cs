namespace Map
{
  public enum SpecialHouses
  {
    Timer = 0,

    /// Placeholder second special used to validate that multiple specials respawn correctly
    /// alongside the Timer.
    TestHouse = 1,

    /// The compass arrow. One per battle: it is moved rather than duplicated, and once smashed it
    /// stays gone until the next battle.
    Arrow = 2,
  }
}
