using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public Button startButton;
    public string mainSceneName = "MainScene";

    void Start()
    {
        startButton.onClick.AddListener(LoadGame);
        if (AudioManager.Instance != null)
        {
            Debug.Log("Playing main menu music.");
            AudioManager.Instance.PlayMainMenuMusic();
        }
    }
    
    void LoadGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Restart();
        }
        else
        {
            Debug.LogWarning("GameManager instance not found. Loading main scene directly.");
            SceneLoader.Instance.LoadScene(mainSceneName);
        }
    }

}