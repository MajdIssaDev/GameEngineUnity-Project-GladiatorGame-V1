using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundAutoDestroy : MonoBehaviour
{
    private void Start()
    {
        AudioSource audioSource = GetComponent<AudioSource>();

        if (audioSource != null && audioSource.clip != null)
        {
            // Destroy this object after the clip finishes playing
            Destroy(gameObject, audioSource.clip.length);
        }
        else
        {
            // Fallback safety: Destroy after 2 seconds if something is wrong
            Destroy(gameObject, 2f);
        }
    }
}