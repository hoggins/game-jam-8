using UnityEngine;

namespace Map
{
  [CreateAssetMenu(fileName = "EnvironmentVisibilitySettings", menuName = "Map/Environment Visibility Settings")]
  public sealed class EnvironmentVisibilitySettings : ScriptableObject
  {
    [Tooltip("Objects within this distance of the player can be rendered.")]
    [SerializeField, Min(0f)] private float visibleRadius = 100f;

    [Tooltip("Extra distance an object must cross before it is hidden again, preventing visibility changes at the boundary.")]
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
