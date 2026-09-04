using UnityEngine;
using UnityEngine.InputSystem;

namespace App
{
  public abstract class UiBase : MonoBehaviour
  {
    [SerializeField] private InputActionReference _cancelAction;

    private InputAction _subscribedCancelAction;
    private bool _enabledCancelAction;

    protected virtual void OnEnable()
    {
      if (_cancelAction == null)
        return;

      _subscribedCancelAction = _cancelAction.action;
      _subscribedCancelAction.performed += CancelPerformed;

      _enabledCancelAction = !_subscribedCancelAction.enabled;
      if (_enabledCancelAction)
        _subscribedCancelAction.Enable();
    }

    protected virtual void OnDisable()
    {
      if (_subscribedCancelAction == null)
        return;

      _subscribedCancelAction.performed -= CancelPerformed;
      if (_enabledCancelAction)
        _subscribedCancelAction.Disable();

      _subscribedCancelAction = null;
      _enabledCancelAction = false;
    }

    protected virtual void OnCancel()
    { }

    private void CancelPerformed(InputAction.CallbackContext context) =>
      OnCancel();
  }
}
