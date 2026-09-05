using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Balance;
using Combat;
using Model;
using UnityEngine;
using UnityEngine.Scripting;
using VContainer.Unity;

namespace Telemetry
{
  [Preserve]
  public sealed class EconomyTelemetryService : IInitializable, ITickable, IDisposable
  {
    private const float SliceDuration = 30f;
    private const string BattleTimePolicy =
      "30-second slices use scaled battle time; upgrade UI time is excluded from battle time and charged to the slice active when the UI opened.";

    private readonly BattleService _battleService;
    private readonly CharacterService _characterService;
    private readonly BattleBalanceConfig _battleBalance;
    private readonly ProgressionBalanceConfig _progressionBalance;

    private RunDump _run;
    private RunSlice _currentSlice;
    private readonly List<RunSlice> _slices = new();
    private bool _runActive;
    private float _battleTime;
    private float _upgradeUiStart;
    private bool _upgradeUiShown;
    private Vector3 _spawnPosition;
    private Vector3 _lastPosition;
    private Transform _player;
    private MobSpawner _mobSpawner;
    private int _observedSpawnCount;
    private DateTime _runStartedAtUtc;

    public EconomyTelemetryService(
      BattleService battleService,
      CharacterService characterService,
      BattleBalanceConfig battleBalance,
      ProgressionBalanceConfig progressionBalance)
    {
      _battleService = battleService;
      _characterService = characterService;
      _battleBalance = battleBalance;
      _progressionBalance = progressionBalance;
    }

    void IInitializable.Initialize()
    {
      _battleService.BattleStarted += StartRun;
      _battleService.BattleWon += OnBattleWon;
      _battleService.BattleDefeated += OnBattleDefeated;
      _battleService.BattleAbandoned += OnBattleAbandoned;
      _characterService.DuckKilled += OnDuckKilled;
      _characterService.BuildingDestroyed += OnBuildingDestroyed;
      _characterService.MobCoinsEarned += OnMobCoinsEarned;
      _characterService.BuildingCoinsEarned += OnBuildingCoinsEarned;
      _characterService.DamageTaken += OnDamageTaken;
      _characterService.MobConnected += OnMobConnected;
      _characterService.UpgradePurchased += OnUpgradePurchased;
    }

    void ITickable.Tick()
    {
      if (!_runActive)
        return;

      ObserveSpawner();
      AccumulateMovement();

      var deltaTime = Time.deltaTime;
      if (deltaTime <= 0f)
        return;

      _battleTime += deltaTime;
      AdvanceSlices();
    }

    public void SetUpgradeUiShown(bool isShown)
    {
      if (!_runActive || _upgradeUiShown == isShown)
        return;

      if (isShown)
      {
        _upgradeUiStart = Time.unscaledTime;
        _upgradeUiShown = true;
        return;
      }

      AddUpgradeUiTime(Time.unscaledTime - _upgradeUiStart);
      _upgradeUiShown = false;
    }

    public void RecordMeleeSwing(int mobHits)
    {
      if (!_runActive)
        return;

      _currentSlice.meleeSwings++;
      _currentSlice.mobHitsPerSwing += Mathf.Max(0, mobHits);
      _currentSlice.maxMobsPerSwing = Mathf.Max(_currentSlice.maxMobsPerSwing, mobHits);
    }

    public void RecordBuildingHit()
    {
      if (_runActive)
        _currentSlice.buildingHits++;
    }

    void IDisposable.Dispose()
    {
      _battleService.BattleStarted -= StartRun;
      _battleService.BattleWon -= OnBattleWon;
      _battleService.BattleDefeated -= OnBattleDefeated;
      _battleService.BattleAbandoned -= OnBattleAbandoned;
      _characterService.DuckKilled -= OnDuckKilled;
      _characterService.BuildingDestroyed -= OnBuildingDestroyed;
      _characterService.MobCoinsEarned -= OnMobCoinsEarned;
      _characterService.BuildingCoinsEarned -= OnBuildingCoinsEarned;
      _characterService.DamageTaken -= OnDamageTaken;
      _characterService.MobConnected -= OnMobConnected;
      _characterService.UpgradePurchased -= OnUpgradePurchased;
    }

