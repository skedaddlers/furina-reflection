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
    public InventoryUI inventoryUI;
    public CursorController cursorController;
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
        if (inventoryUI == null)
        {
            inventoryUI = GetComponent<InventoryUI>();
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
        inventoryUI.CloseInventory();
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

        if (Input.GetKeyDown(KeyCode.I))
        {
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

    private System.Collections.IEnumerator ShowNotificationCoroutine(string message, float duration)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        notificationText.gameObject.SetActive(false);
    }

}
