using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneHud
{
  /// <summary>
  /// The meeting point between in-world <see cref="SceneHudElement"/>s and the HUD widgets that
  /// display them. Elements publish the render texture their camera draws into; views look it up by
  /// id and follow <see cref="Changed"/> from there.
  ///
  /// An element that is not in the scene simply has no entry, which is how a widget knows to hide
  /// itself rather than showing a stale last frame.
  /// </summary>
  public class SceneHudService
  {
    private readonly Dictionary<SceneHudElementId, RenderTexture> _textures = new();

    /// Raised with the new texture, or null once the element is gone.
    public event Action<SceneHudElementId, RenderTexture> Changed;

    public RenderTexture Get(SceneHudElementId id) =>
      _textures.TryGetValue(id, out var texture) ? texture : null;

    public void Register(SceneHudElementId id, RenderTexture texture)
    {
      if (texture == null)
        return;

      _textures[id] = texture;
      Changed?.Invoke(id, texture);
    }

    /// <summary>
    /// Drops <paramref name="texture"/>'s registration. The texture is passed in so that an element
    /// tearing down after a replacement has already registered cannot clear the newer entry.
    /// </summary>
    public void Unregister(SceneHudElementId id, RenderTexture texture)
    {
      if (!_textures.TryGetValue(id, out var current) || current != texture)
        return;

      _textures.Remove(id);
      Changed?.Invoke(id, null);
    }
  }
}
