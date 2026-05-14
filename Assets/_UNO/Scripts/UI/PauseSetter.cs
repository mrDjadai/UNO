using UnityEngine;
using UnityEngine.InputSystem;

public class PauseSetter : MonoBehaviour
{
    [SerializeField] private UIAnimator animator;
    [SerializeField] private StartPanel startPanel;
    private bool isPaused;

    private void Start()
    {
        InputManager.InputActions.Menu.Escape.performed += SwitchPause;
    }

    private void Pause(InputAction.CallbackContext context)
    {
        InputManager.Instance.SetCursorLocked(false);
        InputManager.InputActions.Player.Disable();
        animator.Open();
        isPaused = true;
    }

    private void UnPause(InputAction.CallbackContext context)
    {
        if (startPanel.BlockControl == false)
        {
            InputManager.Instance.SetCursorLocked(true);
            InputManager.InputActions.Player.Enable();
        }
        animator.Close();
        isPaused = false;
    }

    public void UnPause()
    {
        UnPause(new InputAction.CallbackContext());
    }

    public void SwitchPause(InputAction.CallbackContext context)
    {
        if (isPaused)
        {
            UnPause(new InputAction.CallbackContext());
        }
        else
        {
            Pause(new InputAction.CallbackContext());
        }
    }
}
