using System.Collections.Generic;
using System.Collections;
using Mirror;
using UnityEngine;
using static CardData;

public class CardManager : NetworkBehaviour
{
    [SerializeField] private Card cardPrefab;
    [SerializeField] private Transform deckOrigin;
    [SerializeField] private Transform cardSpawnPoint;
    [SerializeField] private Transform pilePoint;

    public static CardManager Instance {  get; private set; }

    private List<CardData> deck = new();
    private List<CardData> discardPile = new();

    [SyncVar(hook = nameof(OnTopDiscardChanged))]
    private CardData topDiscard;

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

    public override void OnStartServer()
    {
        base.OnStartServer();

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

                topDiscard = firstCard;
                return;
            }
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

        return card;
    }

    [Server]
    public void AddToDiscard(CardData card)
    {
        discardPile.Add(card);
    }

    private void OnTopDiscardChanged(CardData oldData, CardData newData)
    {
        if (oldData.Color != CardColor.Invalid)
        {
            return;
        }

        StartCoroutine(SpawnFirstCard(newData, pilePoint));
    }

    public override void OnStartLocalPlayer()
    {
        RotateDeckToPlayer();
        base.OnStartLocalPlayer();
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

    private IEnumerator SpawnFirstCard(CardData data, Transform targetPoint)
    {
        yield return new WaitForSeconds(2);
        SpawnCard(data, targetPoint);
    }

    private Card SpawnCard(CardData data, Transform targetPoint)
    {
        Card card = Instantiate(cardPrefab, cardSpawnPoint.position, cardSpawnPoint.rotation);
        card.Init(data);
        card.MoveToPoint(targetPoint);
        return card;
    }
}