using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource musicSource;
    public float musicVolume = 0.5f;
    public float sfxVolume = 1.0f;
    public float voiceLineVolume = 1.0f;
    public AudioSource sfxSource;
    public AudioSource voiceLineSource;
    public AudioClip gameplayMusic;
    public float pitchVariationMin = 0.9f;
    public float pitchVariationMax = 1.1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void DestroyInstanceForRestart()
    {
        if (Instance == null) return;
        var go = Instance.gameObject;
        Instance = null;
        Object.Destroy(go);
    }

    public void PlayMusic(AudioClip clip)
    {
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.pitch = 1f; // reset pitch to default
        sfxSource.volume = sfxVolume;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayWithVaryingPitch(AudioClip clip)
    {
        sfxSource.pitch = Random.Range(pitchVariationMin, pitchVariationMax);
        sfxSource.PlayOneShot(clip, sfxVolume);
        // sfxSource.pitch = 1f; // reset pitch after playing
    }

    public void StopVoiceLine()
    {
        voiceLineSource.Stop();
    }

    public void PlayVoiceLine(AudioClip clip)
    {
        voiceLineSource.volume = voiceLineVolume;
        voiceLineSource.PlayOneShot(clip);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        sfxSource.volume = sfxVolume;
        voiceLineSource.volume = sfxVolume;
    }

    public void SetVoiceLineVolume(float volume)
    {
        voiceLineVolume = volume;
        voiceLineSource.volume = voiceLineVolume;
    }
}
