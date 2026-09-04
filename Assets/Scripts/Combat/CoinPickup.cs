using System.Collections;
using App;
using Pooling;
using UnityEngine;
using VContainer;

namespace Combat
{
  /// Purely cosmetic: the coins are already credited when the duck dies. This just
  /// rises off the corpse and lobs itself at the player before returning to the pool.
  [DisallowMultipleComponent]
  public sealed class CoinPickup : MonoBehaviour
  {
    [Header("Rise")]
    [SerializeField, Min(0f)] private float _riseDuration = 0.6f;
    [SerializeField, Min(0f)] private float _riseHeight = 0.9f;
    [SerializeField] private AnimationCurve _riseCurve = DefaultRiseCurve();

    [Header("Flight")]
    [SerializeField, Min(0f)] private float _flyDuration = 0.45f;
    [SerializeField, Min(0f)] private float _flyArcHeight = 2.5f;
    [Tooltip("Height on the player the coin homes to, so it vanishes into the body, not the feet.")]
    [SerializeField] private float _targetHeight = 1f;
    [SerializeField] private float _spinDegreesPerSecond = 360f;

    [Inject] private Pool _pool;

    private Transform _player;
    private Coroutine _flightCoroutine;

    private void Awake() =>
      this.AsInjected();

    private void OnEnable()
    {
      _player = null;
      _flightCoroutine = StartCoroutine(Fly(transform.position));
    }

    private void OnDisable()
    {
      if (_flightCoroutine == null)
        return;

      StopCoroutine(_flightCoroutine);
      _flightCoroutine = null;
    }

    private IEnumerator Fly(Vector3 origin)
    {
      var apex = origin + Vector3.up * _riseHeight;

      yield return Rise(origin, apex);
      yield return ArcToPlayer(apex);

      _flightCoroutine = null;
      _pool?.Release(gameObject);
    }

    private IEnumerator Rise(Vector3 from, Vector3 to)
    {
      if (_riseDuration <= 0f)
      {
        transform.position = to;
        yield break;
      }

      var elapsed = 0f;
      while (elapsed < _riseDuration)
      {
        elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(elapsed / _riseDuration);
        transform.position = Vector3.LerpUnclamped(from, to, Evaluate(_riseCurve, progress));
        Spin();
        yield return null;
      }

      transform.position = to;
    }

    private IEnumerator ArcToPlayer(Vector3 from)
    {
      if (_flyDuration <= 0f)
        yield break;

      var elapsed = 0f;
      var target = ResolveTarget(from);
      while (elapsed < _flyDuration)
      {
        elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(elapsed / _flyDuration);

        // Re-read the target every frame so the coin homes in on a moving player.
        target = ResolveTarget(target);
        var ground = Vector3.Lerp(from, target, progress);
        // 4t(1-t) peaks at 1 halfway and is 0 at both ends: a lobbed ball.
        ground.y += _flyArcHeight * 4f * progress * (1f - progress);
        transform.position = ground;
        Spin();
        yield return null;
      }
    }

    private Vector3 ResolveTarget(Vector3 fallback)
    {
      if (_player == null)
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;

      return _player == null
        ? fallback
        : _player.position + Vector3.up * _targetHeight;
    }

    private void Spin() =>
      transform.Rotate(Vector3.up, _spinDegreesPerSecond * Time.deltaTime, Space.World);

    private static float Evaluate(AnimationCurve curve, float progress) =>
      curve == null ? progress : curve.Evaluate(progress);

    /// Leaves the corpse fast and eases into the apex, so the rise blends into the lob.
    private static AnimationCurve DefaultRiseCurve() =>
      new(new Keyframe(0f, 0f, 2.5f, 2.5f), new Keyframe(1f, 1f, 0f, 0f));

    private void OnValidate()
    {
      _riseDuration = Mathf.Max(0f, _riseDuration);
      _riseHeight = Mathf.Max(0f, _riseHeight);
      _riseCurve ??= DefaultRiseCurve();
      _flyDuration = Mathf.Max(0f, _flyDuration);
      _flyArcHeight = Mathf.Max(0f, _flyArcHeight);
    }
  }
}
