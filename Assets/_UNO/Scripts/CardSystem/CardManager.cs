using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CardData;

public class CardManager : NetworkBehaviour
{
    public static CardManager Instance { get; private set; }
    public Transform PilePoint => pilePoint;
    public CardData CurrentCard => currentCard;
    public bool TurnSkipped => turnSkipped;
    public int GetCardCount => getCardCount;

    [SerializeField] private Card cardPrefab;
    [SerializeField] private Transform deckOrigin;
    [SerializeField] private Transform cardSpawnPoint;
    [SerializeField] private Transform pilePoint;

    [SerializeField] private int startCardCount = 7;
    [SerializeField] private float addCartDelay = 0.1f;

    [Header("Visual")]
    [SerializeField] private ColorIndicator colorIndicator;
    [SerializeField] private Transform deckOriginMovable;
    [SerializeField] private float minDeckY;
    [SerializeField] private float maxDeckY;
    private const int DeckSize = 108;
    private Card upperCard;

    private List<CardData> deck = new();
    private List<CardData> discardPile = new();

    [SyncVar(hook = nameof(UpdateDeckHeight))] private int deckLenght;
    [SyncVar] private CardData currentCard;
    [SyncVar] private bool turnSkipped;
    [SyncVar] private int getCardCount;
    [SyncVar(hook = nameof(UpdateColor))] public CardColor overridedColor = CardColor.Invalid;

    [Server]
    public void ResetGettingCards()
    {
        getCardCount = 0;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Server]
    public void OnTurnSkip()
    {
        turnSkipped = true;
    }

    public void OnStartGame()
    {
        GenerateDeck();
        ShuffleDeck();

        CardData firstCard;

        for (int i = 0; i < deck.Count; i++)
        {
            if (deck[i].Color != CardColor.Black && deck[i].Value <= CardValue.V9)
            {
                firstCard = deck[i];
                deck.RemoveAt(i);
                discardPile.Add(firstCard);
                currentCard = firstCard;
                SpawnFirstCard(firstCard);
                break;
            }
        }
        deckLenght = deck.Count;

        DrawStartCards();
    }

    [Server]
    private void DrawStartCards()
    {
        StartCoroutine(SpawnStartCards());
    }

    private IEnumerator SpawnStartCards()
    {
        Dictionary<uint, PlayerInfo> players = TurnManager.Instance.GetLocalPlayersInfo();

        for (int i = 0; i < startCardCount; i++)
        {
            foreach (var item in players.Keys)
            {
                AddCardToPlayer(item);
                yield return new WaitForSeconds(addCartDelay);
            }
        }

        foreach (var item in players.Keys)
        {
            PlayerController player = NetworkClient.spawned[item].GetComponent<PlayerController>();
            player.OnGameStart();
        }
    }

    [Server]
    public void GenerateDeck()
    {
        deck.Clear();
        discardPile.Clear();

        for (int i = 1; i < 5; i++)
        {
            CardColor c = (CardColor)i;

            CardData zero = new CardData();
            zero.Color = c;
            zero.Value = CardValue.V0;
            deck.Add(zero);

            for (int k = 1; k <= 12; k++)
            {
                CardData card = new CardData();
                card.Color = c;
                card.Value = (CardValue)k;
                deck.Add(card);
                deck.Add(card);
            }
        }

        CardData wild1 = new CardData();
        wild1.Color = CardColor.Black;
        wild1.Value = CardValue.Get4;

        CardData wild2 = new CardData();
        wild2.Color = CardColor.Black;
        wild2.Value = CardValue.Colorize;

        for (int i = 0; i < 4; i++)
        {
            deck.Add(wild1);
            deck.Add(wild2);
        }
        deckLenght = deck.Count;
    }

    [Server]
    public void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int randomIndex = Random.Range(i, deck.Count);

