using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public PlayerStats playerStats;

    [Header("UI Parts")]
    public StatsUI statsUI;
    public WeaponUI weaponUI;
    public SkillsUI skillsUI;
    public ShopUI shopUI;
    public CursorController cursorController;
    [Header("Door Interaction UI")]
    public CanvasGroup doorPromptCanvas;
    public TextMeshProUGUI doorPromptText; // atau pakai Text biasa kalau belum pakai TMP

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
        InitUI();
        cursorController.LockCursor();
    }

    void InitUI()
    {
        playerStats.onManaChanged += statsUI.UpdateManaUI;
        statsUI.UpdateManaUI(playerStats.CurrentMana, playerStats.MaxMana);
        playerStats.GetComponent<Health>().onHealthChanged += statsUI.UpdateHealthUI;
        statsUI.UpdateHealthUI(playerStats.health.maxHealth, playerStats.health.maxHealth);
        weaponUI.UpdateWeaponIcon(FindObjectOfType<PlayerLoadout>());
        shopUI.CloseShop();
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


    public void ShowInterractionUI(bool show, string promptText = "")
    {
        doorPromptCanvas.alpha = show ? 1 : 0;
        doorPromptCanvas.blocksRaycasts = show;
        doorPromptCanvas.interactable = show;
        doorPromptText.text = promptText;
    }

}