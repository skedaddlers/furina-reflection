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
    private float _runStartTime;
    private float _finalRunDuration;
    private int _currentRunScore;
    private bool _isRunActive;

    public GameState CurrentState { get; private set; }
    public int CurrentRunScore => _currentRunScore;
    public float CurrentRunDuration => _isRunActive ? Mathf.Max(0f, Time.time - _runStartTime) : _finalRunDuration;

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
        // AudioManager.DestroyInstanceForRestart();
        GlobalDifficultyState.DestroyInstanceForRestart();
        DDAMAPEKitFramework.DDAMAPEKit.DestroyInstanceForRestart();
        HitlagManager.DestroyInstanceForRestart();
        DestroyInstanceForRestart();

        UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
    }

    public void StartGame()
    {
        BeginRun();
        ChangeState(GameState.Playing);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
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

    public void AddScore(int amount)
    {
        if (amount <= 0) return;
        _currentRunScore += amount;
    }

    public void OnBossRoomCleared()
    {
        FinalizeRun();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowVictoryScreen();
        }
        AudioManager.Instance.PlayVictoryMusic();
        ChangeState(GameState.GameOver);
    }

    public void OnPlayerDeath()
    {
        FinalizeRun();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDefeatScreen();
        }
        AudioManager.Instance.PlayDefeatMusic();
        ChangeState(GameState.GameOver);
    }

    public bool IsPaused => CurrentState == GameState.MapView || CurrentState == GameState.Paused || CurrentState == GameState.MainMenu || CurrentState == GameState.GameOver || CurrentState == GameState.InMenu;

    public string GetFormattedRunDuration()
    {
        float totalSeconds = Mathf.Max(0f, CurrentRunDuration);
        int hours = Mathf.FloorToInt(totalSeconds / 3600f);
        int minutes = Mathf.FloorToInt((totalSeconds % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);

        if (hours > 0)
        {
            return $"{hours:00}:{minutes:00}:{seconds:00}";
        }

        return $"{minutes:00}:{seconds:00}";
    }

    private void BeginRun()
    {
        _currentRunScore = 0;
        _finalRunDuration = 0f;
        _runStartTime = Time.time;
        _isRunActive = true;
    }

    private void FinalizeRun()
    {
        if (!_isRunActive) return;

        _finalRunDuration = Mathf.Max(0f, Time.time - _runStartTime);
        _isRunActive = false;
    }
}
