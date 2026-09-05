using System.Collections;
using App;
using Model;
using UnityEngine;
using VContainer;

namespace Combat
{
  /// Drives the _Hit property on every renderer below an actor.
  [DisallowMultipleComponent]
  public sealed class ActorHitAnimation : MonoBehaviour
  {
    private const int CurveInverseSamples = 64;
    private static readonly int HitId = Shader.PropertyToID("_Hit");

    [Header("Hit Animation")]
    [Tooltip("Allows this animation to be switched off without removing the component.")]
    [SerializeField] private bool _enabled = true;
    [Tooltip("Time spent moving the normalized _Hit value from 0 to 1.")]
    [SerializeField, Min(0f)] private float _applyDuration = 0.1f;
    [Tooltip("Normalized _Hit value over the apply duration. It should start at 0 and end at 1.")]
    [SerializeField] private AnimationCurve _applyCurve = DefaultApplyCurve();
    [Tooltip("Time spent moving the normalized _Hit value from 1 back to 0.")]
    [SerializeField, Min(0f)] private float _decayDuration = 0.25f;
    [Tooltip("Normalized _Hit value over the decay duration. It should start at 1 and end at 0.")]
    [SerializeField] private AnimationCurve _decayCurve = DefaultDecayCurve();

    [Header("Player")]
    [Tooltip("Play this animation when CharacterService reports damage to the player.")]
    [SerializeField] private bool _listenToPlayerDamage;

    [Inject] private CharacterService _characterService;

    private enum Phase
    {
      Applying,
      Decaying,
    }

    private Renderer[] _renderers;
    private MaterialPropertyBlock _propertyBlock;
    private Coroutine _animationCoroutine;
    private Phase _phase;
    private float _phaseElapsed;
    private float _hitValue;

    private void Awake()
    {
      _renderers = GetComponentsInChildren<Renderer>(true);
      _propertyBlock = new MaterialPropertyBlock();

      if (_listenToPlayerDamage)
        this.AsInjected();

      SetHit(0f);
    }

    private void OnEnable()
    {
      if (_listenToPlayerDamage && _characterService != null)
        _characterService.Damaged += PlayHit;

      _hitValue = 0f;
      SetHit(0f);
    }

    public void PlayHit()
    {
      if (!_enabled || !isActiveAndEnabled || _renderers == null || _renderers.Length == 0)
        return;

      if (_animationCoroutine == null)
      {
        _phase = Phase.Applying;
        _phaseElapsed = FindCurveProgress(_applyCurve, _hitValue) * _applyDuration;
        _animationCoroutine = StartCoroutine(AnimateHit());
      }
      else if (_phase == Phase.Decaying)
      {
        // Keep the current value and continue applying it at the authored apply speed. Hits during
        // the apply phase leave the existing progress alone, so repeated hits never restart it.
        _phase = Phase.Applying;
        _phaseElapsed = FindCurveProgress(_applyCurve, _hitValue) * _applyDuration;
      }
    }

    private IEnumerator AnimateHit()
    {
      while (true)
      {
        if (_phase == Phase.Applying)
        {
          if (_applyDuration <= 0f)
          {
            _hitValue = 1f;
            SetHit(_hitValue);
            _phase = Phase.Decaying;
            _phaseElapsed = 0f;
            yield return null;
            continue;
          }

          _phaseElapsed = Mathf.Min(_applyDuration, _phaseElapsed + Time.deltaTime);
          _hitValue = EvaluateNormalized(_applyCurve, _phaseElapsed / _applyDuration, 0f);
          SetHit(_hitValue);

          if (_phaseElapsed >= _applyDuration)
          {
            _hitValue = 1f;
            SetHit(_hitValue);
            _phase = Phase.Decaying;
            _phaseElapsed = 0f;
          }
        }
        else
        {
          if (_decayDuration <= 0f)
          {
            _hitValue = 0f;
            SetHit(_hitValue);
            break;
          }

          _phaseElapsed = Mathf.Min(_decayDuration, _phaseElapsed + Time.deltaTime);
          _hitValue = EvaluateNormalized(_decayCurve, _phaseElapsed / _decayDuration, 1f);
          SetHit(_hitValue);

          if (_phaseElapsed >= _decayDuration)
          {
            _hitValue = 0f;
            SetHit(_hitValue);
            break;
          }
        }

        yield return null;
      }

      _animationCoroutine = null;
    }

    private static float EvaluateNormalized(
      AnimationCurve curve,
      float progress,
      float missingCurveEndValue)
    {
      var value = curve == null ? Mathf.Lerp(missingCurveEndValue, 1f - missingCurveEndValue, progress) : curve.Evaluate(progress);
      return Mathf.Clamp01(value);
    }

    private static float FindCurveProgress(AnimationCurve curve, float value)
    {
      value = Mathf.Clamp01(value);
      if (curve == null)
        return value;

      var previousProgress = 0f;
      var previousValue = EvaluateNormalized(curve, 0f, 0f);
      var closestProgress = previousProgress;
      var closestDistance = Mathf.Abs(previousValue - value);

      for (var i = 1; i <= CurveInverseSamples; i++)
      {
        var progress = i / (float)CurveInverseSamples;
        var currentValue = EvaluateNormalized(curve, progress, 0f);
        var distance = Mathf.Abs(currentValue - value);
        if (distance < closestDistance)
        {
          closestProgress = progress;
          closestDistance = distance;
        }

        var minValue = Mathf.Min(previousValue, currentValue);
        var maxValue = Mathf.Max(previousValue, currentValue);
        if (value >= minValue
          && value <= maxValue
          && !Mathf.Approximately(previousValue, currentValue))
        {
          var segmentProgress = Mathf.InverseLerp(previousValue, currentValue, value);
          return Mathf.Lerp(previousProgress, progress, segmentProgress);
        }

        previousProgress = progress;
        previousValue = currentValue;
      }

      return closestProgress;
    }

    private void SetHit(float value)
    {
      if (_renderers == null || _propertyBlock == null)
        return;

      for (var i = 0; i < _renderers.Length; i++)
      {
        var renderer = _renderers[i];
        if (renderer == null)
          continue;

        renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(HitId, value);
        renderer.SetPropertyBlock(_propertyBlock);
      }
    }

    private void OnDisable()
    {
      if (_animationCoroutine != null)
      {
        StopCoroutine(_animationCoroutine);
        _animationCoroutine = null;
      }

      if (_listenToPlayerDamage && _characterService != null)
        _characterService.Damaged -= PlayHit;

      _hitValue = 0f;
      SetHit(0f);
    }

    private void OnValidate()
    {
      _applyDuration = Mathf.Max(0f, _applyDuration);
      _decayDuration = Mathf.Max(0f, _decayDuration);
      _applyCurve ??= DefaultApplyCurve();
      _decayCurve ??= DefaultDecayCurve();

      if (!_enabled && Application.isPlaying)
      {
        if (_animationCoroutine != null)
        {
          StopCoroutine(_animationCoroutine);
          _animationCoroutine = null;
        }

        _hitValue = 0f;
        SetHit(0f);
      }
    }

    private static AnimationCurve DefaultApplyCurve() =>
      AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private static AnimationCurve DefaultDecayCurve() =>
      AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
  }
}
