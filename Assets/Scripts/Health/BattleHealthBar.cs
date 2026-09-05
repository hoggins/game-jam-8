using App;
using Balance;
using Combat;
using Destruction;
using Model;
using UnityEngine;
using VContainer;

namespace Health
{
  /// <summary>
  /// The player's health, standing in the street as a 2x12 bar of 24 smashable pixels.
  ///
  /// The bar is not a readout of health, it *is* the health: the pixels are the player's hit points
  /// made physical, and knocking one off costs him a twenty-fourth of his maximum. Smash all of them
  /// and he dies, which is the whole point of the object — it is a hazard the player can walk up to
  /// and hurt himself on, not a pickup.
  ///
  /// Damage flows in one direction only, from pixels to health, because the reverse cannot work: the
  /// player's maximum health is orders of magnitude larger than his attack power, so dealing himself
  /// weapon damage would never remove a single pixel. Ducks biting the player are handled the other
  /// way round — <see cref="CharacterService.HealthChanged"/> re-syncs the bar — so however the
  /// health was lost, the bar always shows exactly what is left.
  ///
  /// Note that this deliberately does not use <see cref="DestructibleHealth"/>: that would make the
  /// bar an ordinary breakable prop with its own hit points and a random part-fallout order, where
  /// this one has to empty from one end and convert each lost pixel into player damage.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(DestructibleObject))]
  public sealed class BattleHealthBar : MonoBehaviour, IImpactDamageable
  {
    public const int Rows = 2;
    public const int Columns = 12;
    public const int PixelCount = Rows * Columns;

    [Tooltip("Column-major, left to right: index = column * Rows + row. The bar empties from the last index back, so the pixels have to be ordered as the bar reads.")]
    [SerializeField] private DecayPart[] _pixels = new DecayPart[PixelCount];

    [Inject] private CharacterService _characterService;
    [Inject] private BattleBalanceConfig _battleBalance;
    [Inject] private BattleService _battleService;

    private DestructibleObject _destructible;
    private HitFx _hitFx;

    /// Weapon damage this bar absorbs per pixel, from the balance row for
    /// <see cref="DestructibleObjectType.HealthBar"/> spread across the 24 of them.
    private float _damagePerPixel;

    /// <summary>
    /// Health one pixel is worth, and what the player pays when one is knocked off. Read fresh
    /// rather than cached: a Max Health upgrade bought mid-battle changes what a pixel stands for.
    /// Rounded up, so the last pixel always finishes the player off rather than leaving him on a
    /// sliver of health with no bar left to lose.
    /// </summary>
    private int HealthPerPixel =>
      Mathf.Max(1, Mathf.CeilToInt(_characterService.MaxHealth / (float)PixelCount));

    private float _damageTaken;
    private int _pixelsPaidFor;
    private int _litCount = PixelCount;
    private bool _isDestroyed;

    /// <summary>
    /// Whether the player's health has been dealt out yet, latched on and never cleared. This is
    /// deliberately not "is the battle active": the battle ends the instant the player dies, and the
    /// health change that killed him is still being delivered, so a live check would leave the bar
    /// standing over a corpse. All it has to rule out is the window before the first battle starts.
    /// </summary>
    private bool _isLive;

    public bool IsAlive => !_isDestroyed && _litCount > 0;

    private void Awake()
    {
      this.AsInjected();
      _destructible = GetComponent<DestructibleObject>();
      _hitFx = GetComponent<HitFx>();

      var durability = Mathf.Max(1, _battleBalance.GetDestructibleMaxHealth(DestructibleObjectType.HealthBar));
      _damagePerPixel = durability / (float)PixelCount;
    }

    private void OnEnable()
    {
      _characterService.HealthChanged += OnHealthChanged;
      _battleService.BattleStarted += OnBattleStarted;

      // The bar can be placed either side of the battle starting, so catch the case where it has
      // already begun and BattleStarted will not fire again.
      _isLive |= _battleService.IsBattleActive;
      Sync(transform.position);
    }

    private void OnDisable()
    {
      _characterService.HealthChanged -= OnHealthChanged;
      _battleService.BattleStarted -= OnBattleStarted;
    }

    private void OnBattleStarted()
    {
      _isLive = true;
      Sync(transform.position);
    }

    public void TakeDamage(int damage) => TakeDamage(damage, transform.position);

    public void TakeDamage(int damage, Vector3 origin)
    {
      if (damage < 0 || !IsAlive || !_isLive || _battleService.IsWinning)
        return;

      if (_hitFx != null)
        _hitFx.PlayHit();

      _damageTaken += damage;

      // Charge the player for every pixel the accumulated damage has now paid for. The pixels
      // themselves are removed by Sync, off the back of the health change, so a pixel is never
      // counted twice however the health was lost.
      var owed = Mathf.Min(PixelCount, Mathf.FloorToInt(_damageTaken / _damagePerPixel));
      while (_pixelsPaidFor < owed && _characterService.IsAlive)
      {
        _pixelsPaidFor++;
        _characterService.TakeDamage(HealthPerPixel);
      }

      // The invincibility cheat swallows that damage, so the health never moves and Sync has nothing
      // to remove: an invincible player cannot smash his own health bar either. Sync anyway, so a
      // pixel the player paid for while mortal is still taken off.
      Sync(origin);
    }

    private void OnHealthChanged(int current) => Sync(transform.position);

    /// <summary>
    /// Brings the standing pixels back in line with the health that is actually left, knocking off
    /// however many the bar is now over. Everything funnels through here — hits on the bar, duck
    /// bites, anything else that moves the number — so the bar can only ever show the truth.
    /// </summary>
    private void Sync(Vector3 origin)
    {
      // The map is filled from LevelData.Start, which can run before the battle is started and the
      // player's health is dealt out. Reading a health of zero then would shatter the whole bar the
      // moment it was placed; there is simply nothing to mirror until the battle is live.
      if (_isDestroyed || !_isLive)
        return;

      var desired = DesiredLitCount();
      while (_litCount > desired)
      {
        _litCount--;

        var pixel = _pixels[_litCount];
        if (pixel != null)
          _destructible.FallOutPart(pixel, origin);
      }

      if (_litCount > 0)
        return;

      // Every pixel is loose debris now, but the group itself is still standing and nothing has told
      // the decay manager to retire it. Break does that, and is what BattleHealthObject listens for.
      _isDestroyed = true;
      _destructible.Break(origin);
    }

    /// How many pixels the remaining health spells out. Rounded up, so any health at all leaves at
    /// least one pixel standing and an empty bar means exactly one thing.
    private int DesiredLitCount()
    {
      var max = _characterService.MaxHealth;
      if (max <= 0)
        return 0;

      var ratio = _characterService.CurrentHealth / (float)max;
      return Mathf.Clamp(Mathf.CeilToInt(ratio * PixelCount), 0, PixelCount);
    }

    private void OnValidate()
    {
      if (_pixels.Length != PixelCount)
        System.Array.Resize(ref _pixels, PixelCount);
    }
  }
}
