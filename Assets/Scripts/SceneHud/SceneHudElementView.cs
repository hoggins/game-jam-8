using App;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SceneHud
{
  /// <summary>
  /// The HUD half of a scene HUD element: shows whatever the matching <see cref="SceneHudElement"/>
  /// is currently rendering, and hides itself while there is no such element in the scene, so a
  /// destroyed or not-yet-spawned object leaves no stale frame behind.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(RawImage))]
  public sealed class SceneHudElementView : MonoBehaviour
  {
    [Tooltip("Which in-world element this widget shows.")]
    [SerializeField] private SceneHudElementId _id;

    [Tooltip("Optional image shown while the in-world element is destroyed.")]
    [SerializeField] private Image _destroyedImage;

    [Inject] private SceneHudService _sceneHud;

    private RawImage _image;

    private void Awake()
    {
      this.AsInjected();
      _image = GetComponent<RawImage>();
    }

    private void OnEnable()
    {
      _sceneHud.Changed += OnChanged;
      Apply(_sceneHud.Get(_id));
    }

    private void OnDisable() => _sceneHud.Changed -= OnChanged;

    private void OnChanged(SceneHudElementId id, RenderTexture texture)
    {
      if (id == _id)
        Apply(texture);
    }

    private void Apply(RenderTexture texture)
    {
      _image.texture = texture;
      _image.enabled = texture != null;
      if (_destroyedImage != null)
        _destroyedImage.enabled = texture == null;
    }
  }
}