    private void StartRun()
    {
      if (_runActive)
        return;

      _runActive = true;
      _battleTime = 0f;
      _runStartedAtUtc = DateTime.UtcNow;
      _upgradeUiStart = 0f;
      _upgradeUiShown = false;
      _player = null;
      _mobSpawner = null;
      _observedSpawnCount = 0;
      _slices.Clear();

      _player = GameObject.FindGameObjectWithTag("Player")?.transform;
      _spawnPosition = _player != null ? _player.position : Vector3.zero;
      _lastPosition = _spawnPosition;
      ResolveSpawner();
      if (_mobSpawner != null)
        _observedSpawnCount = _mobSpawner.TotalSpawned;

      _run = new RunDump
      {
        header = new RunHeader
        {
          timestampUtc = null,
          outcome = "in_progress",
          durationSeconds = 0f,
          battleTimePolicy = BattleTimePolicy,
          balance = new BalanceSnapshot
          {
            battleBalanceConfigJson = null,
            progressionBalanceConfigJson = null,
          },
        },
        totals = new RunTotals(),
        slices = _slices,
        statTimeline = new List<PlayerStatPoint>(),
      };

      CreateSlice(0f, _lastPosition);
      CaptureStats(0f);
    }

    private void OnBattleWon() => FinishRun("win");

    private void OnBattleDefeated() => FinishRun("defeat");

    private void OnBattleAbandoned() => FinishRun("abandoned");

    private void OnDuckKilled()
    {
      if (_runActive)
        _currentSlice.mobsKilled++;
    }

    private void OnBuildingDestroyed()
    {
      if (_runActive)
        _currentSlice.buildingsDestroyed++;
    }

    private void OnMobCoinsEarned(int coins)
    {
      if (_runActive)
        _currentSlice.mobCoinsEarned += Mathf.Max(0, coins);
    }

    private void OnBuildingCoinsEarned(int coins)
    {
      if (_runActive)
        _currentSlice.buildingCoinsEarned += Mathf.Max(0, coins);
    }

    private void OnDamageTaken(int damage, bool fromMob)
    {
      if (!_runActive)
        return;

      var amount = Mathf.Max(0, damage);
      _currentSlice.damageTaken += amount;
      if (fromMob)
        _currentSlice.damageTakenFromMobs += amount;
      else
        _currentSlice.damageTakenFromOther += amount;
    }

    private void OnMobConnected()
    {
      if (_runActive)
        _currentSlice.mobsConnected++;
    }

    private void OnUpgradePurchased(UpgradeStat stat, int cost)
    {
      if (!_runActive)
        return;

      var amount = Mathf.Max(0, cost);
      switch (stat)
      {
        case UpgradeStat.Attack:
          _currentSlice.attackCoinsSpent += amount;
          _currentSlice.attackPurchases++;
          break;
        case UpgradeStat.Health:
          _currentSlice.healthCoinsSpent += amount;
          _currentSlice.healthPurchases++;
          break;
        case UpgradeStat.Speed:
          _currentSlice.speedCoinsSpent += amount;
          _currentSlice.speedPurchases++;
          break;
        case UpgradeStat.Gun:
          _currentSlice.gunCoinsSpent += amount;
          _currentSlice.gunPurchases++;
          break;
        case UpgradeStat.Timer:
          _currentSlice.timerCoinsSpent += amount;
          _currentSlice.timerPurchases++;
          break;
      }
    }

    private void AddUpgradeUiTime(float seconds)
    {
      if (_runActive)
        _currentSlice.upgradeUiSeconds += Mathf.Max(0f, seconds);
    }

    private void ObserveSpawner()
    {
      ResolveSpawner();
      if (_mobSpawner == null)
        return;

      var totalSpawned = _mobSpawner.TotalSpawned;
      var delta = totalSpawned - _observedSpawnCount;
      if (delta > 0)
        _currentSlice.mobsSpawned += delta;

      _observedSpawnCount = totalSpawned;
    }

