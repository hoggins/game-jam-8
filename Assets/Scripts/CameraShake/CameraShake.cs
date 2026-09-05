using Unity.Cinemachine;
using UnityEngine;

namespace CameraShake
{
  [DisallowMultipleComponent]
  [AddComponentMenu("Camera Shake/Camera Shake")]
  /// <summary>One-shot shake prefab component. Instantiate the prefab to play it.</summary>
  public sealed class CameraShake : MonoBehaviour
  {
    [SerializeField, Min(0.0001f)] private float _duration = 0.25f;
    [SerializeField, Min(0f)] private float _magnitude = 1f;
    [SerializeField] private AnimationCurve _falloff = new(
      new Keyframe(0f, 1f),
      new Keyframe(1f, 0f));
    [SerializeField] private CameraShakeManager _manager;

    private void OnEnable()
    {
      if (!Application.isPlaying)
        return;

      var manager = _manager != null ? _manager : FindManagerInScene();
      manager?.Play(_duration, _magnitude, _falloff);
      Destroy(gameObject);
    }

    private CameraShakeManager FindManagerInScene()
    {
      var roots = gameObject.scene.GetRootGameObjects();
      for (var i = 0; i < roots.Length; i++)
      {
        var manager = roots[i].GetComponentInChildren<CameraShakeManager>(true);
        if (manager != null)
          return manager;
      }

      var perlin = FindPerlinInScene(roots);
      return perlin == null
        ? null
        : perlin.gameObject.AddComponent<CameraShakeManager>();
    }

    private CinemachineBasicMultiChannelPerlin FindPerlinInScene(GameObject[] roots)
    {
      for (var i = 0; i < roots.Length; i++)
      {
        var perlin = roots[i].GetComponentInChildren<CinemachineBasicMultiChannelPerlin>(true);
        if (perlin != null)
          return perlin;
      }

      return null;
    }

    private void OnValidate()
    {
      _duration = Mathf.Max(0.0001f, _duration);
      _magnitude = Mathf.Max(0f, _magnitude);
    }
  }
}
