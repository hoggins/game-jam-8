using App;
using UnityEngine;
using VContainer;

namespace SceneHud
{
  /// <summary>
  /// The world half of a scene HUD element: a camera pointed at one in-world object, drawing it into
  /// a render texture that a <see cref="SceneHudElementView"/> shows on the HUD.
  ///
  /// This component only owns the render texture and the registration. Framing is left to the camera
  /// it sits on and is authored per prefab: its placement, projection, clip planes and its culling
  /// mask, which is what decides how much of the world around the element comes along with it. Note
  /// that culled geometry does not occlude either, so anything that would otherwise be hidden behind
  /// the world (a part sunk below the ground plane, say) has to be cut off by the clip planes
  /// instead.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Camera))]
  public sealed class SceneHudElement : MonoBehaviour
  {
    [Tooltip("Which HUD widget this element feeds.")]
    [SerializeField] private SceneHudElementId _id;

    [Tooltip("Render texture size in pixels. Its aspect has to match the camera's framing.")]
    [SerializeField] private Vector2Int _resolution = new(768, 256);

    [Inject] private SceneHudService _sceneHud;

    private Camera _camera;
    private RenderTexture _texture;

    private void Awake()
    {
      this.AsInjected();
      _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
      _texture = new RenderTexture(
        Mathf.Max(1, _resolution.x),
        Mathf.Max(1, _resolution.y),
        16,
        RenderTextureFormat.ARGB32)
      {
        name = $"SceneHud_{_id}",
      };

      _camera.targetTexture = _texture;
      _sceneHud.Register(_id, _texture);
    }

    private void OnDisable()
    {
      _sceneHud.Unregister(_id, _texture);

      _camera.targetTexture = null;
      if (_texture == null)
        return;

      _texture.Release();
      Destroy(_texture);
      _texture = null;
    }
  }
}
