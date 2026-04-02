using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource musicSource;
    public float musicVolume = 0.5f;
    public float sfxVolume = 1.0f;
    public float voiceLineVolume = 1.0f;
    public AudioSource sfxSource;
    public AudioSource secondarySFXSource; // for overlapping SFX
    public AudioSource voiceLineSource;
    public AudioSource dialogueSource;
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

    public void PlaySFXNoOverlap(AudioClip clip, bool randomizePitch = false)
    {
        if (randomizePitch)
            secondarySFXSource.pitch = Random.Range(pitchVariationMin, pitchVariationMax);
        else
            secondarySFXSource.pitch = 1f; // reset pitch to default

        secondarySFXSource.volume = sfxVolume;
        if (!secondarySFXSource.isPlaying)
            secondarySFXSource.PlayOneShot(clip);
    }

    public void PlaySFXWithVolume(AudioClip clip, float volume, float duration = -1f)
    {
        sfxSource.pitch = 1f; // reset pitch to default
        if (duration > 0f)
        {
            StartCoroutine(PlaySFXWithDuration(clip, volume, duration));
        }
        else
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    private IEnumerator PlaySFXWithDuration(AudioClip clip, float volume, float duration)
    {
        sfxSource.PlayOneShot(clip, volume);
        yield return new WaitForSeconds(duration);
        sfxSource.Stop();
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
        secondarySFXSource.volume = sfxVolume;
    }

    public void SetVoiceLineVolume(float volume)
    {
        voiceLineVolume = volume;
        voiceLineSource.volume = voiceLineVolume;
        dialogueSource.volume = voiceLineVolume;
    }
}
