using System;
using UnityEngine;

namespace Combat
{
  [DisallowMultipleComponent]
  public sealed class MobAnimator : MonoBehaviour
  {
    [Serializable]
    private sealed class AnimationPreset
    {
      [SerializeField] private Vector3 _startPosition;
      [SerializeField] private Vector3 _endPosition;
      [SerializeField] private Vector3 _startScale = Vector3.one;
      [SerializeField] private Vector3 _endScale = Vector3.one;
      [SerializeField] private Vector3 _startRotation;
      [SerializeField] private Vector3 _endRotation;

      internal static AnimationPreset RandomBetween(
        AnimationPreset min,
        AnimationPreset max)
      {
        return new AnimationPreset
        {
          _startPosition = RandomBetween(min._startPosition, max._startPosition),
          _endPosition = RandomBetween(min._endPosition, max._endPosition),
          _startScale = RandomBetween(min._startScale, max._startScale),
          _endScale = RandomBetween(min._endScale, max._endScale),
          _startRotation = RandomBetween(min._startRotation, max._startRotation),
          _endRotation = RandomBetween(min._endRotation, max._endRotation),
        };
      }

      private static Vector3 RandomBetween(Vector3 min, Vector3 max)
      {
        return new Vector3(
          RandomBetween(min.x, max.x),
          RandomBetween(min.y, max.y),
          RandomBetween(min.z, max.z));
      }

      private static float RandomBetween(float min, float max) =>
        UnityEngine.Random.Range(Mathf.Min(min, max), Mathf.Max(min, max));

      internal Vector3 StartPosition => _startPosition;
      internal Vector3 EndPosition => _endPosition;
      internal Vector3 StartScale => _startScale;
      internal Vector3 EndScale => _endScale;
      internal Vector3 StartRotation => _startRotation;
      internal Vector3 EndRotation => _endRotation;
    }

    [Header("Animation")]
    [SerializeField, Min(0f)] private float _durationMin = 0.25f;
    [SerializeField, Min(0f)] private float _durationMax = 0.25f;
    [SerializeField] private bool _randomizeOnEnable = true;
    [SerializeField] private bool _loop = true;

    [Header("Preset")]
    [SerializeField] private AnimationPreset _preset = new();
    [SerializeField] private AnimationPreset _randomMin = new();
    [SerializeField] private AnimationPreset _randomMax = new();

    [Header("Local Position Curves")]
    [SerializeField] private AnimationCurve _positionXCurve = DefaultCurve();
    [SerializeField] private AnimationCurve _positionYCurve = DefaultCurve();
    [SerializeField] private AnimationCurve _positionZCurve = DefaultCurve();

    [Header("Local Rotation Curves")]
    [SerializeField] private AnimationCurve _rotationXCurve = DefaultCurve();
    [SerializeField] private AnimationCurve _rotationYCurve = DefaultCurve();
    [SerializeField] private AnimationCurve _rotationZCurve = DefaultCurve();

    [Header("Scale Curves")]
    [SerializeField] private AnimationCurve _scaleXCurve = DefaultCurve();
    [SerializeField] private AnimationCurve _scaleYCurve = DefaultCurve();
    [SerializeField] private AnimationCurve _scaleZCurve = DefaultCurve();

    private AnimationPreset _activePreset;
    private float _duration;
    private float _elapsed;
    private bool _isPlaying;

    private void OnEnable()
    {
      _activePreset = _randomizeOnEnable
        ? AnimationPreset.RandomBetween(_randomMin, _randomMax)
        : _preset;
      _duration = UnityEngine.Random.Range(_durationMin, _durationMax);
      _elapsed = 0f;
      _isPlaying = true;

      Apply(0f);
      if (_duration <= 0f)
      {
        Apply(1f);
        _isPlaying = false;
      }
    }

    private void Update()
    {
      if (!_isPlaying)
        return;

      _elapsed += Time.deltaTime;
      var progress = _loop
        ? Mathf.PingPong(_elapsed / _duration, 1f)
        : Mathf.Clamp01(_elapsed / _duration);
      Apply(progress);

      if (!_loop && progress >= 1f)
        _isPlaying = false;
    }

    private void Apply(float progress)
    {
      var position = new Vector3(
        Interpolate(_activePreset.StartPosition.x, _activePreset.EndPosition.x,
          _positionXCurve, progress),
        Interpolate(_activePreset.StartPosition.y, _activePreset.EndPosition.y,
          _positionYCurve, progress),
        Interpolate(_activePreset.StartPosition.z, _activePreset.EndPosition.z,
          _positionZCurve, progress));

      var rotation = new Vector3(
        Interpolate(_activePreset.StartRotation.x, _activePreset.EndRotation.x,
          _rotationXCurve, progress),
        Interpolate(_activePreset.StartRotation.y, _activePreset.EndRotation.y,
          _rotationYCurve, progress),
        Interpolate(_activePreset.StartRotation.z, _activePreset.EndRotation.z,
          _rotationZCurve, progress));

      var scale = new Vector3(
        Interpolate(_activePreset.StartScale.x, _activePreset.EndScale.x,
          _scaleXCurve, progress),
        Interpolate(_activePreset.StartScale.y, _activePreset.EndScale.y,
          _scaleYCurve, progress),
        Interpolate(_activePreset.StartScale.z, _activePreset.EndScale.z,
          _scaleZCurve, progress));

      transform.localPosition = position;
      transform.localEulerAngles = rotation;
      transform.localScale = scale;
    }

    private static float Interpolate(
      float start,
      float end,
      AnimationCurve curve,
      float progress)
    {
      var curveProgress = curve == null ? progress : curve.Evaluate(progress);
      return Mathf.LerpUnclamped(start, end, curveProgress);
    }

    private static AnimationCurve DefaultCurve() =>
      AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private void OnValidate()
    {
      _durationMin = Mathf.Max(0f, _durationMin);
      _durationMax = Mathf.Max(_durationMin, _durationMax);
      _preset ??= new AnimationPreset();
      _randomMin ??= new AnimationPreset();
      _randomMax ??= new AnimationPreset();
      _positionXCurve ??= DefaultCurve();
      _positionYCurve ??= DefaultCurve();
      _positionZCurve ??= DefaultCurve();
      _rotationXCurve ??= DefaultCurve();
      _rotationYCurve ??= DefaultCurve();
      _rotationZCurve ??= DefaultCurve();
      _scaleXCurve ??= DefaultCurve();
      _scaleYCurve ??= DefaultCurve();
      _scaleZCurve ??= DefaultCurve();
    }
  }
}
