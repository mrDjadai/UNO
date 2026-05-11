using UnityEngine;
using Mirror;
using DG.Tweening;

public class ColorPicker : MonoBehaviour
{
    public static ColorPicker Instance { get; private set; }
    [SerializeField] private Transform origin;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private CanvasGroup group;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void Open()
    {
        group.interactable = true;
        origin.DOScale(Vector3.one, animationDuration);
        InputManager.Instance.SetCursorLocked(false);
        InputManager.InputActions.Player.Disable();
    }

    private void Close()
    {
        group.interactable = false;
        origin.DOScale(Vector3.zero, animationDuration);
        InputManager.Instance.SetCursorLocked(true);
        InputManager.InputActions.Player.Enable();
    }

    public void OnColorChoose(int color)
    {
        PlayerController.LocalPlayer.ChooseColor(
            (CardData.CardColor)color);

        Close();
    }
}
