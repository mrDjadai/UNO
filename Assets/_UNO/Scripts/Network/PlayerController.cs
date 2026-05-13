using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : NetworkBehaviour
{
    public static PlayerController LocalPlayer { get; private set; }

    [SerializeField] private MessageValue messages;
    [SerializeField] private GameObject cameraOrigin;
    [SerializeField] private HandVisualizer handVisualizer;
    [SerializeField] private Skin[] skins;

    [SerializeField] private Transform nickOrigin;

    [SerializeField] private TMP_Text nickText;

    public readonly SyncList<CardData> Hand = new();
    private bool drawCard;
    private bool currentTurn;

    public uint PlayerId => netId;
    private int selectedCard = 0;
    private string nickName;

    private void Start()
    {
        cameraOrigin.SetActive(isLocalPlayer);
        if (!isLocalPlayer)
        {
            Destroy(cameraOrigin.transform.GetChild(0).GetComponent<AudioListener>());
        }

        if (isLocalPlayer)
        {
            LocalPlayer = this;
            InputManager.InputActions.Player.ScrollCards.Disable();

            InputManager.InputActions.Player.ScrollCards.performed += ScrollCards;
            InputManager.InputActions.Player.DrawCard.performed += TryDrawCard;
            InputManager.InputActions.Player.UseCard.performed += TryPlaceCard;
        }
        TurnManager.Instance.OnTurnChanged += StartTurn;
    }

    public void AddCard(CardData data)
    {
        Hand.Add(data);
        MoveCardToHand(data);
        SortHand();
        RedrawHand();
    }

    public void EnableScrolling()
    {
        InputManager.InputActions.Player.ScrollCards.Enable();
    }

    private void SortHand()
    {
        List<CardData> temp = new List<CardData>(Hand);

        temp.Sort((a, b) =>
        {
            int colorCompare = a.Color.CompareTo(b.Color);

            if (colorCompare != 0)
                return colorCompare;

            return a.Value.CompareTo(b.Value);
        });

        Hand.Clear();

        foreach (var card in temp)
        {
            Hand.Add(card);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        TurnManager.Instance.RegisterPlayer(this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(InitNickname());

        if (isLocalPlayer)
        {
            CmdSendPlayerData(PlayerPrefs.GetString("Nickname"), PlayerPrefs.GetInt("Skin"));
        }
    }


    private void OnDestroy()
    {
        if (isLocalPlayer)
        {
            InputManager.InputActions.Player.ScrollCards.performed -= ScrollCards;
            InputManager.InputActions.Player.DrawCard.performed -= TryDrawCard;
            InputManager.InputActions.Player.UseCard.performed -= TryPlaceCard;
        }
    }

    private void ScrollCards(InputAction.CallbackContext context)
    {
        if (!TurnManager.Instance.IsStarted)
        {
            return;
        }
        if (Hand.Count == 0)
        {
            return;
        }

        float sign = Mathf.Sign(context.ReadValue<float>());

        if (sign == 0)
        {
            return;
        }

        int newSelected = selectedCard;

        if (sign > 0)
        {
            newSelected++;
        }
        else
        {
            newSelected--;
        }

        newSelected =
            (newSelected + Hand.Count) % Hand.Count;

        SelectCard(newSelected);
    }

    private void TryPlaceCard(InputAction.CallbackContext context)
    {
        if (!TurnManager.Instance.IsStarted)
        {
            return;
        }
        if (!currentTurn)
        {
            MessageShower.Instance.Show(messages.Message["OtherTurn"]);
            return;
        }

        if (CardManager.Instance.CanUseCard(Hand[selectedCard]) == false)
        {
            MessageShower.Instance.Show(messages.Message["IncorrectCard"]);
            return;
        }

        handVisualizer.ResetFirstSelect();
        OnPlaceCardCmd(selectedCard);

        if (Hand[selectedCard].Color == CardData.CardColor.Black)
        {
            ColorPicker.Instance.Open();
        }

        if (selectedCard != 0)
        {
            SelectCard(selectedCard - 1);
        }
        else
        {
            SelectCard(0);
        }

        currentTurn = false;
    }

    [Command]
    private void OnPlaceCardCmd(int pos)
    {
        CardManager.Instance.PlaceCard(Hand[pos]);
        PlaceCard(pos);
        Hand.RemoveAt(pos);
    }

    private void TryDrawCard(InputAction.CallbackContext context)
    {
        if (!currentTurn)
        {
            MessageShower.Instance.Show(messages.Message["OtherTurn"]);
            return;
        }

        if (drawCard && ServerDataContainer.Instance.TakeOnlyOneCard)
        {
            MessageShower.Instance.Show(messages.Message["CantDrawCard"]);
            return;
        }


        bool isSkipped = !CardManager.Instance.TurnSkipped 
            && CardManager.Instance.CurrentCard.Value == CardData.CardValue.Stop;

        if (isSkipped)
        {
            MessageShower.Instance.Show(messages.Message["SkipTurn"]);
            OnTurnSkip();
        }
        else
        {
            if (CardManager.Instance.GetCardCount > 0)
            {
                OnDrawCardsCmd();
                return;
            }
            if (CanUseAnyCard())
            {
                MessageShower.Instance.Show(messages.Message["CanUseAnyCard"]);
                return;
            }
        }

        OnDrawCardCmd(isSkipped);
    }

    [Command]
    private void OnTurnSkip()
    {
        CardManager.Instance.OnTurnSkip();
    }

    [Command]
    private void OnDrawCardsCmd()
    {
        for (int i = 0; i < CardManager.Instance.GetCardCount; i++)
        {
            CardManager.Instance.AddCardToPlayer(netId);
        }
        CardManager.Instance.ResetGettingCards();
        TurnManager.Instance.NextTurn();
    }

    [Command]
    private void OnDrawCardCmd(bool isSkipped)
    {
        if (isSkipped)
        {
            currentTurn = false;
            TurnManager.Instance.NextTurn();
            return;
        }

        drawCard = true;

        CardManager.Instance.AddCardToPlayer(netId);

        if (ServerDataContainer.Instance.TakeOnlyOneCard)
        {
            if (!CanUseAnyCard())
            {
                currentTurn = false;
                TurnManager.Instance.NextTurn();
            }
        }
    }

    public void SelectCard(int id)
    {
        handVisualizer.SelectCard(selectedCard, id);
        selectedCard = id;
    }

    [ClientRpc]
    public void OnGameStart()
    {
        if (isLocalPlayer == false)
        {
            return;
        }
        EnableScrolling();
        SelectCard(0);
    }

    public override void OnStopServer()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.UnregisterPlayer(this);
    }

    [ClientRpc]
    public void MoveCardToHand(CardData data)
    {
        handVisualizer.MoveCardToHand(data, netId);
    }

    [ClientRpc]
    public void RedrawHand()
    {
        handVisualizer.SortHand();
    }

    [ClientRpc]
    public void PlaceCard(int num)
    {
        handVisualizer.PlaceCard(num);
    }


    public void StartTurn(uint id)
    {
        currentTurn = (id == netId);
        if (currentTurn)
        {
            if (isLocalPlayer)
            {
                TurnRemainder.Instance.OnTurnStart();
            }
            drawCard = false;
        }
        else
        {
            if (isLocalPlayer)
            {
                TurnRemainder.Instance.OnTurnEnd();
            }
        }
    }


    private bool CanUseAnyCard()
    {
        foreach (var item in Hand)
        {
            if (CardManager.Instance.CanUseCard(item))
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerator InitNickname()
    {
        if (isLocalPlayer)
        {
            nickOrigin.gameObject.SetActive(false);
        }
        else
        {
            nickOrigin.gameObject.SetActive(true);

            yield return new WaitWhile(() => { return NetworkClient.localPlayer == null; }); 
            Vector3 playerPos =
                TurnManager.Instance.GetPlayerPosition(NetworkClient.localPlayer.netId);

            nickOrigin.LookAtVertical(playerPos);
        }
    }

    public void ChooseColor(CardData.CardColor color)
    {
        CmdChooseColor(color);
    }

    [Command]
    private void CmdChooseColor(CardData.CardColor color)
    {
        CardManager.Instance.overridedColor = color;
        TurnManager.Instance.NextTurn();
    }

    [Command]
    private void CmdSendPlayerData(string name, int skinID)
    {
        TurnManager.Instance.SetPlayerData(netId, name, skinID);
    }

    public void SetPlayerVisual(string name, int skinID)
    {
        nickText.text = name;
        foreach (var item in skins)
        {
            item.SetActive(false);
        }
        skins[skinID].SetActive(true);
    }
}

[System.Serializable]
public struct Skin
{
    [SerializeField] private GameObject[] parts;

    public void SetActive(bool active)
    {
        foreach (var item in parts)
        {
            item.SetActive(active);
        }
    }
}