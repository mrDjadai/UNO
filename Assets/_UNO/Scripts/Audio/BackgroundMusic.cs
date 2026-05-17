using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    [SerializeField] private AudioClip[] musicClips;
    private AudioSource audioSource;

    private void Start()
    {
        StopAllCoroutines();
        StartCoroutine(CheckMusic());
    }

    private IEnumerator CheckMusic()
    {
        audioSource = GetComponent<AudioSource>();
        while (true)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = musicClips[Random.Range(0, musicClips.Length)];
                audioSource.Play();
            }
            yield return new WaitForSeconds(2);
        }
    }
}
