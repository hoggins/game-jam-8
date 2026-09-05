using System;
using Model;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using VContainer.Unity;

namespace CustomCamera
{
  [Preserve]
  public sealed class CameraDistanceController : IInitializable, IDisposable
  {
    private const float AdditionalCameraScaleFactor = -0.7f;

    private readonly CharacterService _characterService;

    private CinemachineOrbitalFollow _orbitalFollow;
    private float _authoredRadius;
    private Vector3 _authoredTargetOffset;

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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) =>
      TryBindCamera();

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
      }

      Refresh();
    }

    private void Refresh()
    {
      if (_orbitalFollow == null)
        return;

      var scaleFactor = _characterService.CharacterScaleFactor + (_characterService.CharacterScaleFactor -1f) * AdditionalCameraScaleFactor;
      _orbitalFollow.Radius = _authoredRadius * scaleFactor;
      _orbitalFollow.TargetOffset = _authoredTargetOffset * scaleFactor;
    }

    void IDisposable.Dispose()
    {
      _characterService.ProgressionChanged -= OnProgressionChanged;
      SceneManager.sceneLoaded -= OnSceneLoaded;
      _orbitalFollow = null;
    }
  }
}