            (deck[i], deck[randomIndex]) = (deck[randomIndex], deck[i]);
        }
    }

    private void RebuildDeckFromDiscard()
    {
        deck.AddRange(discardPile);
        discardPile.Clear();
        deckLenght = deck.Count;
        ShuffleDeck();
    }


    [Server]
    public CardData DrawCard()
    {
        if (deck.Count == 0)
        {
            RebuildDeckFromDiscard();
        }

        if (deck.Count == 0)
        {
            throw new System.Exception("No cards available to draw.");
        }

        CardData card = deck[0];
        deck.RemoveAt(0);
        deckLenght = deck.Count;

        return card;
    }

    [Server]
    public void PlaceCard(CardData data)
    {
        discardPile.Insert(0, data);
        currentCard = data;

        switch (data.Value)
        {
            case CardValue.Stop:
                turnSkipped = false;
                break;
            case CardValue.Reverse:
                TurnManager.Instance.ReverseDirection();
                break;
            case CardValue.Get2:
                if (ServerDataContainer.Instance.SummarizeGetCards)
                {
                    getCardCount += 2;
                }
                else
                {
                    getCardCount = 2;
                }
                break;
            case CardValue.Get4:
                getCardCount = 4;
                break;
            case CardValue.Colorize:
                break;
            default:
                break;
        }

        if (data.Color != CardColor.Black)
        {
            if (overridedColor != CardColor.Invalid)
            {
                overridedColor = CardColor.Invalid;
            }
            TurnManager.Instance.NextTurn();
        }
    }


    public void RotateDeckToPlayer()
    {
        Vector3 playerPos =
            TurnManager.Instance.GetPlayerPosition(NetworkClient.localPlayer.netId);

        deckOrigin.LookAtVertical(playerPos);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        StartCoroutine(WaitForLocalPlayer());
    }

    private IEnumerator WaitForLocalPlayer()
    {
        while (NetworkClient.localPlayer == null)
        {
            yield return null;
        }

        RotateDeckToPlayer();
    }

    [ClientRpc]
    private void SpawnFirstCard(CardData data)
    {
        upperCard = SpawnCard(data, pilePoint, null, true);
    }

    [Server]
    public void AddCardToPlayer(uint netId)
    {
        CardData data = DrawCard();

        PlayerController player = NetworkServer.spawned[netId].GetComponent<PlayerController>();

        player.AddCard(data);
    }

    public Card SpawnCard(CardData data, Transform targetPoint, uint? ownerPlayer, bool useJump)
    {
        Card card = Instantiate(cardPrefab, cardSpawnPoint.position, cardSpawnPoint.rotation);
        bool isVisible = ownerPlayer == null;
        if (!isVisible)
        {
            isVisible = NetworkClient.localPlayer.netId == ownerPlayer;
        }

        card.Init(data, isVisible);
        card.MoveToPoint(targetPoint, useJump);
        return card;
    }

    public bool CanUseCard(CardData data)
    {
        if (data.Color == CardColor.Black)
        {
            return currentCard.Value != CardValue.Get4;
        }

        if (overridedColor != CardColor.Invalid)
        {
            if (getCardCount > 0)
            {
                return data.Value == CardValue.Get2 && data.Color == overridedColor;
            }
            return data.Color == overridedColor;
        }

        if (currentCard.Value == CardValue.Stop && !turnSkipped)
        {
            return data.Value == CardValue.Stop;
        }

        if (getCardCount > 0)
        {
            return data.Value == CardValue.Get2;
        }

        return currentCard.Color == data.Color || currentCard.Value == data.Value;
    }

    private void UpdateDeckHeight(int oldValue, int newValue)
    {
        Vector3 pos = deckOriginMovable.localPosition;
        pos.y = Mathf.Lerp(minDeckY, maxDeckY, (float)deckLenght / DeckSize);
        deckOriginMovable.localPosition = pos;
    }

    private void UpdateColor(CardColor oldValue, CardColor newValue)
    {
        colorIndicator.Indicate(newValue);
    }

    public void SetUpperCard(Card card)
    {
        if (upperCard != null)
        {
            Destroy(upperCard.gameObject);
        }
        upperCard = card;
        card.transform.position = pilePoint.position;
    }
}