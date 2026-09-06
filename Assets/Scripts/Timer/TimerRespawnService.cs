using System;
using Arrow;
using Balance;
using Map;
using Model;
using Telemetry;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.Scripting;
using VContainer.Unity;

namespace Timer
{
  /// <summary>
  /// Keeps the battle timer alive: once <see cref="BattleService.DestroyTimer"/> fires (every
  /// digit smashed), spawns a fresh <see cref="BattleTimerObject"/> on a goalward zig-zag route,
  /// clearing any houses in its way, and resumes the countdown from a computed route budget. When the
  /// new timer is far enough from the player, the other live specials form a fence between them
  /// with the health bar in the centre; otherwise they use the near-player rule.
  /// </summary>
  [Preserve]
  public sealed class TimerRespawnService : IInitializable, IDisposable
  {
    private static readonly ProfilerMarker RespawnMarker = new("TimerRespawnService.Respawn");
    private static readonly ProfilerMarker SpawnTimerMarker = new("TimerRespawnService.SpawnTimer");
    private static readonly ProfilerMarker ArrangeFenceMarker = new("TimerRespawnService.ArrangeFence");
    private static readonly ProfilerMarker OtherSpecialsMarker = new("TimerRespawnService.OtherSpecials");

    private static readonly Vector3 EarlySpecialClusterAnchor = new(-700f, 0f, -700f);
    private const int EarlySpecialClusterRespawns = 3;
    private const float EarlySpecialClusterRadius = 20f;
    private readonly BattleService _battleService;
    private readonly MapEnvironmentSpawner _spawner;
    private readonly SpecialSpawnSettings _spawnSettings;
    private readonly BattleBalanceConfig _battleBalance;
    private readonly CharacterService _characterService;
    private readonly EconomyTelemetryService _telemetry;

    private int _respawnCount;
    private Vector3 _lastTimerPosition;
    private bool _hasLastTimerPosition;
    private TimerRoute _timerRoute;

