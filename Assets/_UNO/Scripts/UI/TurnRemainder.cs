using UnityEngine;
using System.Collections;
using DG.Tweening;

public class TurnRemainder : MonoBehaviour
{
    public static TurnRemainder Instance { get; private set; } 
    [SerializeField] private Transform text;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float waitTime;
    [SerializeField] private float showMessageTime;
    [SerializeField] private float messageAnimationDuration;
    private Coroutine cor;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void OnTurnStart()
    {
        if (cor != null)
        {
            StopCoroutine(cor);
        }
        cor = StartCoroutine(Remind());
    }

    public void OnTurnEnd()
    {
        if (cor != null)
        {
            StopCoroutine(cor);
        }
    }

    private IEnumerator Remind()
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);

            if (TurnManager.Instance.IsStarted == false)
            {
                break;
            }
            audioSource.Play();

            Sequence sequence = DOTween.Sequence();

            sequence.Append(text.DOScale(1f, messageAnimationDuration));
            sequence.AppendInterval(showMessageTime);
            sequence.Append(text.DOScale(0f, messageAnimationDuration));

            sequence.Play();
        }
    }
}
