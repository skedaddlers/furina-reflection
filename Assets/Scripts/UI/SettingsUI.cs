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

    private GameState previousState;

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

    void OnVoiceLineVolumeChanged(float value)
    {
        AudioManager.Instance.SetVoiceLineVolume(value);
    }

    void OpenSettings()
    {
        GameManager.Instance.SetCursorState(true);
        previousState = GameManager.Instance.CurrentState;
        GameManager.Instance.ChangeState(GameState.Paused);
        settingsPanel.SetActive(true);
        GameManager.Instance.player.GetComponent<PlayerController>().ResetAllStates();
    }

    void CloseSettings()
    {
        if(previousState == GameState.Playing)
        {
            GameManager.Instance.SetCursorState(false);
        }
        settingsPanel.SetActive(false);
        GameManager.Instance.ChangeState(previousState);
    }
}