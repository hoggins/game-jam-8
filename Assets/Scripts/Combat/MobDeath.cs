using System;
using System.Collections;
using Movement;
using UnityEngine;

namespace Combat
{
  // Plays a duck's death: dissolve in place, or throw-then-dissolve when it dies
  // mid-attach. Rigidbody/Collider for the throw are added only for that instant,
  // since most ducks never attach and don't need to carry physics components.
  [DisallowMultipleComponent]
  public sealed class MobDeath : MonoBehaviour
  {
    [SerializeField, Min(0f)] private float _dissolveDuration = 1f;
    [SerializeField] private AnimationCurve _dissolveCurve = DefaultDissolveCurve();

    [Header("Throw (death while attached)")]
    [SerializeField, Min(0f)] private float _throwDistance = 10f;
    [SerializeField, Min(0f)] private float _throwLaunchAngle = 45f;
    [SerializeField, Min(0f)] private float _throwSpinTorque = 5f;
    [SerializeField, Min(0f)] private float _throwColliderRadius = 0.5f;
    [SerializeField, Min(0f)] private float _throwColliderHeight = 1.5f;
    [SerializeField, Min(0f)] private float _throwSettleDuration = 0.5f;

    private static readonly int InflateId = Shader.PropertyToID("_Inflate");

    private Renderer[] _renderers;
    private MaterialPropertyBlock _propertyBlock;
    private Coroutine _playCoroutine;
    private Rigidbody _throwRigidbody;
    private Collider _throwCollider;

    private void Awake()
    {
      _renderers = GetComponentsInChildren<Renderer>(true);
      _propertyBlock = new MaterialPropertyBlock();
    }

    public void ResetVisual() =>
      SetDissolve(0f);

    public void Play(Transform throwAwayFrom, MovementAgent movementAgent, Action onComplete) =>
      _playCoroutine = StartCoroutine(PlayRoutine(throwAwayFrom, movementAgent, onComplete));

    private IEnumerator PlayRoutine(Transform throwAwayFrom, MovementAgent movementAgent, Action onComplete)
    {
      if (throwAwayFrom != null)
      {
        ThrowAway(throwAwayFrom.position, movementAgent);
        yield return new WaitForSeconds(_throwSettleDuration);
      }

      var elapsed = 0f;
      while (elapsed < _dissolveDuration)
      {
        elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(elapsed / _dissolveDuration);
        SetDissolve(Mathf.Clamp01(Evaluate(_dissolveCurve, progress)));
        yield return null;
      }

      SetDissolve(1f);
      RemoveThrowPhysics();
      _playCoroutine = null;
      onComplete?.Invoke();
    }

    private void ThrowAway(Vector3 throwOrigin, MovementAgent movementAgent)
    {
      var direction = transform.position - throwOrigin;
      direction.y = 0f;
      if (direction.sqrMagnitude < 0.0001f)
        direction = UnityEngine.Random.insideUnitSphere;
      direction.Normalize();

      transform.SetParent(null, true);
      transform.position = throwOrigin;

      if (movementAgent != null)
        movementAgent.enabled = false;

      var capsule = gameObject.AddComponent<CapsuleCollider>();
      capsule.radius = _throwColliderRadius;
      capsule.height = _throwColliderHeight;
      capsule.center = new Vector3(0f, _throwColliderHeight * 0.5f, 0f);
      _throwCollider = capsule;

      var rigidbody = gameObject.AddComponent<Rigidbody>();
      var gravity = Mathf.Abs(Physics.gravity.y);
      var launchSpeed = gravity > 0f ? Mathf.Sqrt(_throwDistance * gravity) : 0f;
      var angleRad = _throwLaunchAngle * Mathf.Deg2Rad;
      var launchVelocity = direction * (launchSpeed * Mathf.Cos(angleRad))
        + Vector3.up * (launchSpeed * Mathf.Sin(angleRad));
      rigidbody.AddForce(launchVelocity, ForceMode.VelocityChange);
      rigidbody.AddTorque(UnityEngine.Random.insideUnitSphere * _throwSpinTorque, ForceMode.VelocityChange);
      _throwRigidbody = rigidbody;
    }

    private void RemoveThrowPhysics()
    {
      if (_throwRigidbody != null)
      {
        Destroy(_throwRigidbody);
        _throwRigidbody = null;
      }

      if (_throwCollider != null)
      {
        Destroy(_throwCollider);
        _throwCollider = null;
      }
    }

    private void SetDissolve(float value)
    {
      if (_renderers == null || _propertyBlock == null)
        return;

      for (var i = 0; i < _renderers.Length; i++)
      {
        var renderer = _renderers[i];
        if (renderer == null)
          continue;

        renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(InflateId, value);
        renderer.SetPropertyBlock(_propertyBlock);
      }
    }

    private void OnDisable()
    {
      if (_playCoroutine != null)
      {
        StopCoroutine(_playCoroutine);
        _playCoroutine = null;
      }

      RemoveThrowPhysics();
    }

    private static float Evaluate(AnimationCurve curve, float progress) =>
      curve == null ? progress : curve.Evaluate(progress);

    private static AnimationCurve DefaultDissolveCurve() =>
      AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private void OnValidate()
    {
      _dissolveDuration = Mathf.Max(0f, _dissolveDuration);
      _dissolveCurve ??= DefaultDissolveCurve();
      _throwDistance = Mathf.Max(0f, _throwDistance);
      _throwLaunchAngle = Mathf.Clamp(_throwLaunchAngle, 0f, 90f);
      _throwSpinTorque = Mathf.Max(0f, _throwSpinTorque);
      _throwColliderRadius = Mathf.Max(0f, _throwColliderRadius);
      _throwColliderHeight = Mathf.Max(0f, _throwColliderHeight);
      _throwSettleDuration = Mathf.Max(0f, _throwSettleDuration);
    }
  }
}
