using UnityEngine;
using TMPro;
using UnityEngine.UI;

public enum UINavigationLevel
{
    Playing,
    Menu,
    Settings
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public PlayerStats playerStats;

    [Header("UI Parts")]
    public StatsUI statsUI;
    public WeaponUI weaponUI;
    public SkillsUI skillsUI;
    public ShopUI shopUI;
    public EventRoomUI eventRoomUI;
    public InventoryUI inventoryUI;
    public DamageNumberUI damageNumberUI;
    public CursorController cursorController;
    public LevelUpUI levelUpUI;
    public BossHPBarUI bossHPBarUI;
    public DialogueUI dialogueUI;

    [Header("Interaction UI")]
    public CanvasGroup doorPromptCanvas;
    public TextMeshProUGUI promptText; // atau pakai Text biasa kalau belum pakai TMP
    public TextMeshProUGUI notificationText;
    [Header("Victory/Defeat UI")]
    public Button restartButton;
    public string mainSceneName = "MainScene";
    public CanvasGroup victoryCanvas;
    public CanvasGroup defeatCanvas;
    public GameObject winLoseScreen;
    public Button mainMenuButton;
    public TextMeshProUGUI victoryScoreText;
    public TextMeshProUGUI victoryTimeText;
    public TextMeshProUGUI defeatScoreText;
    public TextMeshProUGUI defeatTimeText;

    private MonoBehaviour activeMenuOwner;
    private GameState activeMenuState = GameState.InMenu;
    private MonoBehaviour activeSettingsOwner;

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

        ShowInterractionUI(false, "");        
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

