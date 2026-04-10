using UnityEngine;
using UnityEngine.UI;

public class MainMenuSettings : MonoBehaviour
{
    public GameObject settingsPanel;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider voicelineVolumeSlider;
    public Button closeSettingsButton;
    public bool isSettingsOpen = false;

    void Start()
    {
        settingsPanel.SetActive(false);
        if (AudioManager.Instance != null)
        {
            musicVolumeSlider.value = AudioManager.Instance.musicVolume;
            sfxVolumeSlider.value = AudioManager.Instance.sfxVolume;
            voicelineVolumeSlider.value = AudioManager.Instance.voiceLineVolume;
        }
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        voicelineVolumeSlider.onValueChanged.AddListener(OnVoiceLineVolumeChanged);

        closeSettingsButton.onClick.AddListener(CloseSettings);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel == null)
                return;
            if (settingsPanel.activeSelf)
            {
                CloseSettings();
            }
        }
    }

    void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
    }

    void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
    }

    void OnVoiceLineVolumeChanged(float value)
    {
        AudioManager.Instance?.SetVoiceLineVolume(value);
    }

    void CloseSettings()
    {
        isSettingsOpen = false;
        settingsPanel.SetActive(false);
    }
}
