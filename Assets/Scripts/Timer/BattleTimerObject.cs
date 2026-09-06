using App;
using Destruction;
using Model;
using SceneHud;
using UnityEngine;
using Unity.Profiling;
using VContainer;

namespace Timer
{
  /// <summary>
  /// The in-world mm:ss timer building. Drives its four <see cref="TimerDigit"/>s from
  /// <see cref="BattleService.Timer"/>, and pushes the value back the other way when a digit is
  /// smashed.
  ///
  /// Smashing a digit removes a place from the clock entirely: the surviving digits keep their own
  /// values and slide right into the least significant places, so the digits that are left always
  /// spell the remaining time reading left to right, with the rightmost survivor as seconds-units.
  /// Breaking the leading digit of 21:45 leaves 1:45; breaking both seconds digits of 01:45 turns
  /// that leftover "01" from minutes into 1 second. Time therefore only ever goes down. Once every
  /// digit is gone the countdown is stopped instead of running out.
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class BattleTimerObject : MonoBehaviour
  {
    private static readonly ProfilerMarker DigitDestroyedMarker =
      new("BattleTimerObject.OnDigitDestroyed");

    private const int PlaceCount = 4;

    /// How long the smashed clock stays on the HUD after its last digit falls, before the widget
    /// cuts to the replacement timer. Long enough to watch it come apart.
    private const float HudLingerSeconds = 2f;

    /// Seconds contributed by one unit in each place, most significant first.
    private static readonly int[] PlaceValues = { 600, 60, 10, 1 };

    /// Largest digit each place can hold; the seconds-tens place only reaches 5.
    private static readonly int[] PlaceDigitCaps = { 9, 9, 5, 9 };

    [Tooltip("Left to right as they stand in the world: minutes tens, minutes units, seconds tens, seconds units.")]
    [SerializeField] private TimerDigit[] _digits = new TimerDigit[PlaceCount];

    [Inject] private BattleService _battleService;

    private DestructibleObject[] _destructibles;
    private HitFx[] _hitFx;
    private GameObject _hudCamera;
    private SceneHudElement _hudElement;
    private float _hudLingerUntil;
    private bool _isDead;

    /// True once every digit has been smashed. The husk lingers for a few seconds while the debris
    /// decays, so anything looking for "the timer" in the world has to skip a dead one.
    public bool IsDead => _isDead;

    private readonly int[] _places = new int[PlaceCount];
    private readonly int[] _survivors = new int[PlaceCount];
    private readonly int[] _digitValues = new int[PlaceCount];
    private float _secondsRemainingOnArrival;
    private bool _hasSecondsRemainingOnArrival;

    private void Awake()
    {
      this.AsInjected();
      WarnIfRootIsDestructible();

      _destructibles = new DestructibleObject[_digits.Length];
      _hitFx = new HitFx[_digits.Length];
      for (var i = 0; i < _digits.Length; i++)
        if (_digits[i] != null)
        {
          _destructibles[i] = _digits[i].GetComponent<DestructibleObject>();
          _hitFx[i] = _digits[i].GetComponent<HitFx>();
        }

      var hudCamera = GetComponentInChildren<Camera>(true);
      _hudCamera = hudCamera != null ? hudCamera.gameObject : null;
      _hudElement = GetComponentInChildren<SceneHudElement>(true);
    }

    private void OnEnable()
    {
      _secondsRemainingOnArrival = 0f;
      _hasSecondsRemainingOnArrival = false;
      foreach (var destructible in _destructibles)
        if (destructible != null)
          destructible.Destroyed += OnDigitDestroyed;

      if (_battleService != null)
        _battleService.TimerExpired += OnTimerExpired;

      PushTime();
    }

    private void OnDisable()
    {
      foreach (var destructible in _destructibles)
        if (destructible != null)
          destructible.Destroyed -= OnDigitDestroyed;

      if (_battleService != null)
        _battleService.TimerExpired -= OnTimerExpired;
    }

    /// The clock hit 00:00; blink every surviving digit's hit material for the duration of the
    /// timeout defeat delay so the player can see the countdown is really over before the loss lands.
    private void OnTimerExpired(float duration)
    {
      foreach (var hitFx in _hitFx)
        if (hitFx != null)
          hitFx.PlayBlink(duration);
    }

    private void Update()
    {
      if (_isDead)
      {
        // Only the digits and the divider are destructible; nothing owns this root, so once the
        // decay manager has retired the last of them the husk has to clear itself up. Left alone it
        // would linger with its HUD camera and render texture, one more for every respawn. The husk
        // outlives the HUD hold either way, so the widget is never left rendering a dead texture.
        if (!HasDestructibleChildren() && Time.unscaledTime >= _hudLingerUntil)
          Destroy(gameObject);

        return;
      }

      PushTime();
    }

    private bool HasDestructibleChildren()
    {
      for (var i = 0; i < transform.childCount; i++)
        if (transform.GetChild(i).GetComponent<DestructibleObject>() != null)
          return true;

      return false;
    }

    private void PushTime()
    {
      if (_battleService == null)
        return;

      var survivorCount = CollectSurvivors();
      if (survivorCount == 0)
        return;

      SplitTime(_battleService.Timer, survivorCount);

      var firstPlace = PlaceCount - survivorCount;
      for (var j = 0; j < survivorCount; j++)
        _digits[_survivors[j]].SetValue(_places[firstPlace + j]);
    }

    private void OnDigitDestroyed(DestructibleObject destroyed)
    {
      DigitDestroyedMarker.Begin();
      try
      {
        if (_battleService == null)
          return;

        if (!_hasSecondsRemainingOnArrival)
        {
          _secondsRemainingOnArrival = _battleService.Timer;
          _hasSecondsRemainingOnArrival = true;
        }
      // Snapshot what each surviving digit currently reads, derived from the authoritative time
      // rather than from the digits themselves: they only catch up in Update, and this fires
      // mid-frame.
      var oldCount = CollectSurvivors();
      SplitTime(_battleService.Timer, oldCount);

      var oldFirstPlace = PlaceCount - oldCount;
      for (var j = 0; j < oldCount; j++)
        _digitValues[_survivors[j]] = _places[oldFirstPlace + j];

      for (var i = 0; i < _digits.Length; i++)
        if (_digits[i] != null && _destructibles[i] == destroyed)
          _digits[i].MarkDestroyed();

      // Re-seat the survivors in the places they now occupy: one fewer place means every one of
      // them shifts a step towards seconds.
      var newCount = CollectSurvivors();
      var newFirstPlace = PlaceCount - newCount;
      var remainingSeconds = 0;
      for (var j = 0; j < newCount; j++)
        remainingSeconds += _digitValues[_survivors[j]] * PlaceValues[newFirstPlace + j];

      _battleService.SetTimer(remainingSeconds);

      if (newCount != 0)
        return;

      // Not a hard cut: the HUD stays on this clock while it comes apart, so the camera keeps
      // rendering and the replacement's texture waits its turn.
      _hudLingerUntil = Time.unscaledTime + HudLingerSeconds;
      if (_hudElement != null)
        _hudElement.Linger(HudLingerSeconds);
      else if (_hudCamera != null)
        _hudCamera.SetActive(false);

      // DestroyTimer is what triggers the replacement timer, so it registers its HUD element before
      // this one stands down; the hold above is why that registration waits instead of taking the
      // widget over on the spot, and SceneHudService ignores the stale unregister that follows.
      _battleService.DestroyTimer(_secondsRemainingOnArrival);
      _isDead = true;

      // The divider carries no time, so it can still be standing when the last digit dies. A lone
      // colon in a field is not a clock: collapse whatever is left so the whole thing decays away
      // together and this husk can retire.
      BreakRemainingParts();
      }
      finally
      {
        DigitDestroyedMarker.End();
      }
    }

    /// <summary>
    /// Smashes every surviving digit, exactly as if the player finished the clock digit by digit.
    /// Used by the F4 debug cheat so the timer dies through the same path a real hit would —
    /// <see cref="OnDigitDestroyed"/> still recomputes the remaining time after each break and
    /// fires <see cref="BattleService.DestroyTimer"/> once the last one falls, which is what drives
    /// the standard respawn in <see cref="TimerRespawnService"/>.
    /// </summary>
    public void CheatDestroyAll()
    {
      for (var i = 0; i < _digits.Length; i++)
        if (_digits[i] != null && !_digits[i].IsDestroyed && _destructibles[i] != null)
          _destructibles[i].Break(_destructibles[i].transform.position);
    }

    private void BreakRemainingParts()
    {
      for (var i = 0; i < transform.childCount; i++)
      {
        var destructible = transform.GetChild(i).GetComponent<DestructibleObject>();
        if (destructible != null)
          destructible.Break(destructible.transform.position);
      }
    }

    /// Fills <see cref="_survivors"/> with the indices of the digits still standing, left to right.
    private int CollectSurvivors()
    {
      var count = 0;
      for (var i = 0; i < _digits.Length; i++)
        if (_digits[i] != null && !_digits[i].IsDestroyed)
          _survivors[count++] = i;

      return count;
    }

    /// <summary>
    /// Splits the remaining time across the <paramref name="survivorCount"/> least significant
    /// places, writing each place's digit into <see cref="_places"/>. Time is clamped to what those
    /// places can actually spell, so a clock reduced to two digits shows at most 59 seconds.
    /// </summary>
    private void SplitTime(float seconds, int survivorCount)
    {
      var firstPlace = PlaceCount - survivorCount;
      var remaining = Mathf.Clamp(Mathf.CeilToInt(seconds), 0, MaxSeconds(firstPlace));

      for (var place = firstPlace; place < PlaceCount; place++)
      {
        var digit = Mathf.Min(remaining / PlaceValues[place], PlaceDigitCaps[place]);
        _places[place] = digit;
        remaining -= digit * PlaceValues[place];
      }
    }

    /// The largest time the places from <paramref name="firstPlace"/> onwards can represent.
    private static int MaxSeconds(int firstPlace)
    {
      var max = 0;
      for (var place = firstPlace; place < PlaceCount; place++)
        max += PlaceDigitCaps[place] * PlaceValues[place];

      return max;
    }

    private void OnValidate()
    {
      if (_digits.Length != PlaceCount)
        System.Array.Resize(ref _digits, PlaceCount);

      WarnIfRootIsDestructible();
    }

    /// <summary>
    /// The clock is only ever destroyed a digit at a time, and each digit owns its own
    /// <see cref="DestructibleObject"/>. One on this root instead shatters all 62 pixels in a single
    /// break and silently bypasses everything above it: the digits are never told they died, the
    /// remaining time is never recomputed, and <see cref="BattleService.DestroyTimer"/> never fires,
    /// so the timer never respawns either. Running Tools > Destruction > Destructible Object Setup
    /// on this prefab adds exactly that, so say so out loud instead of just misbehaving.
    /// </summary>
    private void WarnIfRootIsDestructible()
    {
      if (GetComponent<DestructibleObject>() == null && GetComponent<DestructibleHealth>() == null)
        return;

      Debug.LogError(
        $"{name}: the timer root carries a DestructibleObject/DestructibleHealth, which breaks the "
        + "whole clock in one hit instead of digit by digit. Remove them from the prefab root, or "
        + "rebuild it with Tools > Destruction > Rebuild Battle Timer Prefab.",
        this);
    }
  }
}
