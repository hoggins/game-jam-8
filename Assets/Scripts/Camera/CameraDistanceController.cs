using System;
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

    private readonly CharacterService _characterService;

    private CinemachineOrbitalFollow _orbitalFollow;
    private Transform _player;
    private float _authoredRadius;
    private Vector3 _authoredTargetOffset;
    private float _goalZoomDistance;
    private float _goalZoomVelocity;

    public CameraDistanceController(CharacterService characterService)
    {
      _characterService = characterService;
    }

    void IInitializable.Initialize()
    {
      _characterService.ProgressionChanged += OnProgressionChanged;
      SceneManager.sceneLoaded += OnSceneLoaded;
      TryBindCamera();
    }

    private void OnProgressionChanged() =>
      TryBindCamera();

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      _player = null;
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
      {
        TryBindCamera();
        if (_orbitalFollow == null)
          return;
      }

      var targetZoomDistance = GetGoalZoomDistance();
      _goalZoomDistance = Mathf.SmoothDamp(
        _goalZoomDistance,
        targetZoomDistance,
        ref _goalZoomVelocity,
        GoalZoomSmoothTime);
      Refresh();
    }

    private void Refresh()
    {
      if (_orbitalFollow == null)
        return;

      var scaleFactor = _characterService.CharacterScaleFactor + (_characterService.CharacterScaleFactor -1f) * AdditionalCameraScaleFactor;
      _orbitalFollow.Radius = _authoredRadius * scaleFactor + _goalZoomDistance;
      _orbitalFollow.TargetOffset = _authoredTargetOffset * scaleFactor;
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
      _orbitalFollow = null;
      _player = null;
    }
  }
}
