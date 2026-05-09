using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private MessageValue messages;
    [SerializeField] private GameObject cameraOrigin;
    [SerializeField] private HandVisualizer handVisualizer;
    public readonly SyncList<CardData> Hand = new();
    [SyncVar] private bool drawCard;
    [SyncVar] private bool currentTurn;

    public uint PlayerId => netId;
    private int selectedCard = 0;

    private void Start()
    {
        cameraOrigin.SetActive(isLocalPlayer);
        if (isLocalPlayer)
        {
            InputManager.InputActions.Player.ScrollCards.performed += ScrollCards;
            InputManager.InputActions.Player.DrawCard.performed += TryDrawCard;
            InputManager.InputActions.Player.UseCard.performed += TryPlaceCard;

            TurnManager.Instance.OnTurnChanged += StartTurn;
        }
    }
    
    public void AddCard(CardData data)
    {
        Hand.Add(data);
        MoveCardToHand(data);
        SortHand();
        RedrawHand();
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

        CardManager.Instance.PlaceCard(Hand[selectedCard]);
        Hand.RemoveAt(selectedCard);

        handVisualizer.ResetFirstSelect();
        PlaceCard(selectedCard);

        if (selectedCard != 0)
        {
            SelectCard(selectedCard - 1);
        }
        else
        {
            SelectCard(0);
        }


        EndTurn();
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

        if (CanUseAnyCard())
        {
            MessageShower.Instance.Show(messages.Message["CanUseAnyCard"]);
            return;
        }


        drawCard = true;
        CardManager.Instance.AddCardToPlayer(netId);

        if (ServerDataContainer.Instance.TakeOnlyOneCard && !CanUseAnyCard())
        {
            EndTurn();
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
        SelectCard(0);
    }

    public override void OnStopServer()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.UnregisterPlayer(this);
    }

    public void EndTurn()
    {
        if (!isLocalPlayer) 
            return;

        currentTurn = false;
        CmdEndTurn();
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
        if (!isLocalPlayer) 
            return;

        currentTurn = true;
        drawCard = false;
    }

    [Command]
    private void CmdEndTurn()
    {
        if (!currentTurn)
            return;


        TurnManager.Instance.NextTurn();
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
}