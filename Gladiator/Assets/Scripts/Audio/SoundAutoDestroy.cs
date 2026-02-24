using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundAutoDestroy : MonoBehaviour
{
    private void Start()
    {
        AudioSource audioSource = GetComponent<AudioSource>();

        if (audioSource != null && audioSource.clip != null)
        {
            //Destroy the object when the clip finishes
            Destroy(gameObject, audioSource.clip.length);
        }
        else
        {
            //Failsafe, destory after 2 seconds in case the clip is missing so we don't get a memoey leak
            Destroy(gameObject, 2f);
        }
    }
}