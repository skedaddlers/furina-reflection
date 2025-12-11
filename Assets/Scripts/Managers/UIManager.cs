using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    public CursorController cursorController;
    [Header("Door Interaction UI")]
    public CanvasGroup doorPromptCanvas;
    public TextMeshProUGUI doorPromptText; // atau pakai Text biasa kalau belum pakai TMP
    [Header("Victory/Defeat UI")]
    public Button restartButton;
    public string mainSceneName = "MainScene";
    public CanvasGroup victoryCanvas;
    public CanvasGroup defeatCanvas;
    public GameObject winLoseScreen;

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

        ShowInterractionUI(false, "");        
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
        restartButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(mainSceneName);
            GameManager.Instance.Restart();
        });
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (cursorController != null)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                    cursorController.UnlockCursor();
                else
                    cursorController.LockCursor();
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
        cursorController.UnlockCursor();
        winLoseScreen.SetActive(true);
        victoryCanvas.gameObject.SetActive(true);
        victoryCanvas.alpha = 1;
        victoryCanvas.blocksRaycasts = true;
        victoryCanvas.interactable = true;
    }

    public void ShowDefeatScreen()
    {
        cursorController.UnlockCursor();
        winLoseScreen.SetActive(true);
        defeatCanvas.gameObject.SetActive(true);
        defeatCanvas.alpha = 1;
        defeatCanvas.blocksRaycasts = true;
        defeatCanvas.interactable = true;
    }


    public void ShowInterractionUI(bool show, string promptText = "")
    {
        doorPromptCanvas.alpha = show ? 1 : 0;
        doorPromptCanvas.blocksRaycasts = show;
        doorPromptCanvas.interactable = show;
        doorPromptText.text = promptText;
    }

}
