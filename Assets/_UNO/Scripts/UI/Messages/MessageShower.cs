using UnityEngine;
using DG.Tweening;
using TMPro;

public class MessageShower : MonoBehaviour
{
    public static MessageShower Instance { get; private set; }
    [SerializeField] private float showTime;
    [SerializeField] private float hideTime;
    [SerializeField] private TMP_Text text;
    [SerializeField] private Color textColor;

    private Tween tween;

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

        text.color = Color.clear;
    }

    public void Show(string txt)
    {
        if (tween != null)
        {
            tween.Kill();
        }
        text.text = txt;
        text.color = textColor;
        tween = text.DOColor(Color.clear, hideTime).SetDelay(showTime);
    }
}
