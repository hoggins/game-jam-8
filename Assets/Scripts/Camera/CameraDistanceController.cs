using System;
using Balance;
using Destruction;
using Model;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using VContainer.Unity;

namespace CustomCamera
{
  [Preserve]
  public sealed class CameraDistanceController : IInitializable, ITickable, IDisposable
  {
    private const string PlayerTag = "Player";
    private const float AdditionalCameraScaleFactor = -0.7f;
    private const float GoalZoomStartDistance = 40f;
    private const float GoalZoomFullDistance = 10f;

    private const float GoalAdditionalZoomDistance = 10f;
    private const float GoalZoomSmoothTime = 0.5f;
    private const string GoalDestructionCameraPath = "Prefabs/GoalDestructionCamera";
    private const float MinimumGoalDestructionOrbitDuration = 0.1f;

    private readonly CharacterService _characterService;
    private readonly BattleBalanceConfig _battleBalance;

    private CinemachineOrbitalFollow _orbitalFollow;
    private GameObject _goalDestructionObject;
    private CinemachineCamera _goalDestructionCamera;
    private CinemachineSplineDolly _goalDestructionDolly;
    private TheGoal _goal;
    private Transform _player;
    private float _authoredRadius;
    private Vector3 _authoredTargetOffset;
    private float _goalZoomDistance;
    private float _goalZoomVelocity;
    private float _goalDestructionCameraTime;

    public CameraDistanceController(
      CharacterService characterService,
      BattleBalanceConfig battleBalance)
    {
      _characterService = characterService;
      _battleBalance = battleBalance;
    }

    void IInitializable.Initialize()
    {
      _characterService.ProgressionChanged += OnProgressionChanged;
      SceneManager.sceneLoaded += OnSceneLoaded;
      TryBindCamera();
      TryBindGoal();
    }

    private void OnProgressionChanged() =>
      TryBindCamera();

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      _player = null;
      _goalDestructionObject = null;
      _goalDestructionCamera = null;
      _goalDestructionDolly = null;
      _goalDestructionCameraTime = 0f;
      TryBindGoal();
      TryBindCamera();
    }

    private void TryBindCamera()
    {
      var orbitalFollow = UnityEngine.Object.FindFirstObjectByType<CinemachineOrbitalFollow>();
      if (orbitalFollow == null)
        return;

      if (_orbitalFollow != orbitalFollow)
      {
        _orbitalFollow = orbitalFollow;
        _authoredRadius = orbitalFollow.Radius;
        _authoredTargetOffset = orbitalFollow.TargetOffset;
        _goalZoomDistance = 0f;
        _goalZoomVelocity = 0f;
      }

      Refresh();
    }

    void ITickable.Tick()
    {
      if (_orbitalFollow == null)
        TryBindCamera();

      if (_orbitalFollow != null)
      {
        var targetZoomDistance = GetGoalZoomDistance();
        _goalZoomDistance = Mathf.SmoothDamp(
          _goalZoomDistance,
          targetZoomDistance,
          ref _goalZoomVelocity,
          GoalZoomSmoothTime);
        Refresh();
      }

      if (_goal == null && TheGoal.Current != null)
        BindGoal(TheGoal.Current);

      UpdateGoalDestructionCamera();
    }

    private void Refresh()
    {
      if (_orbitalFollow == null)
        return;

      var scaleFactor = _characterService.CharacterScaleFactor + (_characterService.CharacterScaleFactor -1f) * AdditionalCameraScaleFactor;
      _orbitalFollow.Radius = _authoredRadius * scaleFactor + _goalZoomDistance;
      _orbitalFollow.TargetOffset = _authoredTargetOffset * scaleFactor;
    }

    private void TryBindGoal()
    {
      var goal = TheGoal.Current;
      if (goal == null)
        goal = UnityEngine.Object.FindFirstObjectByType<TheGoal>();

      BindGoal(goal);
    }

    private void BindGoal(TheGoal goal)
    {
      if (_goal == goal)
        return;

      if (_goal != null)
        _goal.Destroyed -= OnGoalDestroyed;

      _goal = goal;
      if (_goal != null)
        _goal.Destroyed += OnGoalDestroyed;
    }

    private void OnGoalDestroyed(TheGoal goal)
    {
      if (goal != null)
        SpawnGoalDestructionCamera(goal.transform.position);
    }

    private void SpawnGoalDestructionCamera(Vector3 goalPosition)
    {
      if (_goalDestructionCamera != null)
        return;

      var prefab = Resources.Load<GameObject>(GoalDestructionCameraPath);
      if (prefab == null)
      {
        Debug.LogError($"Goal destruction camera prefab not found at Resources/{GoalDestructionCameraPath}.prefab.");
        return;
      }

      var cameraObject = UnityEngine.Object.Instantiate(
        prefab,
        goalPosition,
        Quaternion.identity);
      cameraObject.name = prefab.name;

      _goalDestructionObject = cameraObject;
      _goalDestructionCamera = cameraObject.GetComponentInChildren<CinemachineCamera>();
      _goalDestructionDolly = cameraObject.GetComponentInChildren<CinemachineSplineDolly>();
      if (_goalDestructionCamera == null || _goalDestructionDolly == null)
      {
        Debug.LogError(
          $"Goal destruction camera prefab must contain {nameof(CinemachineCamera)} and {nameof(CinemachineSplineDolly)}.");
        UnityEngine.Object.Destroy(cameraObject);
        _goalDestructionObject = null;
        _goalDestructionCamera = null;
        _goalDestructionDolly = null;
        return;
      }

      _goalDestructionCameraTime = 0f;
    }

    private void UpdateGoalDestructionCamera()
    {
      if (_goalDestructionDolly == null)
        return;

      _goalDestructionCameraTime += Time.deltaTime;
      _goalDestructionDolly.CameraPosition = Mathf.Repeat(
        _goalDestructionCameraTime / Mathf.Max(
          MinimumGoalDestructionOrbitDuration,
          _battleBalance.WinGraceDuration),
        1f);
    }

    private float GetGoalZoomDistance()
    {
      if (_player == null)
        _player = GameObject.FindGameObjectWithTag(PlayerTag)?.transform;

      var goal = TheGoal.Current;
      var health = goal != null ? goal.GetComponent<DestructibleHealth>() : null;
      if (_player == null
        || goal == null
        || health == null
        || health.ObjectType != DestructibleObjectType.Goal
        || goal.IsDestroyed)
        return 0f;

      var offset = goal.transform.position - _player.position;
      offset.y = 0f;
      var distance = offset.magnitude;
      var progress = Mathf.InverseLerp(GoalZoomStartDistance, GoalZoomFullDistance, distance);
      progress = progress * progress * (3f - 2f * progress);
      return GoalAdditionalZoomDistance * progress;
    }

    void IDisposable.Dispose()
    {
      _characterService.ProgressionChanged -= OnProgressionChanged;
      SceneManager.sceneLoaded -= OnSceneLoaded;
      if (_goal != null)
        _goal.Destroyed -= OnGoalDestroyed;

      if (_goalDestructionObject != null)
        UnityEngine.Object.Destroy(_goalDestructionObject);

      _orbitalFollow = null;
      _goalDestructionObject = null;
      _goalDestructionCamera = null;
      _goalDestructionDolly = null;
      _goal = null;
      _player = null;
    }
  }
}