    public TimerRespawnService(
      BattleService battleService,
      MapEnvironmentSpawner spawner,
      SpecialSpawnSettings spawnSettings,
      BattleBalanceConfig battleBalance,
      CharacterService characterService,
      EconomyTelemetryService telemetry)
    {
      _battleService = battleService;
      _spawner = spawner;
      _spawnSettings = spawnSettings;
      _battleBalance = battleBalance;
      _characterService = characterService;
      _telemetry = telemetry;
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

    private void OnBattleStarted()
    {
      _respawnCount = 0;
      _lastTimerPosition = default;
      _hasLastTimerPosition = false;
      _timerRoute = null;
    }

    private void OnTimerDestroyed(float secondsRemainingOnArrival)
    {
      RespawnMarker.Begin();
      try
      {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
          return;

        if (_spawnSettings == null || !_spawnSettings.TryGetRespawnDistance(SpecialHouses.Timer, _respawnCount, out var minDistance, out var maxDistance))
          return;

        var playerSpeedAtStart = _characterService.Speed;
        GameObject timerInstance = null;
        Vector3 hopOrigin;
        float routeProgress;
        float totalRouteProgress;
        SpawnTimerMarker.Begin();
        try
        {
          if (!TrySpawnTimer(
            player.transform,
            minDistance,
            maxDistance,
            out timerInstance,
            out hopOrigin,
            out routeProgress,
            out totalRouteProgress))
            return;
        }
        finally
        {
          SpawnTimerMarker.End();
        }

        var straightLineDistance = HorizontalDistance(hopOrigin, timerInstance.transform.position);
        var secondsGranted = CalculateTimerBudget(straightLineDistance, playerSpeedAtStart);
        _telemetry?.CompleteTimerHop(secondsRemainingOnArrival);
        _battleService.RespawnTimer(secondsGranted);
        _lastTimerPosition = timerInstance.transform.position;
        _hasLastTimerPosition = true;
        _respawnCount++;
        var normalizedRouteProgress = _timerRoute != null
          ? _timerRoute.NormalizeProgress(routeProgress)
          : 0f;
        var intendedHouseTier = _spawner.CurrentHouseSet != null
          ? _spawner.CurrentHouseSet.PickDifficultyLevelByRouteProgress(normalizedRouteProgress)
          : 1;
        _telemetry?.BeginTimerHop(
          _respawnCount,
          straightLineDistance,
          secondsGranted,
          playerSpeedAtStart,
          routeProgress,
          totalRouteProgress,
          _characterService.AttackPower,
          intendedHouseTier);

        if (_respawnCount <= EarlySpecialClusterRespawns)
        {
          MoveSpecialsOutOfMap(player.transform);
          _spawner.RefreshNoGoZones();
          return;
        }

        var arrangedFence = false;
        ArrangeFenceMarker.Begin();
        try
        {
          arrangedFence = _spawner.TryArrangeSpecialFence(
            timerInstance.transform.position, player.transform.position, player.transform);
        }
        finally
        {
          ArrangeFenceMarker.End();
        }

        if (!arrangedFence)
          RespawnOtherSpecials(player.transform, timerInstance.transform.position);

        // All special placement/movement operations are now complete. Refresh once so the newly
        // spawned timer and any moved specials are visible to the flow map as a single change.
        _spawner.RefreshNoGoZones();
      }
      finally
      {
        RespawnMarker.End();
      }
    }

    private bool TrySpawnTimer(
      Transform player,
      float minDistance,
      float maxDistance,
      out GameObject timerInstance,
      out Vector3 hopOrigin,
      out float routeProgress,
      out float totalRouteProgress)
    {
      timerInstance = null;
      routeProgress = 0f;
      totalRouteProgress = 0f;
      hopOrigin = _hasLastTimerPosition ? _lastTimerPosition : player.position;
      if (!_hasLastTimerPosition)
        TryGetCurrentTimerPosition(player.position, out hopOrigin);

      _timerRoute ??= _spawner.CurrentTimerRoute;
      if (_timerRoute == null)
        TimerRoute.TryCreateForBattle(
          player.position,
          _battleBalance != null ? _battleBalance.TimerRouteLateralAmplitude : 60f,
          _battleBalance != null ? _battleBalance.TimerRouteForwardFraction : 0.9f,
          _battleBalance != null ? _battleBalance.TimerRouteOscillations : 1.5f,
          _spawnSettings,
          0,
          out _timerRoute);
        _telemetry?.RecordRoute(_timerRoute);

      if (_timerRoute != null && _timerRoute.TryGetHop(_respawnCount, out var anchor, out var nextRouteProgress))
      {
        routeProgress = nextRouteProgress;
        totalRouteProgress = _timerRoute.TotalLength;
        // TrySpawnSpecial still validates the candidate against live specials and TheGoal. A small
        // configurable jitter around the directed anchor gives it nearby options without losing the
        // absolute route leg.
        if (_spawner.TrySpawnSpecial(
          SpecialHouses.Timer,
          anchor,
          player,
          0f,
          _battleBalance != null ? _battleBalance.TimerRoutePlacementJitter : 4f,
          out timerInstance))
          return true;

        routeProgress = 0f;
        totalRouteProgress = 0f;
      }

      // A missing goal, a short route, or a crowded candidate should not strand the battle. Keep the
      // original player-centered behavior as the safe fallback.
      return _spawner.TrySpawnSpecial(
        SpecialHouses.Timer, player.position, player, minDistance, maxDistance, out timerInstance);
    }

    private void MoveSpecialsOutOfMap(Transform player)
    {
      var houseSet = _spawner.CurrentHouseSet;
      if (houseSet == null)
        return;

      foreach (var special in houseSet.Specials)
      {
        if (special.type == SpecialHouses.Timer || !special.enabled || special.prefab == null
            || _spawner.IsSpecialDestroyed(special.type))
          continue;

        if ((special.type == SpecialHouses.Health || special.type == SpecialHouses.Arrow)
            && !_spawner.TryGetCurrentSpecial(special.type, out _))
          continue;

        if (_spawner.TryGetCurrentSpecial(special.type, out var existing))
          _spawner.TryMoveSpecial(
            special.type,
            existing,
            EarlySpecialClusterAnchor,
            player,
            0f,
            EarlySpecialClusterRadius);
        else
          _spawner.TrySpawnSpecial(
            special.type,
            EarlySpecialClusterAnchor,
            player,
            0f,
            EarlySpecialClusterRadius,
            out _);
      }
    }

    private static bool TryGetCurrentTimerPosition(Vector3 playerPosition, out Vector3 timerPosition)
    {
      // The destroyed timer root remains alive as a decay husk during this callback. Reusing its
      // position makes the first route leg start where the initial timer actually stood instead of
      // jumping to an arbitrary point around the player.
      var timers = UnityEngine.Object.FindObjectsByType<BattleTimerObject>(FindObjectsSortMode.None);
      foreach (var timer in timers)
        if (timer != null && timer.IsDead)
        {
          timerPosition = timer.transform.position;
          return true;
        }

      timerPosition = playerPosition;
      return true;
    }

    private float CalculateTimerBudget(float straightLineDistance, float playerSpeed)
    {
      var path = straightLineDistance * _battleBalance.TimerPathOverhead;
      var travelSpeed = _battleBalance.TimerTravelSpeedFactor * Mathf.Max(0.01f, playerSpeed);
      var travel = path / Mathf.Max(0.01f, travelSpeed);
      var build = path / Mathf.Max(0.01f, _battleBalance.TimerWuPerBuilding)
        * _battleBalance.TimerSecondsPerBuilding;
      return Mathf.Max(
        _battleBalance.TimerMinSeconds,
        _battleBalance.TimerSlack * (travel + build));
    }

    private static float HorizontalDistance(Vector3 first, Vector3 second)
    {
      first.y = 0f;
      second.y = 0f;
      return Vector3.Distance(first, second);
    }

    private void RespawnOtherSpecials(Transform player, Vector3 timerPosition)
    {
      OtherSpecialsMarker.Begin();
      try
      {
        var houseSet = _spawner.CurrentHouseSet;
        if (houseSet == null)
          return;

        _spawner.GetOtherSpecialPlacement(timerPosition, player.position, out var anchor, out var minDistance, out var maxDistance);

        foreach (var special in houseSet.Specials)
        {
          if (special.type == SpecialHouses.Timer || !special.enabled || special.prefab == null
              || _spawner.IsSpecialDestroyed(special.type))
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
      finally
      {
        OtherSpecialsMarker.End();
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
