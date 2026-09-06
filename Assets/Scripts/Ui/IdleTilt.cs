using UnityEngine;

namespace App
{
  /// Rocks a transform back and forth forever, left edge to right edge and back. Sits on the
  /// element itself so each one can be tuned separately in the inspector.
  [RequireComponent(typeof(RectTransform))]
  public class IdleTilt : MonoBehaviour
  {
    [Tooltip("Degrees to lean each way. The sweep runs from minus this to plus this.")]
    [SerializeField] private float _tiltAngle = 2f;

    [Tooltip("Seconds one sweep takes, so a full there-and-back cycle is twice this.")]
    [SerializeField, Min(0f)] private float _duration = 0.8f;

    [Tooltip("Eased in-out by default, which lingers at the edges. X is normalised sweep time, Y is progress from one edge to the other.")]
    [SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform _rectTransform;
    private Vector3 _restEuler;
    private float _elapsed;

    private void Awake()
    {
      _rectTransform = (RectTransform)transform;
      _restEuler = _rectTransform.localEulerAngles;
    }

    private void OnEnable()
    {
      _elapsed = 0f;
      ApplyTilt();
    }

    private void OnDisable() =>
      SetTilt(0f);

    /// Unscaled, like the rest of the HUD's own motion - the popups that freeze the world fade
    /// this out anyway, and it should not resume mid-lean when they let go.
    private void Update()
    {
      _elapsed += Time.unscaledDeltaTime;
      ApplyTilt();
    }

    private void ApplyTilt()
    {
      if (_duration <= 0f)
      {
        SetTilt(0f);
        return;
      }

      var sweep = Mathf.PingPong(_elapsed / _duration, 1f);
      SetTilt(Mathf.LerpUnclamped(-_tiltAngle, _tiltAngle, _curve.Evaluate(sweep)));
    }

    /// Z is an offset from the authored rotation, so an element that ships tilted keeps its lean.
    private void SetTilt(float z) =>
      _rectTransform.localEulerAngles = new Vector3(_restEuler.x, _restEuler.y, _restEuler.z + z);
  }
}
