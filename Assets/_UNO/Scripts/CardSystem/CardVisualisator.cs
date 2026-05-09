using UnityEngine;
using static CardData;

public class CardVisualisator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private CardVisualSettings settings;

    [Header("Renderers")]
    [SerializeField] private MeshRenderer colorIndicator;
    [SerializeField] private MeshRenderer center;
    [SerializeField] private MeshRenderer[] corner;

    [Header("Selection")]
    [SerializeField] private GameObject outline;

    private MaterialPropertyBlock propertyBlock;

    private static readonly int MainTex =
        Shader.PropertyToID("_BaseMap");

    private static readonly int BaseColor =
        Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    public void Visualise(CardValue val, CardColor color)
    {
        Color c = settings.UsedColors[color];

        SetColor(colorIndicator, null, c);

        Texture centerTexture = settings.CenterSprites[val];

        if (val < CardValue.Get4)
        {
            SetColor(center, centerTexture, c);
        }
        else
        {
            SetColor(center, centerTexture, Color.white);
        }

        Texture cornerTexture = settings.CornerSprites[val];

        foreach (var item in corner)
        {
            SetColor(item, cornerTexture, Color.white);
        }
    }

    private void SetColor(
        MeshRenderer renderer,
        Texture texture,
        Color color)
    {
        renderer.GetPropertyBlock(propertyBlock);

        if (texture != null)
        {
            propertyBlock.SetTexture(MainTex, texture);
        }

        propertyBlock.SetColor(BaseColor, color);

        renderer.SetPropertyBlock(propertyBlock);
    }

    public void SetVisible(bool visible)
    {
        colorIndicator.enabled = visible;
        center.enabled = visible;

        foreach (var item in corner)
        {
            item.enabled = visible;
        }
    }

    public void SetSelected(bool selected)
    {
        outline.SetActive(selected);
    }
}