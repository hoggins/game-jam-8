using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace CameraShake
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(CinemachineBasicMultiChannelPerlin))]
  [AddComponentMenu("Camera Shake/Camera Shake Manager")]
  /// <summary>Combines active shake requests and drives Cinemachine's noise amplitude.</summary>
  public sealed class CameraShakeManager : MonoBehaviour
  {
    [SerializeField] private CinemachineBasicMultiChannelPerlin _noise;

    private readonly List<ActiveShake> _activeShakes = new();
    private float _baseAmplitudeGain;
    private bool _initialized;

    private void Awake()
    {
      if (_noise == null)
        _noise = GetComponent<CinemachineBasicMultiChannelPerlin>();

      if (_noise == null)
      {
        Debug.LogError("Camera shake needs a CinemachineBasicMultiChannelPerlin component.", this);
        enabled = false;
        return;
      }

      _baseAmplitudeGain = _noise.AmplitudeGain;
      _initialized = true;
    }

    public void Play(float duration, float magnitude, AnimationCurve falloff = null)
    {
      if (!_initialized)
        return;

      _activeShakes.Add(new ActiveShake(
        Mathf.Max(0.0001f, duration),
        Mathf.Max(0f, magnitude),
        falloff));
    }

    private void Update()
    {
      var amplitude = 0f;

      for (var i = _activeShakes.Count - 1; i >= 0; i--)
      {
        var shake = _activeShakes[i];
        shake.Elapsed += Time.deltaTime;

        if (shake.Elapsed >= shake.Duration)
        {
          _activeShakes.RemoveAt(i);
          continue;
        }

        var normalizedTime = Mathf.Clamp01(shake.Elapsed / shake.Duration);
        var falloff = shake.Falloff == null
          ? 1f - normalizedTime
          : Mathf.Clamp01(shake.Falloff.Evaluate(normalizedTime));
        amplitude = Mathf.Max(amplitude, shake.Magnitude * falloff);
      }

      _noise.AmplitudeGain = amplitude;
    }

    private void OnDisable()
    {
      _activeShakes.Clear();

      if (_initialized && _noise != null)
        _noise.AmplitudeGain = _baseAmplitudeGain;
    }

    private sealed class ActiveShake
    {
      public readonly float Duration;
      public readonly float Magnitude;
      public readonly AnimationCurve Falloff;
      public float Elapsed;

      public ActiveShake(float duration, float magnitude, AnimationCurve falloff)
      {
        Duration = duration;
        Magnitude = magnitude;
        Falloff = falloff == null ? null : new AnimationCurve(falloff.keys);
      }
    }
  }
}
