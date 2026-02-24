using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    [Header("References")]
    public AudioSource musicSource;
    public GameObject sfxObjectPrefab; 

    private void Awake()
    {
        //Ensures global access to the audio system across all scripts without expensive FindObjectOfType calls
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySFX(AudioClip clip, Vector3 position, float volumeMultiplier = 1.0f)
    {
        if (clip == null) return;

        GameObject soundObj = new GameObject("SFX_" + clip.name);
        soundObj.transform.position = position;

        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = sfxVolume * masterVolume * volumeMultiplier;
        
        //Configure 3D audio attenuation (Linear Rolloff) so the sound volume accurately scales down
        //based on the distance to the AudioListener
        source.spatialBlend = 1f; 
        source.maxDistance = 20f; 
        source.rolloffMode = AudioRolloffMode.Linear;

        source.Play();

        //Destroy the object after the clip finishes
        Destroy(soundObj, clip.length + 0.1f);
    }
    
    public void PlayUI_SFX(AudioClip clip)
    {
        if (clip == null) return;
        
        GameObject soundObj = new GameObject("UI_SFX_" + clip.name);
        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = sfxVolume * masterVolume;
        
        //Force 2D spatial blend for UI sounds so they play globally at a constant volume
        source.spatialBlend = 0f; 
        
        source.Play();
        Destroy(soundObj, clip.length);
    }
}