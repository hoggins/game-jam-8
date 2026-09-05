using System.Collections;
using UnityEngine;

namespace Destruction
{
  [DisallowMultipleComponent]
  public sealed class HitFx : MonoBehaviour
  {
    private const string SettingsPath = "HitFxSettings";

    private static readonly int HitId = Shader.PropertyToID("_Hit");
    private static HitFxSettings _settings;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _propertyBlock;
    private Coroutine _hitCoroutine;

    private void Awake()
    {
      _settings = _settings != null ? _settings : Resources.Load<HitFxSettings>(SettingsPath);
      _renderers = GetComponentsInChildren<Renderer>(true);
      _propertyBlock = new MaterialPropertyBlock();
    }

    public void PlayHit()
    {
      if (_settings == null || _renderers.Length == 0)
        return;

      if (_hitCoroutine != null)
        StopCoroutine(_hitCoroutine);

      _hitCoroutine = StartCoroutine(AnimateHit());
    }

    /// Rapidly flips the hit material between lit and unlit for <paramref name="duration"/> seconds,
    /// e.g. to warn that something is about to happen. Ignores <see cref="HitFxSettings.Curve"/> and
    /// <see cref="HitFxSettings.Duration"/>, using <see cref="HitFxSettings.BlinkInterval"/> instead.
    public void PlayBlink(float duration)
    {
      if (_settings == null || _renderers.Length == 0)
        return;

      if (_hitCoroutine != null)
        StopCoroutine(_hitCoroutine);

      _hitCoroutine = StartCoroutine(AnimateBlink(duration));
    }

    private IEnumerator AnimateHit()
    {
      var duration = _settings.Duration;
      if (duration <= 0f)
      {
        SetHit(0f);
        _hitCoroutine = null;
        yield break;
      }

      var elapsed = 0f;
      while (elapsed < duration)
      {
        elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(elapsed / duration);
        SetHit(Mathf.Clamp01(_settings.Curve.Evaluate(progress)));
        yield return null;
      }

      SetHit(0f);
      _hitCoroutine = null;
    }

    private IEnumerator AnimateBlink(float duration)
    {
      var interval = _settings.BlinkInterval;
      var elapsed = 0f;
      var lit = false;

      while (elapsed < duration)
      {
        lit = !lit;
        SetHit(lit ? 1f : 0f);
        yield return new WaitForSeconds(interval);
        elapsed += interval;
      }

      SetHit(0f);
      _hitCoroutine = null;
    }

    private void SetHit(float value)
    {
      for (var i = 0; i < _renderers.Length; i++)
      {
        var renderer = _renderers[i];
        if (renderer == null)
          continue;

        var materials = renderer.sharedMaterials;
        for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
        {
          renderer.GetPropertyBlock(_propertyBlock, materialIndex);
          _propertyBlock.SetFloat(HitId, value);
          renderer.SetPropertyBlock(_propertyBlock, materialIndex);
        }
      }
    }

    private void OnDisable()
    {
      if (_hitCoroutine != null)
      {
        StopCoroutine(_hitCoroutine);
        _hitCoroutine = null;
      }
    }
  }
}