    private void ResolveSpawner()
    {
      if (_mobSpawner == null)
        _mobSpawner = UnityEngine.Object.FindFirstObjectByType<MobSpawner>();
    }

    private void AccumulateMovement()
    {
      if (_player == null)
        return;

      var position = _player.position;
      var movement = position - _lastPosition;
      movement.y = 0f;
      if (movement.sqrMagnitude > 0f)
        _currentSlice.distanceTravelled += movement.magnitude;

      _lastPosition = position;
    }

    private void AdvanceSlices()
    {
      while (_battleTime >= _slices.Count * SliceDuration)
      {
        var boundary = _slices.Count * SliceDuration;
        FinishSlice(boundary, _lastPosition);
        CaptureStats(boundary);
        CreateSlice(boundary, _lastPosition);
      }
    }

    private void CreateSlice(float startTime, Vector3 startPosition)
    {
      _currentSlice = new RunSlice
      {
        startTimeSeconds = startTime,
        endTimeSeconds = startTime,
        startPosition = startPosition,
      };
      _slices.Add(_currentSlice);
    }

    private void FinishSlice(float endTime, Vector3 endPosition)
    {
      _currentSlice.endTimeSeconds = endTime;
      _currentSlice.durationSeconds = Mathf.Max(0f, endTime - _currentSlice.startTimeSeconds);
      _currentSlice.straightLineDisplacement = HorizontalDistance(
        _currentSlice.startPosition,
        endPosition);
      PopulateDerivedValues(_currentSlice);
    }

    private void CaptureStats(float timeSeconds)
    {
      _run.statTimeline.Add(new PlayerStatPoint
      {
        timeSeconds = timeSeconds,
        attackPower = _characterService.AttackPower,
        speed = _characterService.Speed,
        maxHealth = _characterService.MaxHealth,
      });
    }

    private void FinishRun(string outcome)
    {
      if (!_runActive)
        return;

      try
      {
        if (_upgradeUiShown)
        {
          AddUpgradeUiTime(Time.unscaledTime - _upgradeUiStart);
          _upgradeUiShown = false;
        }

        _runActive = false;

        ObserveSpawner();
        var endPosition = _player != null ? _player.position : _lastPosition;
        AccumulateMovement();
        if (_currentSlice.startTimeSeconds < _battleTime || _slices.Count == 1)
          FinishSlice(_battleTime, endPosition);
        else
          _slices.RemoveAt(_slices.Count - 1);

        if (_run.statTimeline.Count == 0
          || _run.statTimeline[_run.statTimeline.Count - 1].timeSeconds < _battleTime)
          CaptureStats(_battleTime);

        _run.header.outcome = outcome;
        _run.header.durationSeconds = _battleTime;
        _run.header.timestampUtc = _runStartedAtUtc.ToString("o", CultureInfo.InvariantCulture);
        _run.header.balance.battleBalanceConfigJson = JsonUtility.ToJson(_battleBalance);
        _run.header.balance.progressionBalanceConfigJson = JsonUtility.ToJson(_progressionBalance);
        _run.totals = BuildTotals(endPosition);
        _run.sanity = BuildSanityChecks();
        WriteDump();
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"Economy telemetry failed without affecting gameplay: {exception.Message}");
      }
    }