    void Start()
    {
        if (cursorController == null)
            cursorController = GetComponent<CursorController>();
        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
        }
        if (statsUI == null)
        {
            statsUI = GetComponent<StatsUI>();
        }
        if (weaponUI == null)
        {
            weaponUI = GetComponent<WeaponUI>();
        }
        if (skillsUI == null)
        {
            skillsUI = GetComponent<SkillsUI>();
        }
        if (shopUI == null)
        {
            shopUI = GetComponent<ShopUI>();
        }
        if (eventRoomUI == null)
        {
            eventRoomUI = GetComponent<EventRoomUI>();
        }
        if (inventoryUI == null)
        {
            inventoryUI = GetComponent<InventoryUI>();
        }
        if (damageNumberUI == null)
        {
            damageNumberUI = GetComponent<DamageNumberUI>();
        }
        if (levelUpUI == null)
        {
            levelUpUI = GetComponent<LevelUpUI>();
        }
        if (bossHPBarUI == null)
        {
            bossHPBarUI = GetComponent<BossHPBarUI>();
        }
        if (dialogueUI == null)
        {
            dialogueUI = GetComponent<DialogueUI>();
        }
        mainMenuButton.onClick.AddListener(() => SceneLoader.Instance.LoadScene(GameManager.Instance.mainMenuSceneName));
        InitUI();
        cursorController.LockCursor();
    }

    void InitUI()
    {
        playerStats.onManaChanged += statsUI.UpdateManaUI;
        statsUI.UpdateManaUI(playerStats.CurrentMana, playerStats.MaxMana);
        playerStats.onStaminaChanged += statsUI.UpdateStaminaUI;
        statsUI.UpdateStaminaUI(playerStats.CurrentStamina, playerStats.maxStamina);
        playerStats.GetComponent<Health>().onHealthChanged += statsUI.UpdateHealthUI;
        statsUI.UpdateHealthUI(playerStats.health.maxHealth, playerStats.health.maxHealth);
        weaponUI.UpdateWeaponIcon(FindObjectOfType<PlayerLoadout>());
        statsUI.UpdateLevelUI(playerStats.level);
        statsUI.UpdateGoldUI(playerStats.gold);
        statsUI.UpdateXPUI(playerStats.currentXP, playerStats.xpToNextLevel);
        shopUI.CloseShop();
        inventoryUI.CloseInventory();
        HideEndScreens();
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.Restart();
                }
            });
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (Player.Instance == null) return;
            Inventory inventory = Player.Instance.GetComponent<Inventory>();
            if (inventory != null)
            {
                if (inventoryUI.inventoryPanel.activeSelf)
                {
                    inventoryUI.CloseInventory();
                }
                else
                {
                    inventoryUI.OpenInventoryUI(inventory.Items);
                }
            }
        }
    }
    
    public static void UpdateWeaponIcon(PlayerLoadout loadout)
    {
        if (Instance != null && Instance.weaponUI != null)
        {
            Instance.weaponUI.UpdateWeaponIcon(loadout);
        }
    }

    public void ShowVictoryScreen()
    {
        if (cursorController != null)
        {
            cursorController.UnlockCursor();
        }

        UpdateEndScreenSummary(victoryScoreText, victoryTimeText);
        if (winLoseScreen != null)
        {
            winLoseScreen.SetActive(true);
        }

        SetCanvasGroupVisible(defeatCanvas, false);
        SetCanvasGroupVisible(victoryCanvas, true);
    }

    public void ShowDefeatScreen()
    {
        if (cursorController != null)
        {
            cursorController.UnlockCursor();
        }

        UpdateEndScreenSummary(defeatScoreText, defeatTimeText);
        if (winLoseScreen != null)
        {
            winLoseScreen.SetActive(true);
        }

        SetCanvasGroupVisible(victoryCanvas, false);
        SetCanvasGroupVisible(defeatCanvas, true);
    }


    public void ShowInterractionUI(bool show, string text = "")
    {
        doorPromptCanvas.alpha = show ? 1 : 0;
        doorPromptCanvas.blocksRaycasts = show;
        doorPromptCanvas.interactable = show;
        promptText.text = text;
    }

    public void ShowNotification(string message, float duration = 2f)
    {
        StartCoroutine(ShowNotificationCoroutine(message, duration));
    }

    public bool TryOpenMenu(MonoBehaviour owner, GameState menuState = GameState.InMenu)
    {
        if (owner == null)
        {
            return false;
        }

        if (GameManager.Instance != null
            && (GameManager.Instance.CurrentState == GameState.MainMenu
            || GameManager.Instance.CurrentState == GameState.GameOver))
        {
            return false;
        }

        if (activeSettingsOwner != null && activeSettingsOwner != owner)
        {
            return false;
        }

        if (activeMenuOwner != null && activeMenuOwner != owner)
        {
            return false;
        }

        activeMenuOwner = owner;
        activeMenuState = menuState;
        ApplyNavigationState();
        return true;
    }

    public void CloseMenu(MonoBehaviour owner)
    {
        if (owner != null && activeMenuOwner == owner)
        {
            activeMenuOwner = null;
            activeMenuState = GameState.InMenu;
        }

        ApplyNavigationState();
    }

    public bool TryOpenSettings(MonoBehaviour owner)
    {
        if (owner == null)
        {
            return false;
        }

        if (GameManager.Instance != null
            && (GameManager.Instance.CurrentState == GameState.MainMenu
            || GameManager.Instance.CurrentState == GameState.GameOver))
        {
            return false;
        }

        if (activeMenuOwner != null && activeMenuOwner != owner)
        {
            return false;
        }

        if (activeSettingsOwner != null && activeSettingsOwner != owner)
        {
            return false;
        }

        activeSettingsOwner = owner;
        ApplyNavigationState();
        return true;
    }

    public void CloseSettings(MonoBehaviour owner)
    {
        if (owner != null && activeSettingsOwner == owner)
        {
            activeSettingsOwner = null;
        }

        ApplyNavigationState();
    }

    public bool HasActiveMenu(MonoBehaviour requester = null)
    {
        return activeMenuOwner != null && activeMenuOwner != requester;
    }

    public bool HasActiveSettings(MonoBehaviour requester = null)
    {
        return activeSettingsOwner != null && activeSettingsOwner != requester;
    }

    public UINavigationLevel GetNavigationLevel()
    {
        if (activeSettingsOwner != null)
        {
            return UINavigationLevel.Settings;
        }

        if (activeMenuOwner != null)
        {
            return UINavigationLevel.Menu;
        }

        return UINavigationLevel.Playing;
    }

    public bool IsAnyUIOpen()
    {
        if (activeMenuOwner != null || activeSettingsOwner != null)
        {
            return true;
        }

        if (GameManager.Instance == null)
        {
            return false;
        }

        return GameManager.Instance.CurrentState == GameState.MainMenu
            || GameManager.Instance.CurrentState == GameState.GameOver;
    }

    private System.Collections.IEnumerator ShowNotificationCoroutine(string message, float duration)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        notificationText.gameObject.SetActive(false);
    }

    private void ApplyNavigationState()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.CurrentState == GameState.MainMenu
            || GameManager.Instance.CurrentState == GameState.GameOver)
        {
            return;
        }

        if (activeSettingsOwner != null)
        {
            GameManager.Instance.SetCursorState(true);
            GameManager.Instance.ChangeState(GameState.Paused);
            return;
        }

        if (activeMenuOwner != null)
        {
            GameManager.Instance.SetCursorState(true);
            GameManager.Instance.ChangeState(activeMenuState);
            return;
        }

        GameManager.Instance.SetCursorState(false);
        GameManager.Instance.ChangeState(GameState.Playing);
    }

    private void UpdateEndScreenSummary(TextMeshProUGUI scoreText, TextMeshProUGUI timeText)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (scoreText != null)
        {
            scoreText.text = $"Score: {GameManager.Instance.CurrentRunScore}";
        }

        if (timeText != null)
        {
            timeText.text = $"Time: {GameManager.Instance.GetFormattedRunDuration()}";
        }
    }

    private void HideEndScreens()
    {
        if (winLoseScreen != null)
        {
            winLoseScreen.SetActive(false);
        }

        SetCanvasGroupVisible(victoryCanvas, false);
        SetCanvasGroupVisible(defeatCanvas, false);
    }

    private void SetCanvasGroupVisible(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.gameObject.SetActive(visible);
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }

}
