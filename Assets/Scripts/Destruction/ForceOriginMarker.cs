using UnityEngine;

namespace Destruction
{
  public class ForceOriginMarker : MonoBehaviour
  {
    private void OnDrawGizmos()
    {
      Gizmos.color = Color.red;
      Gizmos.DrawSphere(transform.position, 2);
    }
  }
}
