using UnityEngine;

namespace Rendering
{
  [DisallowMultipleComponent]
  [AddComponentMenu("Rendering/Billboard Rendering")]
  public sealed class BillboardRendering : MonoBehaviour
  {
    [SerializeField] private Vector3 _rotationOffset;

    private void LateUpdate()
    {
      var camera = Camera.main;
      if (camera == null)
        return;

      var directionToCamera = camera.transform.position - transform.position;
      if (directionToCamera.sqrMagnitude <= Mathf.Epsilon)
        return;

      transform.rotation = Quaternion.LookRotation(directionToCamera) * Quaternion.Euler(_rotationOffset);
    }
  }
}
