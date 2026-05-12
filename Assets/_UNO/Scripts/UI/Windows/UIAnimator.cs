using UnityEngine;
using DG.Tweening;

public class UIAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform hiddenPoint;
    [SerializeField] private bool activeOnAwake;
    [SerializeField] private float animationTime;

    public bool isActive = true;
    private Vector3 defaultPosition;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        defaultPosition = rectTransform.anchoredPosition;

        hiddenPoint.SetParent(rectTransform.parent);


        if (activeOnAwake == false)
        {
            rectTransform.anchoredPosition = hiddenPoint.anchoredPosition;
        }
    }

    public void Open(bool isForce = false)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            defaultPosition = rectTransform.anchoredPosition;

        }
        if (isForce)
        {
            rectTransform.DOAnchorPos(defaultPosition, 0);

        }
        else
        {
            rectTransform.DOAnchorPos(defaultPosition, animationTime);
        }
    }

    public void Close()
    {
        rectTransform.DOAnchorPos(hiddenPoint.anchoredPosition, animationTime);
    }
}
