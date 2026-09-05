using System;
using Arrow;
using Balance;
using Destruction;
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

    private const float ZigZagPlacementJitter = 4f;

    private readonly BattleService _battleService;
    private readonly MapEnvironmentSpawner _spawner;
    private readonly SpecialSpawnSettings _spawnSettings;
    private readonly BattleBalanceConfig _battleBalance;
    private readonly CharacterService _characterService;
    private readonly EconomyTelemetryService _telemetry;

    private int _respawnCount;
    private Vector3 _lastTimerPosition;
    private bool _hasLastTimerPosition;
    private Vector3 _routeOrigin;
    private bool _hasRouteOrigin;
    private float _routeProgress;

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
      _routeOrigin = default;
      _hasRouteOrigin = false;
      _routeProgress = 0f;
    }

    private void OnTimerDestroyed()
    {
      RespawnMarker.Begin();
      try
      {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
          return;

        if (_spawnSettings == null || !_spawnSettings.TryGetRespawnDistance(SpecialHouses.Timer, _respawnCount, out var minDistance, out var maxDistance))
          return;

        var secondsRemainingOnArrival = _battleService.Timer;
        var playerSpeedAtStart = _characterService.Speed;
        GameObject timerInstance = null;
        Vector3 hopOrigin;
        SpawnTimerMarker.Begin();
        try
        {
          if (!TrySpawnTimer(player.transform, minDistance, maxDistance, out timerInstance, out hopOrigin))
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
        _telemetry?.BeginTimerHop(_respawnCount, straightLineDistance, secondsGranted, playerSpeedAtStart);

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
      Transform player, float minDistance, float maxDistance, out GameObject timerInstance, out Vector3 hopOrigin)
    {
      timerInstance = null;
      hopOrigin = _hasLastTimerPosition ? _lastTimerPosition : player.position;
      if (!_hasLastTimerPosition)
        TryGetCurrentTimerPosition(player.position, out hopOrigin);

      if (!_hasRouteOrigin)
      {
        _routeOrigin = hopOrigin;
        _hasRouteOrigin = true;
        _routeProgress = 0f;
      }

      if (TryGetGoalPosition(out var goalPosition)
          && TryGetZigZagAnchor(
            _routeOrigin,
            goalPosition,
            minDistance,
            maxDistance,
            _respawnCount,
            _routeProgress,
            out var anchor,
            out var nextRouteProgress))
      {
        // TrySpawnSpecial still validates the candidate against live specials and TheGoal. A small
        // jitter around the directed anchor gives it a few nearby options without losing the route.
        if (_spawner.TrySpawnSpecial(
          SpecialHouses.Timer, anchor, player, 0f, ZigZagPlacementJitter, out timerInstance))
        {
          _routeProgress = nextRouteProgress;
          return true;
        }
      }

      // A missing goal, a short route, or a crowded candidate should not strand the battle. Keep the
      // original player-centered behavior as the safe fallback.
      return _spawner.TrySpawnSpecial(
        SpecialHouses.Timer, player.position, player, minDistance, maxDistance, out timerInstance);
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

    private static bool TryGetGoalPosition(out Vector3 goalPosition)
    {
      var goal = TheGoal.Current;
      if (goal == null)
        goal = UnityEngine.Object.FindFirstObjectByType<TheGoal>();

      if (goal == null || goal.IsDestroyed)
      {
        goalPosition = default;
        return false;
      }

      goalPosition = goal.transform.position;
      return true;
    }

    private bool TryGetZigZagAnchor(
      Vector3 routeOrigin,
      Vector3 goalPosition,
      float minDistance,
      float maxDistance,
      int respawnIndex,
      float routeProgress,
      out Vector3 anchor,
      out float nextRouteProgress)
    {
      var toGoal = goalPosition - routeOrigin;
      toGoal.y = 0f;
      var totalDistance = toGoal.magnitude;
      var remainingDistance = totalDistance - routeProgress;
      if (remainingDistance < 0.001f)
      {
        anchor = default;
        nextRouteProgress = routeProgress;
        return false;
      }

      var forward = toGoal / totalDistance;
      var forwardDistance = Mathf.Min(UnityEngine.Random.Range(minDistance, maxDistance), remainingDistance * 0.8f);
      if (forwardDistance < 0.001f)
      {
        anchor = default;
        nextRouteProgress = routeProgress;
        return false;
      }

      // Limit the lateral leg so the next point is still closer to the goal than the current one,
      // especially when the timer is on its final leg.
      var maxCloserLateralDistance = Mathf.Sqrt(
        Mathf.Max(0f, 2f * remainingDistance * forwardDistance - forwardDistance * forwardDistance));
      var lateralDistance = Mathf.Min(
        forwardDistance * _battleBalance.TimerLateralDistanceRatio,
        maxCloserLateralDistance);
      var side = new Vector3(-forward.z, 0f, forward.x);
      if ((respawnIndex & 1) == 0)
        side = -side;

      nextRouteProgress = routeProgress + forwardDistance;
      anchor = routeOrigin + forward * nextRouteProgress + side * lateralDistance;
      anchor.y = routeOrigin.y;
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
