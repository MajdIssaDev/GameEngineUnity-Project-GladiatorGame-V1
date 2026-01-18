using UnityEngine;

public class MusicManager : MonoBehaviour
{
    void Start()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            // This line forces the music to IGNORE the pause menu silence
            audioSource.ignoreListenerPause = true;
        }
    }
}