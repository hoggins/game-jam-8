namespace SceneHud
{
  /// <summary>
  /// Identifies one in-world object that is mirrored onto the HUD. The world side
  /// (<see cref="SceneHudElement"/>) and the HUD side (<see cref="SceneHudElementView"/>) are wired
  /// to each other by this id alone, so neither prefab needs a reference to the other.
  /// </summary>
  public enum SceneHudElementId
  {
    BattleTimer = 0,

    /// The compass arrow pointing at the current battle timer.
    Arrow = 1,

    /// The player's health bar, standing in the world as 24 smashable pixels.
    Hp = 2,

    /// The static upgrade house display. It is hidden after the house is destroyed.
    Upgrade = 3,
  }
}
