using System;
using Arrow;
using Map;
using Model;
using UnityEngine;
using UnityEngine.Scripting;
using VContainer.Unity;

namespace Timer
{
  /// <summary>
  /// Keeps the battle timer alive: once <see cref="BattleService.DestroyTimer"/> fires (every
  /// digit smashed), spawns a fresh <see cref="BattleTimerObject"/> near the player, clearing any
  /// houses in its way, and resumes the countdown from the default duration. When the new timer is
  /// far enough from the player, the other live specials form a fence between them with the health
  /// bar in the centre; otherwise they use the near-player rule.
  /// </summary>
  [Preserve]
  public sealed class TimerRespawnService : IInitializable, IDisposable
  {
    private readonly BattleService _battleService;
    private readonly MapEnvironmentSpawner _spawner;
    private readonly SpecialSpawnSettings _spawnSettings;

    private int _respawnCount;

    public TimerRespawnService(BattleService battleService, MapEnvironmentSpawner spawner, SpecialSpawnSettings spawnSettings)
    {
      _battleService = battleService;
      _spawner = spawner;
      _spawnSettings = spawnSettings;
    }

    void IInitializable.Initialize()
    {
      _battleService.TimerDestroyed += OnTimerDestroyed;
      _battleService.BattleStarted += OnBattleStarted;
    }

    void IDisposable.Dispose()
    {
      _battleService.TimerDestroyed -= OnTimerDestroyed;
      _battleService.BattleStarted -= OnBattleStarted;
    }

    private void OnBattleStarted() =>
      _respawnCount = 0;

    private void OnTimerDestroyed()
    {
      var player = GameObject.FindGameObjectWithTag("Player");
      if (player == null)
        return;

      if (_spawnSettings == null || !_spawnSettings.TryGetRespawnDistance(SpecialHouses.Timer, _respawnCount, out var minDistance, out var maxDistance))
        return;

      if (!_spawner.TrySpawnSpecial(SpecialHouses.Timer, player.transform.position, player.transform, minDistance, maxDistance, out var timerInstance))
        return;

      _respawnCount++;
      _battleService.RespawnTimer();

      if (_spawner.TryArrangeSpecialFence(timerInstance.transform.position, player.transform.position, player.transform))
        return;

      RespawnOtherSpecials(player.transform, timerInstance.transform.position);
    }

    private void RespawnOtherSpecials(Transform player, Vector3 timerPosition)
    {
      var houseSet = _spawner.CurrentHouseSet;
      if (houseSet == null)
        return;

      _spawner.GetOtherSpecialPlacement(timerPosition, player.position, out var anchor, out var minDistance, out var maxDistance);

      foreach (var special in houseSet.Specials)
      {
        if (special.type == SpecialHouses.Timer || !special.enabled || special.prefab == null)
          continue;

        if (special.type == SpecialHouses.Arrow)
        {
          MoveArrow(SpecialHouses.Arrow, BattleArrowObject.Current, anchor, player, minDistance, maxDistance);
          continue;
        }

        if (special.type == SpecialHouses.GoalArrow)
        {
          MoveArrow(SpecialHouses.GoalArrow, BattleArrowObject.GoalCurrent, anchor, player, minDistance, maxDistance);
          continue;
        }

        // The health bar is the player's own health made physical, so the fallback path never
        // recreates or relocates it. The fence path moves the existing live bar without resetting
        // its current pixels and makes it the central fence asset.
        if (special.type == SpecialHouses.Health)
          continue;

        // A special respawns alongside every Timer death; if one is already standing from an earlier
        // respawn, relocate it instead of spawning a duplicate on top of the world.
        if (_spawner.TryGetCurrentSpecial(special.type, out var existing))
          _spawner.TryMoveSpecial(special.type, existing, anchor, player, minDistance, maxDistance);
        else
          _spawner.TrySpawnSpecial(special.type, anchor, player, minDistance, maxDistance, out _);
      }
    }

    /// <summary>
    /// Compass arrows are the specials that are never duplicated. There is one timer arrow and one
    /// goal arrow per battle: while either stands, a new timer moves it somewhere new along the
    /// route rather than handing the player a second one, so the navigation aid has to be found
    /// again; once smashed it is gone for the rest of the battle.
    ///
    /// <see cref="BattleArrowObject.Current"/> is null in exactly that second case, which is why
    /// nothing here spawns a replacement.
    /// </summary>
    private void MoveArrow(
      SpecialHouses type, BattleArrowObject arrow, Vector3 anchor, Transform player,
      float minDistance, float maxDistance)
    {
      if (arrow == null)
        return;

      _spawner.TryMoveSpecial(type, arrow.gameObject, anchor, player, minDistance, maxDistance);
    }
  }
}
