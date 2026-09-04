using UnityEngine;

namespace Combat
{
  public interface IImpactDamageable : IDamageable
  {
    void TakeDamage(int damage, Vector3 origin);
  }
}
