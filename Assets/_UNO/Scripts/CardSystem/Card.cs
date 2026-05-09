using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CardVisualisator))]
public class Card : MonoBehaviour
{
    public CardData Data => currentData;
    public bool IsVisible
    {
        get
        {
            return isVisible;
        }
        set
        {
            isVisible = value;
            cardVisualisator.SetVisible(value);
        }
    }

    private bool isVisible;
    [SerializeField] private float movingSpeed = 1f;
    [SerializeField] private float jumpPower = 1f;
    [SerializeField] private float selectOffset = 0.01f;
    [SerializeField] private float selectVerticalOffset = 0.02f;

    private Vector3 currentTarget;
    private Vector3 currentTargetAngles;
    private CardData currentData;

    private CardVisualisator cardVisualisator;
    private Tween moveTween;
    private Tween rotationTween;
    private Transform tr;

    private void Awake()
    {
        tr = transform;
        cardVisualisator = GetComponent<CardVisualisator>();    
    }

    public void Init(CardData data, bool visible)
    {
        currentData = data;
        IsVisible = visible;
        cardVisualisator.Visualise(data.Value, data.Color);
    }

    public void MoveToPoint(Transform point, bool useJump)
    {
        MoveToPoint(point.position, point.eulerAngles, useJump);
    }

    public void MoveToPoint(Vector3 point, Vector3 angles, bool useJump)
    {
        currentTarget = point; 
        currentTargetAngles = angles;

        if (moveTween != null)
        {
            moveTween.Kill();
            rotationTween.Kill();
        }

        float duration = Vector3.Distance(tr.position, point) / movingSpeed;

        if (useJump)
        {
            moveTween = tr.DOJump(point, jumpPower, 1, duration);
        }
        else
        {
            moveTween = tr.DOMove(point, duration);
        }
        rotationTween = tr.DORotate(angles, duration);
    }

    public void SetSelected(bool v)
    {
        cardVisualisator.SetSelected(v);
        MoveToPoint(currentTarget + tr.forward * (v? selectOffset : -selectOffset), currentTargetAngles, false);
        MoveToPoint(currentTarget + tr.up * (v? selectVerticalOffset: -selectVerticalOffset), currentTargetAngles, false);
    }
}
