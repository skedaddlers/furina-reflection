using UnityEngine;

public enum GameState
{
    MainMenu,
    Playing,
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

    public bool withDialogue = true;

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
        }
    }

    void Start()
    {
        var layout = roomGenerator.Generate();
        roomManager.Initialize(layout);
        ChangeState(GameState.MainMenu);
        StartGame();
    }

    public void Restart()
    {
    }

    public void StartGame()
    {
        ChangeState(GameState.Playing);
        AudioManager.Instance.PlayMusic(AudioManager.Instance.gameplayMusic);
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

    public void OnBossRoomCleared()
    {
        UIManager.Instance.ShowVictoryScreen();
        ChangeState(GameState.GameOver);
    }

    public void OnPlayerDeath()
    {
        UIManager.Instance.ShowDefeatScreen();
        ChangeState(GameState.GameOver);
    }

    public bool IsPaused => CurrentState == GameState.Paused || CurrentState == GameState.MainMenu || CurrentState == GameState.GameOver || CurrentState == GameState.InMenu;
}