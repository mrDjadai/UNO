[System.Serializable]
public struct CardData
{
    public CardColor Color;
    public CardValue Value;

    public enum CardColor
    {
        Invalid,
        Red,
        Green,
        Blue,
        Yellow,
        Black
    }

    public enum CardValue
    {
        V0,
        V1,
        V2,
        V3,
        V4,
        V5,
        V6,
        V7,
        V8,
        V9,
        Stop,
        Reverse,
        Get2,
        Get4,
        Colorize
    }
}