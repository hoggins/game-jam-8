using Destruction;
using UnityEngine;

namespace Timer
{
  /// <summary>
  /// One 3x5 digit of the in-world battle timer. Owns its 15 pixel boxes and raises/sinks them as the
  /// displayed value changes. Destruction itself is handled by the sibling
  /// <see cref="DestructibleObject"/>; <see cref="BattleTimerObject"/> owns that subscription and
  /// calls <see cref="MarkDestroyed"/>, so the ordering between the two is explicit rather than
  /// dependent on which component subscribed to the event first.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(DestructibleObject))]
  public sealed class TimerDigit : MonoBehaviour
  {
    public const int Columns = 3;
    public const int Rows = 5;
    public const int PixelCount = Columns * Rows;

    /// Row-major, top row first, '1' = lit. Index into the array is the digit itself.
    private static readonly string[] Glyphs =
    {
      "111101101101111", // 0
      "010110010010111", // 1
      "111001111100111", // 2
      "111001111001111", // 3
      "101101111001001", // 4
      "111100111001111", // 5
      "111100111101111", // 6
      "111001001001001", // 7
      "111101111101111", // 8
      "111101111001111", // 9
    };

    [Tooltip("Row-major, top-left first. Exactly 15 entries.")]
    [SerializeField] private Transform[] _pixels = new Transform[PixelCount];

    [Tooltip("How far below its lit position a pixel sinks when it is not part of the current digit.")]
    [SerializeField, Min(0f)] private float _sinkDepth = 6f;

    [Tooltip("Units per second a pixel travels between its sunk and lit positions.")]
    [SerializeField, Min(0.01f)] private float _moveSpeed = 12f;

    private float[] _litHeights;
    private int _value = -1;

    public bool IsDestroyed { get; private set; }

    private void Awake()
    {
      _litHeights = new float[_pixels.Length];
      for (var i = 0; i < _pixels.Length; i++)
      {
        if (_pixels[i] == null)
          continue;

        _litHeights[i] = _pixels[i].localPosition.y;
        // Nothing is shown until the first SetValue, so start every pixel underground.
        SetHeight(i, _litHeights[i] - _sinkDepth);
      }
    }

    public void SetValue(int value)
    {
      if (IsDestroyed)
        return;

      _value = Mathf.Clamp(value, 0, Glyphs.Length - 1);
    }

    private void Update()
    {
      if (IsDestroyed || _value < 0)
        return;

      var glyph = Glyphs[_value];
      var step = _moveSpeed * Time.deltaTime;

      for (var i = 0; i < _pixels.Length; i++)
      {
        if (_pixels[i] == null)
          continue;

        var target = glyph[i] == '1' ? _litHeights[i] : _litHeights[i] - _sinkDepth;
        var current = _pixels[i].localPosition.y;
        if (Mathf.Approximately(current, target))
          continue;

        SetHeight(i, Mathf.MoveTowards(current, target, step));
      }
    }

    private void SetHeight(int index, float y)
    {
      var position = _pixels[index].localPosition;
      position.y = y;
      _pixels[index].localPosition = position;
    }

    /// <summary>
    /// Called by <see cref="BattleTimerObject"/> once this digit has been smashed. From here on the
    /// pixels are loose rigidbodies owned by EnvironmentDecayManager, so this component stops writing
    /// their transforms.
    /// </summary>
    public void MarkDestroyed()
    {
      if (IsDestroyed)
        return;

      IsDestroyed = true;

      // Unlit pixels are parked below the ground plane, so breaking them just drops them out of the
      // world: they never come to rest, the decay manager never retires them, and the digit is never
      // despawned. Remove them instead and let only the visible pixels become debris. Destroying the
      // body is what EnvironmentDecayManager already treats as "this part is finished".
      var glyph = _value >= 0 ? Glyphs[_value] : null;
      for (var i = 0; i < _pixels.Length; i++)
      {
        if (_pixels[i] == null || (glyph != null && glyph[i] == '1'))
          continue;

        Destroy(_pixels[i].gameObject);
      }
    }

    private void OnValidate()
    {
      if (_pixels.Length != PixelCount)
        System.Array.Resize(ref _pixels, PixelCount);
    }
  }
}
