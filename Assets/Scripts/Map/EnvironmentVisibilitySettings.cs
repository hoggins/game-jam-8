using UnityEngine;

namespace Map
{
  [CreateAssetMenu(fileName = "EnvironmentVisibilitySettings", menuName = "Map/Environment Visibility Settings")]
  public class EnvironmentVisibilitySettings : ScriptableObject
  {
    [Tooltip("Spawned environment objects within this distance of the player are rendered.")]
    [SerializeField, Min(0f)] private float visibleRadius = 45f;

    [Tooltip("Extra distance beyond visibleRadius an object must cross before it is hidden again, so objects near the boundary don't flicker in and out every check.")]
    [SerializeField, Min(0f)] private float hiddenMargin = 15f;

    public float VisibleRadius => visibleRadius;
    public float HiddenRadius => visibleRadius + hiddenMargin;

    private void OnValidate()
    {
      visibleRadius = Mathf.Max(0f, visibleRadius);
      hiddenMargin = Mathf.Max(0f, hiddenMargin);
    }
  }
}
