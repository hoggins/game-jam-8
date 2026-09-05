using System.Collections;
using App;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SceneHud
{
  /// <summary>
  /// The HUD half of a scene HUD element: shows whatever the matching <see cref="SceneHudElement"/>
  /// is currently rendering, and hides itself while there is no such element in the scene, so a
  /// destroyed or not-yet-spawned object leaves no stale frame behind.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(RawImage))]
  public sealed class SceneHudElementView : MonoBehaviour
  {
    [Tooltip("Which in-world element this widget shows.")]
    [SerializeField] private SceneHudElementId _id;

    [Tooltip("Optional group shown while the in-world element is destroyed.")]
    [SerializeField] private CanvasGroup _enableGroupWhenDestroyed;

    [Tooltip("Seconds the destroyed group takes to fade and scale in or out.")]
    [SerializeField] private float _destroyedFadeDuration = 0.35f;

    [Tooltip("Scale the destroyed group starts from when appearing and ends at when hiding.")]
    [SerializeField] private float _destroyedFadeScale = 1.2f;

    [Tooltip("Eases the fade over its duration. Sampled from 0..1; a flat or empty curve falls back to linear.")]
    [SerializeField] private AnimationCurve _destroyedFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Inject] private SceneHudService _sceneHud;

    private RawImage _image;
    private Transform _destroyedTransform;
    private Coroutine _destroyedFade;
    private bool _destroyedShown;
    private bool _seenTexture;

    private void Awake()
    {
      this.AsInjected();
      _image = GetComponent<RawImage>();
      if (_enableGroupWhenDestroyed != null)
        _destroyedTransform = _enableGroupWhenDestroyed.transform;
    }

    private void OnEnable()
    {
      _sceneHud.Changed += OnChanged;
      // The HUD comes up before the in-world elements register, so an empty entry at this point
      // means "not spawned yet", not "destroyed". Wait for the first texture before the destroyed
      // group may appear, otherwise every battle opens on a flash of it.
      _seenTexture = false;
      Apply(_sceneHud.Get(_id), false);
    }

    private void OnDisable()
    {
      _sceneHud.Changed -= OnChanged;
      _destroyedFade = null;
    }

    private void OnChanged(SceneHudElementId id, RenderTexture texture)
    {
      if (id == _id)
        Apply(texture, true);
    }

    private void Apply(RenderTexture texture, bool animate)
    {
      _image.texture = texture;
      _image.enabled = texture != null;
      if (texture != null)
        _seenTexture = true;

      SetDestroyedShown(_seenTexture && texture == null, animate);
    }

    private void SetDestroyedShown(bool shown, bool animate)
    {
      if (_enableGroupWhenDestroyed == null)
        return;

      if (_destroyedFade != null)
      {
        StopCoroutine(_destroyedFade);
        _destroyedFade = null;
      }
      else if (_destroyedShown == shown && animate)
      {
        return;
      }

      _destroyedShown = shown;

      if (!animate || _destroyedFadeDuration <= 0f || !isActiveAndEnabled)
      {
        _enableGroupWhenDestroyed.gameObject.SetActive(shown);
        _enableGroupWhenDestroyed.alpha = shown ? 1f : 0f;
        _destroyedTransform.localScale = Vector3.one * (shown ? 1f : _destroyedFadeScale);
        return;
      }

      _enableGroupWhenDestroyed.gameObject.SetActive(true);
      _destroyedFade = StartCoroutine(FadeDestroyed(shown));
    }

    private IEnumerator FadeDestroyed(bool shown)
    {
      var fromAlpha = _enableGroupWhenDestroyed.alpha;
      var toAlpha = shown ? 1f : 0f;
      var fromScale = _destroyedTransform.localScale.x;
      var toScale = shown ? 1f : _destroyedFadeScale;
      if (shown && Mathf.Approximately(fromAlpha, 0f))
        fromScale = _destroyedFadeScale;

      for (var elapsed = 0f; elapsed < _destroyedFadeDuration; elapsed += Time.unscaledDeltaTime)
      {
        var t = Ease(Mathf.Clamp01(elapsed / _destroyedFadeDuration));
        _enableGroupWhenDestroyed.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
        _destroyedTransform.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, t);
        yield return null;
      }

      _enableGroupWhenDestroyed.alpha = toAlpha;
      _destroyedTransform.localScale = Vector3.one * toScale;
      _enableGroupWhenDestroyed.gameObject.SetActive(shown);
      _destroyedFade = null;
    }

    /// A curve left empty by an older serialized instance would evaluate to a constant 0 and freeze
    /// the fade on its first frame, so anything without a real span stays linear.
    private float Ease(float t) =>
      _destroyedFadeCurve == null || _destroyedFadeCurve.length < 2 ? t : _destroyedFadeCurve.Evaluate(t);
  }
}
