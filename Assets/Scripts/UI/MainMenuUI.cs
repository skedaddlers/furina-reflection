using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public Button startButton;
    public Button quitButton;
    public string mainSceneName = "MainScene";

    void Start()
    {
        startButton.onClick.AddListener(LoadGame);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMainMenuMusic();
        }
        quitButton.onClick.AddListener(() =>
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.QuitGame();
            }
            else
            {
                // Debug.LogWarning("SceneLoader instance not found. Quitting application directly.");
            }
        });
    }
    
    void LoadGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Restart();
        }
        else
        {
            // Debug.LogWarning("GameManager instance not found. Loading main scene directly.");
            SceneLoader.Instance.LoadScene(mainSceneName);
        }
    }

}