using UnityEngine;
using System.Collections;

public class Window : MonoBehaviour
{
    [field: SerializeField] public bool CanBeCloseByShortcut = true;
    public bool HasCloseSound => hasCloseSound;
    public bool IsOpen => isOpen;
    public Window FatherWindow => fatherWindow;
    [SerializeField] private Window fatherWindow;
    [SerializeField] private UIAnimator[] animators = new UIAnimator[0];
    [SerializeField] protected bool hasCloseSound = true;
    private bool isOpen = false;

    public void DisableAnimators()
    {
        StartCoroutine(DisableAnimator());
    }

    private IEnumerator DisableAnimator()
    {
        foreach (var item in animators)
        {
            item.isActive = false;
        }

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        foreach (var item in animators)
        {
            item.isActive = true;
        }
    }

    public virtual void Open(bool isForce = false)
    {
        if (animators.Length != 0)
        {
            foreach (var item in animators)
            {
                if (item.isActive)
                {
                    item.Open(isForce);
                }
            }
        }
        else
        {
            gameObject.SetActive(true);
        }
        isOpen = true;
    }

    public virtual void Close()
    {
        if (animators.Length != 0)
        {
            foreach (var item in animators)
            {
                if (item.isActive)
                {
                    item.Close();
                }
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
        isOpen = false;
    }
}
