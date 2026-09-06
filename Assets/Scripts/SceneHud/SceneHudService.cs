using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace SceneHud
{
  /// <summary>
  /// The meeting point between in-world <see cref="SceneHudElement"/>s and the HUD widgets that
  /// display them. Elements publish the render texture their camera draws into; views look it up by
  /// id and follow <see cref="Changed"/> from there.
  ///
  /// An element that is not in the scene simply has no entry, which is how a widget knows to hide
  /// itself rather than showing a stale last frame.
  ///
  /// A dying element can ask for a <see cref="Hold"/> before its replacement arrives: while the hold
  /// runs, a newly registered texture is parked as pending and the widget keeps showing the old one,
  /// so the player sees the object it mirrors come apart before the HUD cuts away.
  /// </summary>
  public class SceneHudService : ITickable
  {
    private readonly Dictionary<SceneHudElementId, RenderTexture> _textures = new();
    private readonly Dictionary<SceneHudElementId, RenderTexture> _pending = new();
    private readonly Dictionary<SceneHudElementId, float> _holdUntil = new();
    private readonly List<SceneHudElementId> _expiredHolds = new();

    /// Raised with the new texture, or null once the element is gone.
    public event Action<SceneHudElementId, RenderTexture> Changed;

    public RenderTexture Get(SceneHudElementId id) =>
      _textures.TryGetValue(id, out var texture) ? texture : null;

    /// <summary>
    /// Keeps whatever is currently registered for <paramref name="id"/> on the HUD for
    /// <paramref name="seconds"/>, parking any replacement registered in that window until it runs
    /// out. Called by the element that is about to die, which is the only one that knows there is
    /// something worth watching before the swap.
    /// </summary>
    public void Hold(SceneHudElementId id, float seconds)
    {
      if (seconds <= 0f || !_textures.ContainsKey(id))
        return;

      _holdUntil[id] = Time.unscaledTime + seconds;
    }

    public void Register(SceneHudElementId id, RenderTexture texture)
    {
      if (texture == null)
        return;

      if (_holdUntil.ContainsKey(id) && _textures.TryGetValue(id, out var current) && current != texture)
      {
        _pending[id] = texture;
        return;
      }

      Apply(id, texture);
    }

    /// <summary>
    /// Drops <paramref name="texture"/>'s registration. The texture is passed in so that an element
    /// tearing down after a replacement has already registered cannot clear the newer entry.
    /// </summary>
    public void Unregister(SceneHudElementId id, RenderTexture texture)
    {
      // A replacement that dies while parked never reached the HUD, so it just stops being the
      // pending one; the held element carries on.
      if (_pending.TryGetValue(id, out var pending) && pending == texture)
      {
        _pending.Remove(id);
        return;
      }

      if (!_textures.TryGetValue(id, out var current) || current != texture)
        return;

      // The held element is gone early — with nothing left to watch, its replacement takes over now
      // rather than blinking the widget out for the rest of the hold.
      if (_pending.TryGetValue(id, out var next))
      {
        Apply(id, next);
        return;
      }

      _textures.Remove(id);
      _holdUntil.Remove(id);
      Changed?.Invoke(id, null);
    }

    void ITickable.Tick()
    {
      if (_holdUntil.Count == 0)
        return;

      var now = Time.unscaledTime;
      foreach (var pair in _holdUntil)
        if (now >= pair.Value)
          _expiredHolds.Add(pair.Key);

      foreach (var id in _expiredHolds)
        if (_pending.TryGetValue(id, out var next))
          Apply(id, next);
        else
          _holdUntil.Remove(id);

      _expiredHolds.Clear();
    }

    private void Apply(SceneHudElementId id, RenderTexture texture)
    {
      _textures[id] = texture;
      _pending.Remove(id);
      _holdUntil.Remove(id);
      Changed?.Invoke(id, texture);
    }
  }
}