    private RunTotals BuildTotals(Vector3 endPosition)
    {
      var totals = new RunTotals();
      for (var i = 0; i < _slices.Count; i++)
      {
        var slice = _slices[i];
        totals.mobsSpawned += slice.mobsSpawned;
        totals.mobsKilled += slice.mobsKilled;
        totals.meleeSwings += slice.meleeSwings;
        totals.mobHitsPerSwing += slice.mobHitsPerSwing;
        totals.maxMobsPerSwing = Mathf.Max(totals.maxMobsPerSwing, slice.maxMobsPerSwing);
        totals.mobCoinsEarned += slice.mobCoinsEarned;
        totals.buildingCoinsEarned += slice.buildingCoinsEarned;
        totals.attackPurchases += slice.attackPurchases;
        totals.healthPurchases += slice.healthPurchases;
        totals.speedPurchases += slice.speedPurchases;
        totals.gunPurchases += slice.gunPurchases;
        totals.timerPurchases += slice.timerPurchases;
        totals.attackCoinsSpent += slice.attackCoinsSpent;
        totals.healthCoinsSpent += slice.healthCoinsSpent;
        totals.speedCoinsSpent += slice.speedCoinsSpent;
        totals.gunCoinsSpent += slice.gunCoinsSpent;
        totals.timerCoinsSpent += slice.timerCoinsSpent;
        totals.distanceTravelled += slice.distanceTravelled;
        totals.damageTaken += slice.damageTaken;
        totals.damageTakenFromMobs += slice.damageTakenFromMobs;
        totals.damageTakenFromOther += slice.damageTakenFromOther;
        totals.mobsConnected += slice.mobsConnected;
        totals.buildingsDestroyed += slice.buildingsDestroyed;
        totals.buildingHits += slice.buildingHits;
        totals.upgradeUiSeconds += slice.upgradeUiSeconds;
      }

      totals.coinsEarned = totals.mobCoinsEarned + totals.buildingCoinsEarned;
      totals.coinsSpent = totals.attackCoinsSpent
        + totals.healthCoinsSpent
        + totals.speedCoinsSpent
        + totals.gunCoinsSpent
        + totals.timerCoinsSpent;
      totals.killFraction = totals.mobsSpawned > 0
        ? totals.mobsKilled / (float)totals.mobsSpawned
        : 0f;
      totals.meanMobsPerSwing = totals.meleeSwings > 0
        ? totals.mobHitsPerSwing / (float)totals.meleeSwings
        : 0f;
      totals.straightLineDisplacement = HorizontalDistance(_spawnPosition, endPosition);
      totals.detourRatio = totals.straightLineDisplacement > 0f
        ? totals.distanceTravelled / totals.straightLineDisplacement
        : 0f;
      return totals;
    }

    private SanityChecks BuildSanityChecks()
    {
      var sliceMobsSpawned = 0;
      var sliceMobsKilled = 0;
      var sliceMobCoins = 0;
      var sliceBuildingCoins = 0;
      for (var i = 0; i < _slices.Count; i++)
      {
        sliceMobsSpawned += _slices[i].mobsSpawned;
        sliceMobsKilled += _slices[i].mobsKilled;
        sliceMobCoins += _slices[i].mobCoinsEarned;
        sliceBuildingCoins += _slices[i].buildingCoinsEarned;
      }

      return new SanityChecks
      {
        killsAtMostSpawns = _run.totals.mobsKilled <= _run.totals.mobsSpawned,
        coinSourcesSumToTotal = _run.totals.coinsEarned
          == _run.totals.mobCoinsEarned + _run.totals.buildingCoinsEarned,
        slicesSumToTotals = sliceMobsSpawned == _run.totals.mobsSpawned
          && sliceMobsKilled == _run.totals.mobsKilled
          && sliceMobCoins == _run.totals.mobCoinsEarned
          && sliceBuildingCoins == _run.totals.buildingCoinsEarned,
      };
    }

