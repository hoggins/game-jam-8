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
  }
}
