using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public GameObject settingsPanel;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider voicelineVolumeSlider;
    public Button restartButton;
    public Button closeSettingsButton;
    public Button mainMenuButton;
    public bool isSettingsOpen = false;

    void Start()
    {
        settingsPanel.SetActive(false);
        musicVolumeSlider.value = AudioManager.Instance.musicVolume;
        sfxVolumeSlider.value = AudioManager.Instance.sfxVolume;
        voicelineVolumeSlider.value = AudioManager.Instance.voiceLineVolume;
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        voicelineVolumeSlider.onValueChanged.AddListener(OnVoiceLineVolumeChanged);

        restartButton.onClick.AddListener(() => GameManager.Instance.Restart());
        closeSettingsButton.onClick.AddListener(CloseSettings);
        mainMenuButton.onClick.AddListener(() => SceneLoader.Instance.LoadScene(GameManager.Instance.mainMenuSceneName));
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
            else if (UIManager.Instance == null || !UIManager.Instance.HasActiveMenu(this))
            {
                OpenSettings();
            }
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

    void OnVoiceLineVolumeChanged(float value)
    {
        AudioManager.Instance.SetVoiceLineVolume(value);
    }

    void OpenSettings()
    {
        if (UIManager.Instance != null && !UIManager.Instance.TryOpenSettings(this))
        {
            return;
        }

        isSettingsOpen = true;
        settingsPanel.SetActive(true);
        GameManager.Instance.player.GetComponent<PlayerController>().ResetAllStates();
    }

    void CloseSettings()
    {
        isSettingsOpen = false;
        settingsPanel.SetActive(false);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseSettings(this);
        }
    }
}
