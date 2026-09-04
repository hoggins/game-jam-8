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

    private void SetHit(float value)
    {
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
      if (_hitCoroutine != null)
      {
        StopCoroutine(_hitCoroutine);
        _hitCoroutine = null;
      }
    }
  }
}
