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
    private readonly CharacterService _characterService;

    private CinemachineOrbitalFollow _orbitalFollow;
    private float _authoredRadius;

    public CameraDistanceController(CharacterService characterService)
    {
      _characterService = characterService;
    }

    void IInitializable.Initialize()
    {
      _characterService.ProgressionChanged += Refresh;
      SceneManager.sceneLoaded += OnSceneLoaded;
      TryBindCamera();
    }

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
      }

      Refresh();
    }

    private void Refresh()
    {
      if (_orbitalFollow == null)
        return;

      _orbitalFollow.Radius = _authoredRadius * _characterService.CharacterScaleFactor;
    }

    void IDisposable.Dispose()
    {
      _characterService.ProgressionChanged -= Refresh;
      SceneManager.sceneLoaded -= OnSceneLoaded;
      _orbitalFollow = null;
    }
  }
}
