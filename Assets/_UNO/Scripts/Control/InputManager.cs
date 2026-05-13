using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public static GameInput InputActions { get; private set; }

    public void SetCursorLocked(bool isLokced)
    {
        Cursor.lockState = isLokced? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLokced;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Can exists only 1 inputManager");
        }
        InputActions = new GameInput();
        InputActions.Enable();
    }
}
