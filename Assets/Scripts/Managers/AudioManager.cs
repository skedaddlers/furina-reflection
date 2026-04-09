using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource musicSource;
    public float musicVolume = 0.5f;
    public float sfxVolume = 1.0f;
    public float voiceLineVolume = 1.0f;
    public AudioSource sfxSource;
    public AudioSource voiceLineSource;
    public AudioSource dialogueSource;
    public List<AudioSource> additionalSFXSources; // for more overlapping SFX if needed
    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;
    public AudioClip bossMusic;
    public AudioClip bossMusicPhase2;
    public AudioClip victoryMusic;
    public AudioClip defeatMusic;
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


    void Start()
    {
        if (additionalSFXSources == null) additionalSFXSources = new List<AudioSource>();
        for (int i = 0; i < 3; i++) // create a few additional SFX sources for overlapping sounds
        {
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            additionalSFXSources.Add(newSource);
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

    public void PlayGameplayMusic()
    {
        musicSource.clip = gameplayMusic;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlayMainMenuMusic()
    {
        musicSource.clip = mainMenuMusic;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlayBossMusic()
    {
        musicSource.clip = bossMusic;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlayBossMusicPhase2()
    {
        musicSource.clip = bossMusicPhase2;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlayVictoryMusic()
    {
        musicSource.clip = victoryMusic;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlayDefeatMusic()
    {
        musicSource.clip = defeatMusic;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.pitch = 1f; // reset pitch to default
        sfxSource.volume = sfxVolume;
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySFXNoOverlap(AudioClip clip, bool randomizePitch = false, float duration = -1f)
    {
        foreach (var source in additionalSFXSources)
        {
            if (!source.isPlaying)
            {
                if (randomizePitch)
                {
                    source.pitch = Random.Range(pitchVariationMin, pitchVariationMax);
                }
                else
                {
                    source.pitch = 1f; // reset pitch to default
                }
                source.volume = sfxVolume;
                if (duration > 0f)
                {
                    StartCoroutine(PlaySFXWithDuration(clip, sfxVolume, duration, source));
                }
                else
                {
                    source.PlayOneShot(clip);
                }
                return;
            }
        }
    }

    public void PlaySFXWithVolume(AudioClip clip, float volume, float duration = -1f)
    {
        sfxSource.pitch = 1f; // reset pitch to default
        if (duration > 0f)
        {
            StartCoroutine(PlaySFXWithDuration(clip, volume, duration, sfxSource));
        }
        else
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    private IEnumerator PlaySFXWithDuration(AudioClip clip, float volume, float duration, AudioSource source)
    {
        source.PlayOneShot(clip, volume);
        yield return new WaitForSeconds(duration);
        source.Stop();
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
        // dont play if dialogue source is currently playing to avoid overlap with dialogue lines
        if (dialogueSource.isPlaying) return;
        voiceLineSource.volume = voiceLineVolume;
        voiceLineSource.PlayOneShot(clip);
    }

    public void PlayDialogueLine(AudioClip clip)
    {
        dialogueSource.volume = voiceLineVolume;
        dialogueSource.PlayOneShot(clip);
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
        foreach (var source in additionalSFXSources)
        {
            source.volume = sfxVolume;
        }
    }

    public void SetVoiceLineVolume(float volume)
    {
        voiceLineVolume = volume;
        voiceLineSource.volume = voiceLineVolume;
        dialogueSource.volume = voiceLineVolume;
    }
}
