using System.Collections.Generic;
using UnityEngine;

public class HandVisualizer : MonoBehaviour
{
    [Header("Hand Drawing")]
    [SerializeField] private Transform handPoint;
    [SerializeField] private float handMaxAngle = 30f;
    [SerializeField] private float handCardSpacing = 0.35f;
    [SerializeField] private float handCurveOffset = 0.05f;
    [SerializeField] private float handDepthOffset = 0.01f;

    private List<Card> cards = new List<Card>();

    public void MoveCardToHand(CardData data, uint netId)
    {
        Card card = CardManager.Instance.SpawnCard(data, handPoint, netId, false);
        cards.Add(card);
        RedrawHand();
    }

    public void SortHand()
    {
        if (cards == null || cards.Count <= 1)
            return;

        cards.Sort((a, b) =>
        {
            CardData aData = a.Data;
            CardData bData = b.Data;

            int colorCompare = aData.Color.CompareTo(bData.Color);

            if (colorCompare != 0)
                return colorCompare;

            return aData.Value.CompareTo(bData.Value);
        });

        RedrawHand();
    }

    public void RedrawHand()
    {
        if (cards == null || cards.Count == 0)
            return;

        int count = cards.Count;

        float centerIndex = (count - 1) * 0.5f;

        float angleStep = count > 1
            ? handMaxAngle / centerIndex
            : 0f;

        for (int i = 0; i < count; i++)
        {
            Card card = cards[i];

            float offset = i - centerIndex;

            float angle = -offset * angleStep;

            Vector3 position =
                handPoint.position +
                handPoint.right * offset * handCardSpacing -
                handPoint.forward * offset * handDepthOffset;

            position +=
                handPoint.up *
                (-Mathf.Abs(offset) * handCurveOffset);

            Vector3 rotation =
                handPoint.eulerAngles +
                new Vector3(0f, 0f, angle);

            card.MoveToPoint(position, rotation, false);
        }
    }

    public void SelectCard(int oldSelected, int newSelected)
    {
        cards[oldSelected].SetSelected(false);
        cards[newSelected].SetSelected(true);
    }
}