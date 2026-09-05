using System;
using System.Collections.Generic;
using App;
using Balance;
using Destruction;
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
    private const float RepositionOutsideScreenDistance = 3f;
    private const float SpawnRaycastHeight = 100f;
    private const float SpawnRaycastDistance = 200f;

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
    [SerializeField] private UnityEngine.Camera _camera;
    [SerializeField, Min(0f)] private float _cameraViewportMargin = 0.05f;
    [SerializeField, Min(1)] private int _positionAttempts = 32;

    [Inject] private Pool _pool;
    [Inject] private MovementUpdater _movementUpdater;
    [Inject] private BattleService _battleService;
    [Inject] private BattleBalanceConfig _battleBalance;

    private float _elapsedSeconds;
    private int _nextSpawnSide;
    private int _repositionFrame;
    private int _damagableLayerMask;
    private bool _isSpawning;
    private int _liveMobCount;
    private int _totalSpawned;
    private Vector3 _playerMovementSum;
    private Vector3 _previousPlayerPosition;
    private int _playerMovementSamples;
    private bool _hasPreviousPlayerPosition;

    public int TotalSpawned => _totalSpawned;

    private void Awake()
    {
      this.AsInjected();
      _damagableLayerMask = LayerMask.GetMask(DestructibleLayers.Damagable);
    }

    private void OnEnable()
    {
      _elapsedSeconds = 0f;
      _nextSpawnSide = 0;
      _repositionFrame = 0;
      _isSpawning = true;
      _liveMobCount = 0;
      _totalSpawned = 0;
      _playerMovementSum = Vector3.zero;
      _previousPlayerPosition = Vector3.zero;
      _playerMovementSamples = 0;
      _hasPreviousPlayerPosition = false;
      for (var i = 0; i < _mobs.Count; i++)
        _mobs[i]?.ResetRuntime();

      if (_battleService != null)
        _battleService.BattleWinStarted += StopSpawning;
    }

    private void OnDisable()
    {
      if (_battleService != null)
        _battleService.BattleWinStarted -= StopSpawning;
    }

    private void Update()
    {
      if (!_isSpawning || _battleService is not { IsBattleActive: true })
        return;

      _elapsedSeconds += Time.deltaTime;
      ResolveSceneReferences();

      if (_pool == null
          || _movementUpdater == null
          || _player == null
          || !_movementUpdater.HasWalkableFlowMap)
        return;

      AccumulatePlayerMovement();

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

        if (_battleBalance.MaxLiveMobs > 0 && _liveMobCount >= _battleBalance.MaxLiveMobs)
          continue;

        item.AddSpawnProgress(item.GetRate(_elapsedSeconds) * Time.deltaTime);
        while (item.HasPendingSpawn)
        {
          if (_battleBalance.MaxLiveMobs > 0 && _liveMobCount >= _battleBalance.MaxLiveMobs)
            break;

          if (!TrySpawn(item.Mob))
            break;

          _totalSpawned++;
          _liveMobCount++;
          item.ConsumeSpawn();
        }
      }
    }

    private void RepositionMobs()
    {
      var agents = _movementUpdater.ActiveAgents;
      var repositionDistance = _battleBalance.DuckRepositionDistance;
      var repositionDistanceSquared = repositionDistance * repositionDistance;
      var averagePlayerMovement = _playerMovementSamples > 0
        ? _playerMovementSum / _playerMovementSamples
        : Vector3.zero;
      var repositionSide = GetRepositionSide(averagePlayerMovement);
      _playerMovementSum = Vector3.zero;
      _playerMovementSamples = 0;
      _liveMobCount = 0;

      for (var i = 0; i < agents.Count; i++)
      {
        var agent = agents[i];
        if (agent == null || !agent.isActiveAndEnabled)
          continue;

        var mob = agent.GetComponent<Mob>();
        if (mob == null || !mob.IsAlive)
          continue;

        _liveMobCount++;
        if (mob.IsAttached)
          continue;

        var offset = agent.Position - _player.position;
        offset.y = 0f;
        var isFarFromPlayer = offset.sqrMagnitude > repositionDistanceSquared;
        if (!isFarFromPlayer
            && !IsBeyondCameraView(agent.Position, RepositionOutsideScreenDistance))
          continue;

        var radius = agent.Controller?.Radius ?? 0f;
        if (!TryGetRepositionPosition(repositionSide, radius, out var position))
          continue;

        agent.Teleport(position);
        _nextSpawnSide = (_nextSpawnSide + 1) % SideCount;
      }
    }

    private void AccumulatePlayerMovement()
    {
      var currentPosition = _player.position;
      if (!_hasPreviousPlayerPosition)
      {
        _previousPlayerPosition = currentPosition;
        _hasPreviousPlayerPosition = true;
        return;
      }

      var movement = currentPosition - _previousPlayerPosition;
      movement.y = 0f;
      _previousPlayerPosition = currentPosition;
      _playerMovementSum += movement;
      _playerMovementSamples++;
    }

    private int GetRepositionSide(Vector3 averagePlayerMovement)
    {
      if (averagePlayerMovement.sqrMagnitude <= 0.0001f)
        return _nextSpawnSide;

      var camera = _camera != null ? _camera : UnityEngine.Camera.main;
      if (camera == null)
        return _nextSpawnSide;

      var start = camera.WorldToViewportPoint(_player.position);
      var end = camera.WorldToViewportPoint(_player.position + averagePlayerMovement);
      var screenMovement = end - start;
      if (screenMovement.x * screenMovement.x + screenMovement.y * screenMovement.y <= 0.0001f)
        return _nextSpawnSide;

      if (Mathf.Abs(screenMovement.x) >= Mathf.Abs(screenMovement.y))
        return screenMovement.x >= 0f ? RightSide : LeftSide;

      return screenMovement.y >= 0f ? TopSide : BottomSide;
    }

    private bool TryGetRepositionPosition(int side, float radius, out Vector3 position)
    {
      position = default;
      var camera = _camera != null ? _camera : UnityEngine.Camera.main;
      if (camera == null)
        return false;

      var outwardDirection = GetScreenOutwardDirection(camera, side);
      if (outwardDirection.sqrMagnitude <= 0.0001f)
        return false;

      for (var attempt = 0; attempt < _positionAttempts; attempt++)
      {
        var edgePoint = GetCameraEdgeViewportPoint(side);
        if (!TryGetGroundPoint(camera, edgePoint, out var candidate))
          continue;

        candidate += outwardDirection * RepositionOutsideScreenDistance;
        if (!_movementUpdater.IsInsideLevelBounds(candidate, radius)
            || IsOnDamagableLayer(candidate)
            || (_movementUpdater.IsInsideFlowMap(candidate)
                && !_movementUpdater.IsWalkable(candidate)))
          continue;

        position = candidate;
        return true;
      }

      return false;
    }

    private bool IsBeyondCameraView(Vector3 position, float distance)
    {
      var camera = _camera != null ? _camera : UnityEngine.Camera.main;
      if (camera == null)
        return true;

      var viewportPosition = camera.WorldToViewportPoint(position);
      if (viewportPosition.z <= 0f)
        return true;

      var verticalScreenSize = camera.orthographic
        ? camera.orthographicSize * 2f
        : 2f * viewportPosition.z * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
      var horizontalScreenSize = verticalScreenSize * camera.aspect;
      if (verticalScreenSize <= 0f || horizontalScreenSize <= 0f)
        return false;

      var horizontalMargin = distance / horizontalScreenSize;
      var verticalMargin = distance / verticalScreenSize;
      return viewportPosition.x <= -horizontalMargin
        || viewportPosition.x >= 1f + horizontalMargin
        || viewportPosition.y <= -verticalMargin
        || viewportPosition.y >= 1f + verticalMargin;
    }

    private static Vector3 GetScreenOutwardDirection(UnityEngine.Camera camera, int side)
    {
      var direction = side == LeftSide || side == RightSide
        ? Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized
        : Vector3.ProjectOnPlane(camera.transform.up, Vector3.up).normalized;

      if (side == LeftSide || side == BottomSide)
        direction = -direction;

      return direction;
    }

    private static Vector2 GetCameraEdgeViewportPoint(int side)
    {
      var edgePosition = UnityEngine.Random.value;
      if (side == LeftSide)
        return new Vector2(0f, edgePosition);

      if (side == RightSide)
        return new Vector2(1f, edgePosition);

      if (side == BottomSide)
        return new Vector2(edgePosition, 0f);

      return new Vector2(edgePosition, 1f);
    }

    private bool TrySpawn(GameObject mobPrefab)
    {
      var mobMovement = mobPrefab.GetComponent<MobMovement>();
      var radius = mobMovement != null ? mobMovement.Radius : 0f;
      if (!TryGetSpawnPosition(radius, out var position))
        return false;

      var spawned = _pool.Get(mobPrefab, position, Quaternion.identity);
      if (spawned == null)
        return false;

      var agent = spawned.GetComponent<MovementAgent>();
      var actualRadius = agent?.Controller?.Radius ?? radius;
      if (!_movementUpdater.IsInsideLevelBounds(position, actualRadius))
      {
        _pool.Release(spawned);
        return false;
      }

      _nextSpawnSide = (_nextSpawnSide + 1) % SideCount;

      return true;
    }

    private bool TryGetSpawnPosition(float radius, out Vector3 position)
    {
      if (TryGetSpawnPosition(_nextSpawnSide, radius, out position))
        return true;

      return TryGetSpawnPosition(-1, radius, out position);
    }

    private bool TryGetSpawnPosition(int requiredSide, float radius, out Vector3 position)
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

        if (!IsOutsideCameraView(candidate, side)
            || !_movementUpdater.IsInsideLevelBounds(candidate, radius)
            || IsOnDamagableLayer(candidate)
            || (_movementUpdater.IsInsideFlowMap(candidate)
                && !_movementUpdater.IsWalkable(candidate)))
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
      UnityEngine.Camera camera,
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

    private bool IsOnDamagableLayer(Vector3 position) =>
      Physics.Raycast(
        position + Vector3.up * SpawnRaycastHeight,
        Vector3.down,
        SpawnRaycastDistance,
        _damagableLayerMask,
        QueryTriggerInteraction.Ignore);

    private bool IsOutsideCameraView(Vector3 position, int requiredSide)
    {
      var camera = _camera != null ? _camera : UnityEngine.Camera.main;
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

    private void StopSpawning() =>
      _isSpawning = false;

    private void OnValidate()
    {
      _cameraViewportMargin = Mathf.Max(0f, _cameraViewportMargin);
      _positionAttempts = Mathf.Max(1, _positionAttempts);

      for (var i = 0; i < _mobs.Count; i++)
        _mobs[i]?.Validate();
    }
  }
}
