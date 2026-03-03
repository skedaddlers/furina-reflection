using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public GameObject settingsPanel;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Button closeSettingsButton;

    void Start()
    {
        settingsPanel.SetActive(false);
        musicVolumeSlider.value = AudioManager.Instance.musicVolume;
        sfxVolumeSlider.value = AudioManager.Instance.sfxVolume;
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        closeSettingsButton.onClick.AddListener(CloseSettings);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf)
                CloseSettings();
            else
                OpenSettings();
        }
    }

    void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance.SetMusicVolume(value);
    }

    void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
    }

    void OpenSettings()
    {
        GameManager.Instance.SetCursorState(true);
        GameManager.Instance.ChangeState(GameState.InMenu);
        settingsPanel.SetActive(true);
    }

    void CloseSettings()
    {
        GameManager.Instance.SetCursorState(false);
        settingsPanel.SetActive(false);
        GameManager.Instance.ChangeState(GameState.Playing);
    }
}