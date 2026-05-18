using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static ServerDataContainer;

[RequireComponent(typeof(AudioSource))]
public class PlayerController : NetworkBehaviour
{
    public static PlayerController LocalPlayer { get; private set; }

    [SerializeField] private AudioClip drawCardClip;
    [SerializeField] private AudioClip playCardClip;
    [SerializeField] private AudioClip selectCardClip;
    [SerializeField] private AudioClip skipClip;

    [SerializeField] private MessageValue messages;
    [SerializeField] private GameObject cameraOrigin;
    [SerializeField] private HandVisualizer handVisualizer;
    [SerializeField] private Skin[] skins;
    private Skin currentSkin;

    [SerializeField] private Transform nickOrigin;

    [SerializeField] private TMP_Text nickText;

    [SyncVar] private bool saidUno = true;
    [SyncVar] public bool saidUnoSafe;

    public readonly SyncList<CardData> Hand = new();
    private bool drawCard;
    private bool currentTurn;
    private AudioSource source;


    public uint PlayerId => netId;
    private int selectedCard = 0;

    private void Start()
    {
        source = GetComponent<AudioSource>();
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
            InputManager.InputActions.Player.Uno.performed += TrySayUno;
        }
        else
        {
            foreach (var item in skins)
            {
                item.SetLayer(0);
            }
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
        if (!TurnManager.Instance.IsGameFull && !TurnManager.Instance.IsStarted)
        {
            TurnManager.Instance.RegisterPlayer(this);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(InitNickname());

        if (!isLocalPlayer)
        {
            return;
        }

        if (!TurnManager.Instance.IsGameFull && !TurnManager.Instance.IsStarted)
        {
            CmdSendPlayerData(PlayerPrefs.GetString("Nickname"), PlayerPrefs.GetInt("Skin"));
        }

        ServerDataContainer.Instance.SetDisconnectType(DisconnectType.NetworkError);

        if (TurnManager.Instance.IsStarted)
        {
            ServerDataContainer.Instance.SetDisconnectType(DisconnectType.GameStarted);
            StartCoroutine(DisconnectCor());

        }
        else if (TurnManager.Instance.IsGameFull)
        {
            ServerDataContainer.Instance.SetDisconnectType(DisconnectType.FullGame);
            StartCoroutine(DisconnectCor());
        }
    }

    private IEnumerator DisconnectCor()
    {
        yield return new WaitForEndOfFrame();
        NetworkClient.Disconnect();

    }

    private void OnDestroy()
    {
        if (isLocalPlayer && InputManager.InputActions != null)
        {
            InputManager.InputActions.Player.ScrollCards.performed -= ScrollCards;
            InputManager.InputActions.Player.DrawCard.performed -= TryDrawCard;
            InputManager.InputActions.Player.UseCard.performed -= TryPlaceCard;
            InputManager.InputActions.Player.Uno.performed -= TrySayUno;
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

        source.PlayOneShot(selectCardClip);

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

        if (!ServerDataContainer.Instance.AlwaysAllowWildCards && Hand[selectedCard].Color == CardData.CardColor.Black && CanUseAnyNotWildCard())
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

        if (Hand.Count == 1)
        {
            saidUno = false;
            TurnManager.Instance.StartUnoTimer(netId);
        }

        if (Hand.Count == 0)
        {
            TurnManager.Instance.OnPlayerHandEmpty(netId);
        }
    }

    [Server]
    public void ClearHand()
    {
        Hand.Clear();
        ClearHandRPC();
    }

    [ClientRpc]
    private void ClearHandRPC()
    {
        handVisualizer.ResetFirstSelect();
        handVisualizer.ClearVisual();
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
            source.PlayOneShot(skipClip);
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

    private void TrySayUno(InputAction.CallbackContext context)
    {
        PlayerController lastPlayer = TurnManager.Instance.GetLastPlayer();

        if (lastPlayer.netId != netId)
        {

            SaidUnoResult res = lastPlayer.OnSaidUno();

            switch (res)
            {
                case SaidUnoResult.NotUno:
                    MessageShower.Instance.Show(messages.Message["NotUno"]);
                    break;
                case SaidUnoResult.SaidUno:
                    MessageShower.Instance.Show(messages.Message["UnoSaid"]);
                    break;
                case SaidUnoResult.NotSaidUno:
                    AddUnoCards(lastPlayer.netId);
                    CmdSayUno();
                    break;
                case SaidUnoResult.SafeTime:
                    MessageShower.Instance.Show(messages.Message["UnoSafe"]);
                    break;
            }
            return;
        }

        if (Hand.Count > 1)
        {
            MessageShower.Instance.Show(messages.Message["NotUno"]);
            return;
        }

        if (saidUno)
        {
            MessageShower.Instance.Show(messages.Message["UnoSaid"]);

            return;
        }

        CmdSayUnoPlayer();
    }

    [Command]
    private void CmdSayUnoPlayer()
    {
        saidUno = true;
        SayUnoRPC();
    }


    [Command]
    private void CmdSayUno()
    {
        SayUnoRPC();
    }

    [ClientRpc]
    private void SayUnoRPC()
    {
        source.PlayOneShot(currentSkin.unoClip);
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
        source.PlayOneShot(drawCardClip);
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
        source.PlayOneShot(playCardClip);
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

    private bool CanUseAnyNotWildCard()
    {
        foreach (var item in Hand)
        {
            if (item.Color == CardData.CardColor.Black)
            {
                continue;
            }
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
        currentSkin = skins[skinID];
    }

    private SaidUnoResult OnSaidUno()
    {
        if (Hand.Count > 1)
        {
            return SaidUnoResult.NotUno;
        }

        if (saidUno)
        {
            return SaidUnoResult.SaidUno;
        }

        if (saidUnoSafe)
        {
            return SaidUnoResult.SafeTime;
        }

        return SaidUnoResult.NotSaidUno;
    }

    [Command]
    private void AddUnoCards(uint id)
    {
        CardManager.Instance.AddCardToPlayer(id);
        CardManager.Instance.AddCardToPlayer(id);
        NetworkServer.spawned[id].GetComponent<PlayerController>().saidUno = true;
    }

    public enum SaidUnoResult
    {
        NotUno,
        SaidUno,
        NotSaidUno,
        SafeTime
    }
}

[System.Serializable]
public struct Skin
{
    [SerializeField] private GameObject[] parts;
    public AudioClip unoClip;

    public void SetActive(bool active)
    {
        foreach (var item in parts)
        {
            item.SetActive(active);
        }
    }

    public void SetLayer(int layer)
    {
        foreach (var item in parts)
        {
            item.layer = layer;
        }
    }
}