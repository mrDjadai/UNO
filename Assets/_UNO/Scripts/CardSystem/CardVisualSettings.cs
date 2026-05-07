using UnityEngine;
using AYellowpaper.SerializedCollections;
using static CardData;

[CreateAssetMenu(menuName = "UNO/CardVisual", fileName = "CardVisualSettings")]
public class CardVisualSettings : ScriptableObject
{
    [field: SerializeField] public SerializedDictionary<CardValue, Sprite> CenterSprites { get; private set; }
    [field: SerializeField] public SerializedDictionary<CardValue, Sprite> CornerSprites { get; private set; }
    [field: SerializeField] public SerializedDictionary<CardColor, Color> UsedColors { get; private set; }
}
