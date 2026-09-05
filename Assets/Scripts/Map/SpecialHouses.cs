namespace Map
{
  public enum SpecialHouses
  {
    Timer = 0,

    /// A destructible House01 that opens the in-battle progression screen. It is respawned with the
    /// Timer and its HUD camera is static once it has been placed.
    Upgrade = 1,

    /// The compass arrow. One per battle: it is moved rather than duplicated, and once smashed it
    /// stays gone until the next battle.
    Arrow = 2,

    /// The player's health bar. It is never recreated after being smashed, but a live bar may move
    /// into the centre of the special fence when the timer respawns.
    Health = 3,

    /// The compass arrow for the final goal. It is placed and moved with the other specials, but it
    /// has its own slot so it can coexist with the timer arrow.
    GoalArrow = 4,
  }
}
