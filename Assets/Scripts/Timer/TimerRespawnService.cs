using System;
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

      if (_spawnSettings == null || !_spawnSettings.TryGetRespawnMaxDistance(SpecialHouses.Timer, _respawnCount, out var maxDistance))
        return;

      if (!_spawner.TrySpawnSpecial(SpecialHouses.Timer, player.transform.position, player.transform, 0f, maxDistance, out var timerInstance))
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

      MapEnvironmentSpawner.GetOtherSpecialPlacement(timerPosition, player.position, out var anchor, out var minDistance, out var maxDistance);

      foreach (var special in houseSet.Specials)
      {
        if (special.type == SpecialHouses.Timer || !special.enabled || special.prefab == null)
          continue;

        _spawner.TrySpawnSpecial(special.type, anchor, player, minDistance, maxDistance, out _);
      }
    }
  }
}
