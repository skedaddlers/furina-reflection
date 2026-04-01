using UnityEngine;

public enum GameState
{
    MainMenu,
    Playing,
    MapView,
    Paused,
    GameOver,
    InMenu
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public Player player;
    public RoomManager roomManager;
    public RoomGenerator roomGenerator;
    public CursorController cursorController;
    public string mainSceneName = "MainScene";
    public string mainMenuSceneName = "MainMenu";

    public bool withDialogue = true;
    private bool _isRestarting;

    public GameState CurrentState { get; private set; }

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
        var layout = roomGenerator.Generate();
        roomManager.Initialize(layout);
        ChangeState(GameState.MainMenu);
        StartGame();
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

    public void Restart()
    {
        if (_isRestarting) return;
        _isRestarting = true;

        string targetSceneName = string.IsNullOrEmpty(mainSceneName)
            ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            : mainSceneName;

        Time.timeScale = 1f;
        UIManager.DestroyInstanceForRestart();
        PlayerStats.DestroyInstanceForRestart();
        AudioManager.DestroyInstanceForRestart();
        GlobalDifficultyState.DestroyInstanceForRestart();
        DDAMAPEKitFramework.DDAMAPEKit.DestroyInstanceForRestart();
        HitlagManager.DestroyInstanceForRestart();
        DestroyInstanceForRestart();

        UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
    }

    public void StartGame()
    {
        ChangeState(GameState.Playing);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(AudioManager.Instance.gameplayMusic);
        }
    }
    

    public void SetCursorState(bool unlocked)
    {
        if (unlocked)
        {
            cursorController.UnlockCursor();
        }
        else
        {
            cursorController.LockCursor();
        }
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        // Handle state-specific logic here (e.g., UI updates, pausing the game, etc.)
        switch (newState)
        {
            case GameState.MainMenu:
                Time.timeScale = 0;
                // Show main menu UI
                break;
            case GameState.Playing:
                Time.timeScale = 1;
                // Hide all menus
                break;
            case GameState.MapView:
                Time.timeScale = 0;
                break;
            case GameState.Paused:
                Time.timeScale = 0;
                // Show pause menu UI
                break;
            case GameState.GameOver:
                Time.timeScale = 0;
                // Show game over UI
                break;
            case GameState.InMenu:
                Time.timeScale = 0;
                // Show in-game menu UI
                break;
        }
    }
    
    public bool IsInGame()
    {
        return CurrentState == GameState.Playing;
    }

    public void OnBossRoomCleared()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowVictoryScreen();
        }
        ChangeState(GameState.GameOver);
    }

    public void OnPlayerDeath()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDefeatScreen();
        }
        ChangeState(GameState.GameOver);
    }

    public bool IsPaused => CurrentState == GameState.MapView || CurrentState == GameState.Paused || CurrentState == GameState.MainMenu || CurrentState == GameState.GameOver || CurrentState == GameState.InMenu;
}
