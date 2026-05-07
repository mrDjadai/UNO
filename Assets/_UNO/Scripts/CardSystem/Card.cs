using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CardVisualisator))]
public class Card : MonoBehaviour
{
    [SerializeField] private float movingSpeed = 1f;
    [SerializeField] private float jumpPower = 1f;
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

    public void Init(CardData data)
    {
        currentData = data;
        cardVisualisator.Visualise(data.Value, data.Color);
    }

    public void MoveToPoint(Transform point)
    {
        if (moveTween != null)
        {
            moveTween.Kill();
            rotationTween.Kill();
        }

        float duration = Vector3.Distance(tr.position, point.position) / movingSpeed;

        moveTween = tr.DOJump(point.position, jumpPower, 1, duration);
        rotationTween = tr.DORotate(point.eulerAngles, duration);
    }
}
