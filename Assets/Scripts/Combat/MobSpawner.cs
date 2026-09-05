using System;
using System.Collections.Generic;
using App;
using Balance;
using Model;
using Movement;
using Pooling;
using UnityEngine;
using VContainer;

namespace Combat
{
  [DisallowMultipleComponent]
  public sealed class MobSpawner : MonoBehaviour
  {
    private const int LeftSide = 0;
    private const int RightSide = 1;
    private const int BottomSide = 2;
    private const int TopSide = 3;
    private const int SideCount = 4;
    private const int RepositionCheckInterval = 5;

    [Serializable]
    private sealed class SpawnItem
    {
      [SerializeField] private GameObject _mob;
      [SerializeField, Min(0f)] private float _mobsPerSecStart = 1f;
      [SerializeField, Min(0f)] private float _mobPerSecondEnd = 1f;
      [SerializeField, Min(0f)] private float _secondStart;
      [SerializeField, Min(0f)] private float _secondEnd = 60f;

      [NonSerialized] private float _spawnProgress;

      internal GameObject Mob => _mob;

      internal void ResetRuntime() =>
        _spawnProgress = 0f;

      internal void AddSpawnProgress(float amount)
      {
        _spawnProgress += Mathf.Max(0f, amount);
      }

      internal bool HasPendingSpawn => _spawnProgress >= 1f;

      internal void ConsumeSpawn() =>
        _spawnProgress -= 1f;

      internal float GetRate(float second)
      {
        if (second < _secondStart)
          return 0f;

        if (_secondEnd <= _secondStart)
          return _mobPerSecondEnd;

        var interpolation = Mathf.InverseLerp(_secondStart, _secondEnd, second);
        return Mathf.Lerp(_mobsPerSecStart, _mobPerSecondEnd, interpolation);
      }

      internal void Validate()
      {
        _mobsPerSecStart = Mathf.Max(0f, _mobsPerSecStart);
        _mobPerSecondEnd = Mathf.Max(0f, _mobPerSecondEnd);
        _secondStart = Mathf.Max(0f, _secondStart);
        _secondEnd = Mathf.Max(_secondStart, _secondEnd);
      }
    }

    [Header("Mob Types")]
    [SerializeField] private List<SpawnItem> _mobs = new();

    [Header("Spawn Position")]
    [SerializeField] private Transform _player;
    [SerializeField] private Camera _camera;
    [SerializeField, Min(0f)] private float _cameraViewportMargin = 0.05f;
    [SerializeField, Min(1)] private int _positionAttempts = 32;

    [Inject] private Pool _pool;
    [Inject] private MovementUpdater _movementUpdater;
    [Inject] private BattleService _battleService;
    [Inject] private BattleBalanceConfig _battleBalance;

    private float _elapsedSeconds;
    private int _nextSpawnSide;
    private int _repositionFrame;

    private void Awake() =>
      this.AsInjected();

    private void OnEnable()
    {
      _elapsedSeconds = 0f;
      _nextSpawnSide = 0;
      _repositionFrame = 0;
      for (var i = 0; i < _mobs.Count; i++)
        _mobs[i]?.ResetRuntime();
    }

    private void Update()
    {
      if (_battleService is not { IsBattleActive: true })
        return;

      _elapsedSeconds += Time.deltaTime;
      ResolveSceneReferences();

      if (_pool == null
          || _movementUpdater == null
          || _player == null
          || !_movementUpdater.HasWalkableFlowMap)
        return;

      if (++_repositionFrame >= RepositionCheckInterval)
      {
        _repositionFrame = 0;
        RepositionMobs();
      }

      for (var i = 0; i < _mobs.Count; i++)
      {
        var item = _mobs[i];
        if (item == null || item.Mob == null)
          continue;

        item.AddSpawnProgress(item.GetRate(_elapsedSeconds) * Time.deltaTime);
        while (item.HasPendingSpawn)
        {
          if (!TrySpawn(item.Mob))
            break;

          item.ConsumeSpawn();
        }
      }
    }

