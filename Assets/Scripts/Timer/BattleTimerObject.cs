using App;
using Destruction;
using Model;
using UnityEngine;
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
    private const int PlaceCount = 4;

    /// Seconds contributed by one unit in each place, most significant first.
    private static readonly int[] PlaceValues = { 600, 60, 10, 1 };

    /// Largest digit each place can hold; the seconds-tens place only reaches 5.
    private static readonly int[] PlaceDigitCaps = { 9, 9, 5, 9 };

    [Tooltip("Left to right as they stand in the world: minutes tens, minutes units, seconds tens, seconds units.")]
    [SerializeField] private TimerDigit[] _digits = new TimerDigit[PlaceCount];

    [Inject] private BattleService _battleService;

    private DestructibleObject[] _destructibles;

    private readonly int[] _places = new int[PlaceCount];
    private readonly int[] _survivors = new int[PlaceCount];
    private readonly int[] _digitValues = new int[PlaceCount];

    private void Awake()
    {
      this.AsInjected();

      _destructibles = new DestructibleObject[_digits.Length];
      for (var i = 0; i < _digits.Length; i++)
        if (_digits[i] != null)
          _destructibles[i] = _digits[i].GetComponent<DestructibleObject>();
    }

    private void OnEnable()
    {
      foreach (var destructible in _destructibles)
        if (destructible != null)
          destructible.Destroyed += OnDigitDestroyed;

      PushTime();
    }

    private void OnDisable()
    {
      foreach (var destructible in _destructibles)
        if (destructible != null)
          destructible.Destroyed -= OnDigitDestroyed;
    }

    private void Update() => PushTime();

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
      if (_battleService == null)
        return;

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

      if (newCount == 0)
        _battleService.DestroyTimer();
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
    }
  }
}
