using UnityEngine;
using UnityEngine.UI;

namespace ScenesManagement
{
  public class SceneLoadingUi : MonoBehaviour
  {
    [SerializeField] private GameObject _root;
    [SerializeField] private Slider _progressBar;

    public void EnableLoading(bool isEnabled)
    {
      var root = _root != null ? _root : gameObject;
      root.SetActive(isEnabled);
    }

    public void SetProgress(float progress)
    {
      if (_progressBar != null)
        _progressBar.value = Mathf.Clamp01(progress);
    }
  }
}
