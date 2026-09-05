using System;
using UnityEngine;

namespace Weapons
{
  [DisallowMultipleComponent]
  public sealed class RangedWeapon : Weapon
  {
    [Header("Bullets")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField, Min(0)] private int _bulletPrewarmCount = 16;
    [SerializeField] private Transform _bulletPoint;
    [SerializeField, Min(1)] private int _bulletCount = 1;
    [SerializeField, Range(0f, 360f)] private float _bulletConeAngle;

    public int BulletCount => _bulletCount;

    protected override bool NotifyAttackPerformed => false;

    protected override void Awake()
    {
      base.Awake();
      PrewarmFx(_bulletPrefab, _bulletPrewarmCount);
    }

    protected override void Attack(int damage)
    {
      if (Pool == null || _bulletPrefab == null)
        return;

      var point = _bulletPoint != null ? _bulletPoint : transform;
      var owner = transform.root;
      var forward = GetOwnerForward(owner);
      var bulletCount = Mathf.Max(1, _bulletCount);

      for (var i = 0; i < bulletCount; i++)
      {
        var direction = GetBulletDirection(forward, i, bulletCount);
        var bulletObject = Pool.Get(
          _bulletPrefab,
          point.position,
          Quaternion.LookRotation(direction, Vector3.up));
        var bullet = bulletObject != null
          ? bulletObject.GetComponent<Bullet>()
          : null;

        if (bullet != null)
          bullet.Launch(direction, damage, owner);
        else if (bulletObject != null)
          Pool.Release(bulletObject);
      }
    }

    public void UpgradeBulletCount(int amount = 1)
    {
      if (amount < 0)
        throw new ArgumentOutOfRangeException(nameof(amount), amount, "Upgrade amount cannot be negative.");

      _bulletCount = checked(_bulletCount + amount);
    }

    private Vector3 GetBulletDirection(Vector3 forward, int index, int bulletCount)
    {
      if (bulletCount <= 1 || _bulletConeAngle <= 0f)
        return forward;

      var halfAngle = _bulletConeAngle * 0.5f;
      var angle = Mathf.Lerp(-halfAngle, halfAngle, index / (bulletCount - 1f));
      return Quaternion.AngleAxis(angle, Vector3.up) * forward;
    }

    private static Vector3 GetOwnerForward(Transform owner)
    {
      var forward = owner != null ? owner.forward : Vector3.forward;
      forward.y = 0f;
      return forward.sqrMagnitude > Mathf.Epsilon ? forward.normalized : Vector3.forward;
    }

    private void OnValidate()
    {
      _bulletPrewarmCount = Mathf.Max(0, _bulletPrewarmCount);
      _bulletCount = Mathf.Max(1, _bulletCount);
      _bulletConeAngle = Mathf.Clamp(_bulletConeAngle, 0f, 360f);
    }
  }
}
