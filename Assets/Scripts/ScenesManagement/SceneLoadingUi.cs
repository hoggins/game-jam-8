using UnityEngine;
using UnityEngine.UI;

namespace ScenesManagement
{
  public class SceneLoadingUi : MonoBehaviour
  {
    [SerializeField] private GameObject _root;
    [SerializeField] private Slider _progressBar;

    [Tooltip("Max bar fill per second. Caps how fast the bar reacts to a jump in reported progress.")]
    [SerializeField] private float _fillSpeed = 1.5f;

    [Tooltip("How long the bar takes to travel to the hand-off point when loading is not the bottleneck.")]
    [SerializeField] private float _fillSeconds = 1.2f;

    [Tooltip("Where the bar waits for scene activation. The rest is the activation stall.")]
    [Range(0.5f, 1f)]
    [SerializeField] private float _activationSplit = 0.9f;

    private float _displayed;
    private float _reported;
    private float _elapsed;
    private bool _isRunning;
    private bool _isComplete;

    /// <summary>
    /// True once the bar has filled up to the hand-off point. Activating the scene before this
    /// would freeze the bar mid-travel, since the activation stall renders no frames at all.
    /// </summary>
    public bool IsReadyForActivation => !_isRunning || _displayed >= _activationSplit;

    /// <summary>True once the bar has visually reached the end, after <see cref="CompleteLoading"/>.</summary>
    public bool IsDisplayFinished => !_isRunning || (_isComplete && _displayed >= 1f);

    public void BeginLoading()
    {
      _displayed = 0f;
      _reported = 0f;
      _elapsed = 0f;
      _isComplete = false;
      _isRunning = true;
      Apply();
      EnableLoading(true);
    }

    public void EnableLoading(bool isEnabled)
    {
      if (!isEnabled)
        _isRunning = false;

      var root = _root != null ? _root : gameObject;
      root.SetActive(isEnabled);
    }

    /// <summary>Real load progress in 0..1. Acts as a ceiling - the bar never runs ahead of it.</summary>
    public void SetProgress(float progress)
    {
      _reported = Mathf.Max(_reported, Mathf.Clamp01(progress));
    }

    /// <summary>The scene is in. Lets the bar fill the rest of the way.</summary>
    public void CompleteLoading()
    {
      _isComplete = true;
    }

    private void Update()
    {
      if (!_isRunning)
        return;

      // Unscaled: a paused battle must not freeze the overlay.
      float deltaTime = Time.unscaledDeltaTime;
      _elapsed += deltaTime;

      // AsyncOperation.progress plateaus at 0.9 within a frame or two, so real progress alone reads
      // as "instantly full, then a long wait". The time ramp gives the bar something to accumulate
      // on; real progress stays the ceiling, so a genuinely slow load still holds the bar back.
      float ramp = Mathf.SmoothStep(0f, 1f, _elapsed / Mathf.Max(_fillSeconds, 0.01f));
      float target = _isComplete ? 1f : _activationSplit * Mathf.Min(ramp, _reported);

      _displayed = Mathf.MoveTowards(_displayed, target, Mathf.Max(_fillSpeed, 0.01f) * deltaTime);
      Apply();
    }

    private void Apply()
    {
      if (_progressBar != null)
        _progressBar.value = _displayed;
    }
  }
}
