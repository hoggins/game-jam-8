using System.Collections;
using App;
using Combat;
using Destruction;
using Model;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VContainer;

namespace Health
{
  /// <summary>
  /// The player's health, standing in the street as a 2x12 bar of 24 smashable pixels.
  ///
  /// The bar is not a readout of health, it *is* the health: the ten logical segments are the
  /// player's hit points made physical, and knocking one off costs him a tenth of his maximum. Smash all of them
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

    private const float HitVignetteDuration = 0.25f;
    private const float HitVignetteIntensity = 0.6f;
    private const float HitVignettePriority = 100f;

    [Tooltip("Column-major, left to right: index = column * Rows + row. The bar empties from the last index back, so the pixels have to be ordered as the bar reads.")]
    [SerializeField] private DecayPart[] _pixels = new DecayPart[PixelCount];

    [Inject] private CharacterService _characterService;
    [Inject] private BattleService _battleService;

    private DestructibleObject _destructible;
    private HitFx _hitFx;
    private Volume _hitVignetteVolume;
    private VolumeProfile _hitVignetteProfile;
    private Vignette _hitVignette;
    private Coroutine _hitVignetteCoroutine;

    private const int HitCount = 10;
    private int HealthPerHit =>
      Mathf.Max(1, Mathf.RoundToInt(_characterService.MaxHealth / (float)HitCount));

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
      CreateHitVignette();
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

      if (_hitVignetteCoroutine != null)
      {
        StopCoroutine(_hitVignetteCoroutine);
        _hitVignetteCoroutine = null;
      }

      if (_hitVignetteVolume != null)
        _hitVignetteVolume.weight = 0f;
    }

    private void OnDestroy()
    {
      if (_hitVignetteProfile != null)
        Destroy(_hitVignetteProfile);
    }

    private void CreateHitVignette()
    {
      // The bar root is on the damageable layer, while the gameplay camera only samples volumes
      // from the Default layer. Keep the runtime volume on its own Default-layer child.
      var volumeObject = new GameObject("HealthBarHitVignette");
      volumeObject.layer = 0;
      volumeObject.transform.SetParent(transform, false);

      _hitVignetteVolume = volumeObject.AddComponent<Volume>();
      _hitVignetteVolume.isGlobal = true;
      _hitVignetteVolume.priority = HitVignettePriority;
      _hitVignetteVolume.weight = 0f;

      _hitVignetteProfile = ScriptableObject.CreateInstance<VolumeProfile>();
      _hitVignetteProfile.name = "HealthBarHitVignette";
      _hitVignetteVolume.sharedProfile = _hitVignetteProfile;

      _hitVignette = _hitVignetteProfile.Add<Vignette>(true);
      _hitVignette.color.value = new Color(1f, 0.03f, 0.03f);
      _hitVignette.intensity.value = HitVignetteIntensity;
      _hitVignette.smoothness.value = 0.2f;
    }

    private void PlayHitVignette()
    {
      if (_hitVignetteVolume == null || _hitVignette == null)
        return;

      if (_hitVignetteCoroutine != null)
        StopCoroutine(_hitVignetteCoroutine);

      _hitVignetteCoroutine = StartCoroutine(AnimateHitVignette());
    }

    private IEnumerator AnimateHitVignette()
    {
      var elapsed = 0f;
      _hitVignetteVolume.weight = 1f;

      while (elapsed < HitVignetteDuration)
      {
        elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(elapsed / HitVignetteDuration);
        _hitVignetteVolume.weight = 1f - progress;
        yield return null;
      }

      _hitVignetteVolume.weight = 0f;
      _hitVignetteCoroutine = null;
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

      PlayHitVignette();

      _characterService.TakeDamage(HealthPerHit);

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
