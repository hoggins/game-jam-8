using Unity.Cinemachine;
using UnityEngine;

namespace CameraShake
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(CinemachineImpulseSource))]
  [AddComponentMenu("Camera Shake/Shake Impulse")]
  public sealed class ShakeImpulse : MonoBehaviour
  {
    [SerializeField, Min(0f)] private float _amount = 1f;

    private CinemachineImpulseSource _source;

    private void Awake() =>
      _source = GetComponent<CinemachineImpulseSource>();

    private void OnEnable()
    {
      if (!Application.isPlaying)
        return;

      _source.GenerateImpulseWithForce(_amount);
    }

    private void OnValidate() =>
      _amount = Mathf.Max(0f, _amount);
  }
}
