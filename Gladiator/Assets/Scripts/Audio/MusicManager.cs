using UnityEngine;

public class MusicManager : MonoBehaviour
{
    void Start()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            //Allows background music to keep playing even when AudioListener paused by the GameManager
            audioSource.ignoreListenerPause = true;
        }
    }
}