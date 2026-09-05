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

    /// The player's health bar. Placed once at the start of a battle and never respawned or moved:
    /// smashing it is self-harm, so it must not come back offering a second chance to do it.
    Health = 3,
  }
}
