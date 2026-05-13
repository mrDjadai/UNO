using UnityEngine;
using UnityEngine.InputSystem;

public class MenuInputManager : MonoBehaviour
{
    private GameInput input;
    private void Awake()
    {
        input = new GameInput();

        input.Menu.Escape.performed += Escape;

        input.Enable();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Escape(InputAction.CallbackContext context)
    {
        WindowOpener.instance.CloseWindow();
    }
}
