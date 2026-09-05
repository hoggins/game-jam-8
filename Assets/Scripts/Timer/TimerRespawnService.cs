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
  /// houses in its way, and resumes the countdown from the default duration. Every other
  /// configured special respawns alongside it: between the new timer and the player when they're
  /// far apart, or just randomly near the player when they're already close.
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
          MoveArrow(anchor, player, minDistance, maxDistance);
          continue;
        }

        // The health bar is the player's own health made physical, so putting a fresh one in front of
        // him would hand back what he spent smashing the last one. It is placed once by
        // MapEnvironmentSpawner.SpawnInitialSpecials and stays where it was put until the next
        // battle.
        if (special.type == SpecialHouses.Health)
          continue;

        _spawner.TrySpawnSpecial(special.type, anchor, player, minDistance, maxDistance, out _);
      }
    }

    /// <summary>
    /// The compass arrow is the one special that is never respawned. There is exactly one per
    /// battle: while it stands, a new timer moves it somewhere new along the route rather than
    /// handing the player a second one, so the navigation aid has to be found again; once it is
    /// smashed it is gone for the rest of the battle, and losing your bearings is the price of
    /// having broken it.
    ///
    /// <see cref="BattleArrowObject.Current"/> is null in exactly that second case, which is why
    /// nothing here spawns a replacement.
    /// </summary>
    private void MoveArrow(Vector3 anchor, Transform player, float minDistance, float maxDistance)
    {
      var arrow = BattleArrowObject.Current;
      if (arrow == null)
        return;

      _spawner.TryMoveSpecial(SpecialHouses.Arrow, arrow.gameObject, anchor, player, minDistance, maxDistance);
    }
  }
}
