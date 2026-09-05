using System.Collections;
using App;
using Pooling;
using UnityEngine;
using VContainer;

namespace Combat
{
  /// Purely cosmetic: the coins are credited when their source is destroyed. This flies from the
  /// source to its drop target, rises, and then lobs itself at the player before returning to pool.
  [DisallowMultipleComponent]
  public sealed class CoinPickup : MonoBehaviour
  {
    [Header("Outward Drop")]
    [SerializeField, Min(0f)] private float _outwardDuration = 0.5f;
    [SerializeField, Min(0f)] private float _outwardArcHeight = 0.75f;
    [SerializeField] private AnimationCurve _outwardArcCurve = DefaultOutwardArcCurve();

    [Header("Rise")]
    [SerializeField, Min(0f)] private float _riseDuration = 0.6f;
    [SerializeField, Min(0f)] private float _riseHeight = 0.9f;
    [SerializeField] private AnimationCurve _riseCurve = DefaultRiseCurve();
    [Tooltip("How far the apex drifts sideways, so coins from one kill do not stack.")]
    [SerializeField, Min(0f)] private float _spreadRadius = 0.35f;

    [Header("Flight")]
    [SerializeField, Min(0f)] private float _flyDuration = 0.45f;
    [SerializeField, Min(0f)] private float _flyArcHeight = 2.5f;
    [Tooltip("Height on the player the coin homes to, so it vanishes into the body, not the feet.")]
    [SerializeField] private float _targetHeight = 1f;
    [SerializeField] private float _spinDegreesPerSecond = 360f;

    [Inject] private Pool _pool;

    /// Fans consecutive spawns by the golden angle: coins dropped by the same kill are
    /// spawned back to back, so successive indices never land on top of each other.
    private static int _spawnIndex;

    private Transform _player;
    private Coroutine _flightCoroutine;
    private Vector3? _outwardTarget;
    private Vector3 _launchOrigin;

    private void Awake() =>
      this.AsInjected();

    private void OnEnable()
    {
      _player = null;
      _outwardTarget = null;
      _launchOrigin = transform.position;
      StartFlight();
    }

    private void OnDisable()
    {
      if (_flightCoroutine == null)
        return;

      StopCoroutine(_flightCoroutine);
      _flightCoroutine = null;
    }

    internal void SetOutwardTarget(Vector3 target)
    {
      _outwardTarget = target;
      transform.position = _launchOrigin;
      if (_flightCoroutine != null)
        StopCoroutine(_flightCoroutine);

      StartFlight();
    }

    private void StartFlight() =>
      _flightCoroutine = StartCoroutine(Fly(transform.position));

    private IEnumerator Fly(Vector3 origin)
    {
      if (_outwardTarget.HasValue)
      {
        var target = _outwardTarget.Value;
        yield return FlyOut(origin, target);
        origin = target;
      }

      var apex = origin + Vector3.up * _riseHeight + GetSpreadOffset();

      yield return Rise(origin, apex);
      yield return ArcToPlayer(apex);

      _flightCoroutine = null;
      _pool?.Release(gameObject);
    }

    private IEnumerator FlyOut(Vector3 from, Vector3 to)
    {
      if (_outwardDuration <= 0f)
      {
        transform.position = to;
        yield break;
      }

      var elapsed = 0f;
      while (elapsed < _outwardDuration)
      {
        elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(elapsed / _outwardDuration);
        var ground = Vector3.Lerp(from, to, progress);
        ground.y += _outwardArcHeight * Evaluate(_outwardArcCurve, progress);
        transform.position = ground;
        Spin();
        yield return null;
      }

      transform.position = to;
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

    private Vector3 GetSpreadOffset()
    {
      if (_spreadRadius <= 0f)
        return Vector3.zero;

      const float goldenAngleDegrees = 137.5f;
      var angle = _spawnIndex++ * goldenAngleDegrees * Mathf.Deg2Rad;
      return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _spreadRadius;
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

    private static AnimationCurve DefaultOutwardArcCurve() =>
      new(
        new Keyframe(0f, 0f, 2f, 2f),
        new Keyframe(0.5f, 1f, 0f, 0f),
        new Keyframe(1f, 0f, -2f, -2f));

    private void OnValidate()
    {
      _outwardDuration = Mathf.Max(0f, _outwardDuration);
      _outwardArcHeight = Mathf.Max(0f, _outwardArcHeight);
      _outwardArcCurve ??= DefaultOutwardArcCurve();
      _riseDuration = Mathf.Max(0f, _riseDuration);
      _riseHeight = Mathf.Max(0f, _riseHeight);
      _riseCurve ??= DefaultRiseCurve();
      _spreadRadius = Mathf.Max(0f, _spreadRadius);
      _flyDuration = Mathf.Max(0f, _flyDuration);
      _flyArcHeight = Mathf.Max(0f, _flyArcHeight);
    }
  }
}
