using UnityEngine;
using AYellowpaper.SerializedCollections;
using static CardData;

[CreateAssetMenu(menuName = "UNO/CardVisual", fileName = "CardVisualSettings")]
public class CardVisualSettings : ScriptableObject
{
    [field: SerializeField] public SerializedDictionary<CardValue, Texture> CenterSprites { get; private set; }
    [field: SerializeField] public SerializedDictionary<CardValue, Texture> CornerSprites { get; private set; }
    [field: SerializeField] public SerializedDictionary<CardColor, Color> UsedColors { get; private set; }
}
