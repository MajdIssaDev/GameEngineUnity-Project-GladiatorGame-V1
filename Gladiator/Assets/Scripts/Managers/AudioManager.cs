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
    public GameObject sfxObjectPrefab; // A prefab with just an AudioSource on it

    private void Awake()
    {
        // Singleton Pattern (Like GameManager)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this manager between scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySFX(AudioClip clip, Vector3 position, float volumeMultiplier = 1.0f)
    {
        if (clip == null) return;

        // Create a temporary sound object
        // (For a bigger game, use "Object Pooling" here instead of Instantiate)
        GameObject soundObj = new GameObject("SFX_" + clip.name);
        soundObj.transform.position = position;

        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = sfxVolume * masterVolume * volumeMultiplier;
        source.spatialBlend = 1f; // 1 = 3D Sound, 0 = 2D Sound
        source.maxDistance = 20f; // Sound fades out after 20 meters
        source.rolloffMode = AudioRolloffMode.Linear;

        source.Play();

        // Destroy the object after the clip finishes
        Destroy(soundObj, clip.length + 0.1f);
    }
    
    public void PlayUI_SFX(AudioClip clip)
    {
        // Plays sound "globally" (2D) for buttons/UI
        if (clip == null) return;
        
        GameObject soundObj = new GameObject("UI_SFX_" + clip.name);
        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = sfxVolume * masterVolume;
        source.spatialBlend = 0f; // 2D Sound
        
        source.Play();
        Destroy(soundObj, clip.length);
    }
}