using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class StartPanel : NetworkBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private string noPlayersTxt;
    [SerializeField] private string canStartTxt;
    [SerializeField] private float hideDuration = 0.5f;

    private void Awake()
    {
        group.alpha = 0;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void Start()
    {
        if (NetworkServer.active == false)
        {
            InputManager.Instance.SetCursorLocked(true);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        TurnManager.Instance.OnPlayersListUpdated += UpdateVisual;
        UpdateVisual(TurnManager.Instance.GetLocalPlayersInfo());
        group.alpha = 1;
        group.interactable = true;
        group.blocksRaycasts = true;
        startButton.onClick.AddListener(OnButtonClick);
        InputManager.Instance.SetCursorLocked(false);
        InputManager.InputActions.Disable();

    }

    private void OnDisable()
    {
        if (NetworkServer.active)
        {
            TurnManager.Instance.OnPlayersListUpdated -= UpdateVisual;
        }
    }

    private void UpdateVisual(Dictionary<uint, PlayerInfo> data)
    {
        countText.text = data.Count.ToString() + "/6";

        bool canStart = data.Count > 1;

        typeText.text = canStart ? canStartTxt : noPlayersTxt;
        startButton.interactable = canStart;
    }

    private void OnButtonClick()
    {
        group.transform.DOScale(Vector3.zero, hideDuration);

        TurnManager.Instance.StartGame();

        InputManager.InputActions.Enable();
        InputManager.Instance.SetCursorLocked(true);
    }
}
