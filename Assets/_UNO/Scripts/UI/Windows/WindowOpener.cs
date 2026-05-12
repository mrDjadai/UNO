using UnityEngine;
using UnityEngine.UI;

public class WindowOpener : MonoBehaviour
{
    public static WindowOpener instance { get; private set; }
    public Window openedWindow => currentWindow;
    [SerializeField, Tooltip("Открывается, когда нет открытых окон")] private Window baseWindow;
    [SerializeField] private bool initOnAwake;
    [SerializeField] private bool allowReOpen;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip openSound;

    private Window currentWindow;

    private void Awake()
    {
        if (initOnAwake)
        {
            Init();
        }
    }

    public void Init()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            throw new System.Exception("Может быть только 1 объект типа WindowOpener");
        }

        if (baseWindow != null)
        {
            OpenWindow(baseWindow, true);
        }
        if (audioSource)
        {
            audioSource.enabled = true;
        }
    }

    public void OpenWindow(Window window)
    {
        OpenWindow(window, false);
    }

    public void OpenWindow(Window window, bool isForce)
    {
      //  Debug.Log(window + "  opened");
        if (allowReOpen || currentWindow == null || window.FatherWindow == currentWindow)
        {
            if (allowReOpen && currentWindow != null)
            {
                currentWindow.Close();
            }
            currentWindow = window;
            window.Open(isForce);
            if (audioSource)
            {
                audioSource.PlayOneShot(openSound);
            }
        }
    }

    public void CloseWindow()
    {
        if (currentWindow != null && currentWindow != baseWindow)
        {

            currentWindow.Close();
            if (currentWindow.HasCloseSound && audioSource)
            {
                audioSource.PlayOneShot(closeSound);
            }

            //      Debug.Log(currentWindow);
            //     Debug.Log(currentWindow.FatherWindow);
            if (currentWindow.FatherWindow != null)
            {
                if (allowReOpen)
                {
                    OpenWindow(currentWindow.FatherWindow);
                }
                else
                {
                    currentWindow = currentWindow.FatherWindow;
                }
            }
            else
            {
                currentWindow = null;
                if (baseWindow != null)
                {
                    OpenWindow(baseWindow);  
                }
            }
        }
    }

    public void CloseAll()
    {
        while (currentWindow != null)
        {
            CloseWindow();
        }
    }

    public void CloseWindow(Window win)
    {
        if (currentWindow == win)
        {
            CloseWindow();
        }
    }

    public bool IsCurrent(Window win)
    {
        return win == currentWindow;
    }


    [System.Serializable]
    private struct ShortCut
    {
        public KeyCode code;
        public Window window;
        public Button button;
    }
}
