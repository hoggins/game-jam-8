using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace App
{
  /// Tilts a button slightly while the pointer is over it, and straightens it back out on exit.
  /// Sits on the button itself so every button can be tuned separately in the inspector.
  [RequireComponent(typeof(RectTransform))]
  public class ButtonHoverTilt : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
  {
    [Tooltip("Degrees to lean while hovered. Positive leans to the right.")]
    [SerializeField] private float _tiltAngle = 2f;

    [Tooltip("Seconds the lean takes, both ways.")]
    [SerializeField] private float _duration = 0.15f;

    [Tooltip("Eased in-out by default. X is normalised time, Y is progress towards the tilt.")]
    [SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform _rectTransform;
    private Vector3 _restEuler;
    private Coroutine _running;

    private void Awake()
    {
      _rectTransform = (RectTransform)transform;
      _restEuler = _rectTransform.localEulerAngles;
    }

    private void OnDisable()
    {
      // The pointer never gets an exit event when the screen is hidden mid-hover.
      _running = null;
      _rectTransform.localEulerAngles = _restEuler;
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) =>
      TiltTo(-_tiltAngle);

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) =>
      TiltTo(0f);

    private void TiltTo(float targetZ)
    {
      if (_running != null)
        StopCoroutine(_running);

      if (!isActiveAndEnabled)
      {
        SetTilt(targetZ);
        return;
      }

      _running = StartCoroutine(Tilt(targetZ));
    }

    private IEnumerator Tilt(float targetZ)
    {
      var startZ = Mathf.DeltaAngle(_restEuler.z, _rectTransform.localEulerAngles.z);

      if (_duration > 0f)
      {
        for (var elapsed = 0f; elapsed < _duration; elapsed += Time.unscaledDeltaTime)
        {
          SetTilt(Mathf.LerpUnclamped(startZ, targetZ, _curve.Evaluate(elapsed / _duration)));
          yield return null;
        }
      }

      SetTilt(targetZ);
      _running = null;
    }

    /// Z is an offset from the authored rotation, so a button that ships tilted stays that way.
    private void SetTilt(float z) =>
      _rectTransform.localEulerAngles = new Vector3(_restEuler.x, _restEuler.y, _restEuler.z + z);
  }
}