    private void RepositionMobs()
    {
      var agents = _movementUpdater.ActiveAgents;
      var repositionDistance = _battleBalance.DuckRepositionDistance;
      var repositionDistanceSquared = repositionDistance * repositionDistance;

      for (var i = 0; i < agents.Count; i++)
      {
        var agent = agents[i];
        if (agent == null || !agent.isActiveAndEnabled)
          continue;

        var mob = agent.GetComponent<Mob>();
        if (mob == null || !mob.IsAlive || mob.IsAttached)
          continue;

        var offset = agent.Position - _player.position;
        offset.y = 0f;
        if (offset.sqrMagnitude <= repositionDistanceSquared)
          continue;

        if (!TryGetSpawnPosition(out var position))
          continue;

        agent.Teleport(position);
        _nextSpawnSide = (_nextSpawnSide + 1) % SideCount;
      }
    }

    private bool TrySpawn(GameObject mobPrefab)
    {
      if (!TryGetSpawnPosition(out var position))
        return false;

      var spawned = _pool.Get(mobPrefab, position, Quaternion.identity) != null;
      if (spawned)
        _nextSpawnSide = (_nextSpawnSide + 1) % SideCount;

      return spawned;
    }

    private bool TryGetSpawnPosition(out Vector3 position)
    {
      if (TryGetSpawnPosition(_nextSpawnSide, out position))
        return true;

      return TryGetSpawnPosition(-1, out position);
    }

    private bool TryGetSpawnPosition(int requiredSide, out Vector3 position)
    {
      position = default;
      var camera = _camera != null ? _camera : Camera.main;

      if (camera == null)
        return false;

      for (var attempt = 0; attempt < _positionAttempts; attempt++)
      {
        var side = requiredSide >= 0
          ? requiredSide
          : UnityEngine.Random.Range(0, SideCount);
        var viewportPoint = GetSpawnViewportPoint(side);
        if (!TryGetGroundPoint(camera, viewportPoint, out var candidate))
          continue;

        if (!_movementUpdater.IsWalkable(candidate)
            || !IsOutsideCameraView(candidate, side))
          continue;

        position = candidate;
        return true;
      }

      return false;
    }

    private Vector2 GetSpawnViewportPoint(int side)
    {
      var edgePosition = UnityEngine.Random.value;
      var margin = Mathf.Max(0.001f, _cameraViewportMargin);

      if (side == LeftSide)
        return new Vector2(-margin, edgePosition);

      if (side == RightSide)
        return new Vector2(1f + margin, edgePosition);

      if (side == BottomSide)
        return new Vector2(edgePosition, -margin);

      return new Vector2(edgePosition, 1f + margin);
    }

    private static bool TryGetGroundPoint(
      Camera camera,
      Vector2 viewportPoint,
      out Vector3 groundPoint)
    {
      var ray = camera.ViewportPointToRay(viewportPoint);
      var ground = new Plane(Vector3.up, Vector3.zero);
      if (!ground.Raycast(ray, out var distance) || distance < 0f)
      {
        groundPoint = default;
        return false;
      }

      groundPoint = ray.GetPoint(distance);
      return true;
    }

    private bool IsOutsideCameraView(Vector3 position, int requiredSide)
    {
      var camera = _camera != null ? _camera : Camera.main;
      if (camera == null)
        return true;

      var viewportPosition = camera.WorldToViewportPoint(position);
      if (viewportPosition.z <= 0f)
        return requiredSide < 0;

      var left = viewportPosition.x <= -_cameraViewportMargin;
      var right = viewportPosition.x >= 1f + _cameraViewportMargin;
      var bottom = viewportPosition.y <= -_cameraViewportMargin;
      var top = viewportPosition.y >= 1f + _cameraViewportMargin;

      if (requiredSide == LeftSide)
        return left;

      if (requiredSide == RightSide)
        return right;

      if (requiredSide == BottomSide)
        return bottom;

      if (requiredSide == TopSide)
        return top;

      return left || right || bottom || top;
    }

    private void ResolveSceneReferences()
    {
      if (_player == null)
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;

      if (_camera == null)
        _camera = Camera.main;
    }

    private void OnValidate()
    {
      _cameraViewportMargin = Mathf.Max(0f, _cameraViewportMargin);
      _positionAttempts = Mathf.Max(1, _positionAttempts);

      for (var i = 0; i < _mobs.Count; i++)
        _mobs[i]?.Validate();
    }
  }
}
