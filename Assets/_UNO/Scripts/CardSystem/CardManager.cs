using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CardData;

public class CardManager : NetworkBehaviour
{
    public static CardManager Instance { get; private set; }
    public Transform PilePoint => pilePoint;

    [SerializeField] private Card cardPrefab;
    [SerializeField] private Transform deckOrigin;
    [SerializeField] private Transform cardSpawnPoint;
    [SerializeField] private Transform pilePoint;

    [SerializeField] private int startCardCount = 7;
    [SerializeField] private float addCartDelay = 0.1f;

    [Header("Visual")]
    [SerializeField] private Transform deckOriginMovable;
    [SerializeField] private float minDeckY;
    [SerializeField] private float maxDeckY;
    private const int DeckSize = 108;
    private Card upperCard;

    private List<CardData> deck = new();
    private List<CardData> discardPile = new();

    [SyncVar] private CardData currentCard;

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

    public void OnStartGame()
    {
        GenerateDeck();
        ShuffleDeck();

        CardData firstCard;

        for (int i = 0; i < deck.Count; i++)
        {
            if (deck[i].Color != CardColor.Black)
            {
                firstCard = deck[i];
                deck.RemoveAt(i);
                discardPile.Add(firstCard);
                currentCard = firstCard;
                SpawnFirstCard(firstCard);
                break;
            }
        }

        DrawStartCards();
        UpdateDeckHeight();
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
        UpdateDeckHeight();

        return card;
    }

    [Command]
    public void PlaceCard(CardData data)
    {
        discardPile.Insert(0, data);
        currentCard = data;
    }


    public void RotateDeckToPlayer()
    {
        Vector3 playerPos =
            TurnManager.Instance.GetPlayerPosition(NetworkClient.localPlayer.netId);

        Vector3 direction = playerPos - deckOrigin.position;

        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            deckOrigin.forward = direction.normalized;
        }
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

    public void AddCardToPlayer(uint netId)
    {
        CardData data = DrawCard();

        PlayerController player = NetworkClient.spawned[netId].GetComponent<PlayerController>();

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

        return currentCard.Color == data.Color || currentCard.Value == data.Value;
    }

    [ClientRpc]
    private void UpdateDeckHeight()
    {
        Vector3 pos = deckOriginMovable.localPosition;
        pos.y = Mathf.Lerp(minDeckY, maxDeckY, (float)deck.Count / DeckSize);
        deckOriginMovable.localPosition = pos;
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