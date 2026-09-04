using UnityEngine;

namespace Destruction
{
  [CreateAssetMenu(fileName = "HitFxSettings", menuName = "Destruction/Hit Fx Settings")]
  public sealed class HitFxSettings : ScriptableObject
  {
    [SerializeField, Min(0f)] private float duration = 0.25f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    public float Duration => duration;
    public AnimationCurve Curve => curve;
  }
}
