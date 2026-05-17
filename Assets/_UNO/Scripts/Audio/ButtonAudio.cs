using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class ButtonAudio : MonoBehaviour
{
    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Toggle[] toggles = FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
       // Slider[] sliders = FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var item in buttons)
        {
            item.onClick.AddListener(OnClick);
        }
        foreach (var item in toggles)
        {
            item.onValueChanged.AddListener(OnClick);
        }
    /*    foreach (var item in sliders)
        {
            item.on.AddListener(OnClick);
        }*/
    }

    private void OnClick()
    {
        source.PlayOneShot(source.clip);
    }

    private void OnClick(bool v)
    {
        source.PlayOneShot(source.clip);
    }

 /*   private void OnClick(float v)
    {
        source.PlayOneShot(source.clip);
    }*/
}
