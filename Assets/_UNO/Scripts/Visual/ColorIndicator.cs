using UnityEngine;
using static CardData;

public class ColorIndicator : MonoBehaviour
{
    [SerializeField] private CardVisualSettings settings;
    [SerializeField] private MeshRenderer rend;
    [SerializeField] private Color baseColor;

    private void Awake()
    {
        SetColor(baseColor);
    }

    public void Indicate(CardColor color)
    {
        if (color == CardColor.Invalid)
        {
            SetColor(baseColor);
        }
        else
        {
            SetColor(settings.UsedColors[color]);
        }
    }

    private void SetColor(Color color)
    {
        rend.material.SetColor("_BaseColor", color);
    }
}
