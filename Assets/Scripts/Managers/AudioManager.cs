using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource musicSource;
    public float musicVolume = 0.5f;
    public float sfxVolume = 1.0f;
    public AudioSource sfxSource;
    public AudioSource voiceLineSource;
    public AudioClip gameplayMusic;

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
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.volume = sfxVolume;
        sfxSource.PlayOneShot(clip);
    }

    public void StopVoiceLine()
    {
        voiceLineSource.Stop();
    }

    public void PlayVoiceLine(AudioClip clip)
    {
        voiceLineSource.volume = sfxVolume;
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
}