    private void WriteDump()
    {
      var directory = Application.isEditor
        ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "artifacts"))
        : Path.Combine(Application.persistentDataPath, "artifacts");
      Directory.CreateDirectory(directory);

      var json = JsonUtility.ToJson(_run, true);
      var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH-mm-ss.fff'Z'", CultureInfo.InvariantCulture);
      var path = Path.Combine(directory, $"run-{stamp}.json");
      var suffix = 1;
      while (File.Exists(path))
      {
        path = Path.Combine(directory, $"run-{stamp}-{suffix}.json");
        suffix++;
      }

      File.WriteAllText(path, json);
      Debug.Log($"Economy telemetry written to {path}");
    }

    private static void PopulateDerivedValues(RunSlice slice)
    {
      slice.coinsEarned = slice.mobCoinsEarned + slice.buildingCoinsEarned;
      slice.coinsSpent = slice.attackCoinsSpent
        + slice.healthCoinsSpent
        + slice.speedCoinsSpent
        + slice.gunCoinsSpent
        + slice.timerCoinsSpent;
      slice.killFraction = slice.mobsSpawned > 0
        ? slice.mobsKilled / (float)slice.mobsSpawned
        : 0f;
      slice.meanMobsPerSwing = slice.meleeSwings > 0
        ? slice.mobHitsPerSwing / (float)slice.meleeSwings
        : 0f;
    }

    private static float HorizontalDistance(Vector3 first, Vector3 second)
    {
      first.y = 0f;
      second.y = 0f;
      return Vector3.Distance(first, second);
    }

    [Serializable]
    private sealed class RunDump
    {
      public RunHeader header;
      public RunTotals totals;
      public List<RunSlice> slices;
      public List<PlayerStatPoint> statTimeline;
      public SanityChecks sanity;
    }

    [Serializable]
    private sealed class RunHeader
    {
      public string timestampUtc;
      public string outcome;
      public float durationSeconds;
      public string battleTimePolicy;
      public BalanceSnapshot balance;
    }

    [Serializable]
    private sealed class BalanceSnapshot
    {
      public string battleBalanceConfigJson;
      public string progressionBalanceConfigJson;
    }

    [Serializable]
    private sealed class RunTotals
    {
      public int mobsSpawned;
      public int mobsKilled;
      public float killFraction;
      public int meleeSwings;
      public int mobHitsPerSwing;
      public float meanMobsPerSwing;
      public int maxMobsPerSwing;
      public int coinsEarned;
      public int mobCoinsEarned;
      public int buildingCoinsEarned;
      public int coinsSpent;
      public int attackPurchases;
      public int healthPurchases;
      public int speedPurchases;
      public int gunPurchases;
      public int timerPurchases;
      public int attackCoinsSpent;
      public int healthCoinsSpent;
      public int speedCoinsSpent;
      public int gunCoinsSpent;
      public int timerCoinsSpent;
      public float distanceTravelled;
      public float straightLineDisplacement;
      public float detourRatio;
      public int damageTaken;
      public int damageTakenFromMobs;
      public int damageTakenFromOther;
      public int mobsConnected;
      public int buildingsDestroyed;
      public int buildingHits;
      public float upgradeUiSeconds;
    }

    [Serializable]
    private sealed class RunSlice
    {
      public float startTimeSeconds;
      public float endTimeSeconds;
      public float durationSeconds;
      public int mobsSpawned;
      public int mobsKilled;
      public float killFraction;
      public int meleeSwings;
      public int mobHitsPerSwing;
      public float meanMobsPerSwing;
      public int maxMobsPerSwing;
      public int coinsEarned;
      public int mobCoinsEarned;
      public int buildingCoinsEarned;
      public int coinsSpent;
      public int attackPurchases;
      public int healthPurchases;
      public int speedPurchases;
      public int gunPurchases;
      public int timerPurchases;
      public int attackCoinsSpent;
      public int healthCoinsSpent;
      public int speedCoinsSpent;
      public int gunCoinsSpent;
      public int timerCoinsSpent;
      public float distanceTravelled;
      public float straightLineDisplacement;
      public int damageTaken;
      public int damageTakenFromMobs;
      public int damageTakenFromOther;
      public int mobsConnected;
      public int buildingsDestroyed;
      public int buildingHits;
      public float upgradeUiSeconds;

      [NonSerialized] public Vector3 startPosition;
    }

    [Serializable]
    private sealed class PlayerStatPoint
    {
      public float timeSeconds;
      public int attackPower;
      public int speed;
      public int maxHealth;
    }

    [Serializable]
    private sealed class SanityChecks
    {
      public bool killsAtMostSpawns;
      public bool coinSourcesSumToTotal;
      public bool slicesSumToTotals;
    }
  }
}
