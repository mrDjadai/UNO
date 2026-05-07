using UnityEngine;
using UnityEngine.UI;
using static CardData;
using NaughtyAttributes;

public class CardVisualisator : MonoBehaviour
{
    [SerializeField] private CardVisualSettings settings;
    [SerializeField] private Image colorIndicator;
    [SerializeField] private Image center;
    [SerializeField] private Image[] corner;

   /* [SerializeField] private CardValue testValue;
    [SerializeField] private CardColor testColor;

    [Button]
    private void Test()
    {
        Visualise(testValue, testColor);    
    }
   */
    public void Visualise(CardValue val, CardColor color)
    {
        Color c = settings.UsedColors[color];

        colorIndicator.color = c;
        center.sprite = settings.CenterSprites[val];
        if (val < CardValue.Get4)
        {
            center.color = c;
        }
        else
        {
            center.color = Color.white;
        }
        foreach (var item in corner)
        {
            item.sprite = settings.CornerSprites[val];
        }
    }
}
