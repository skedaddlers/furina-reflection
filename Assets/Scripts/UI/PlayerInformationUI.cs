using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInformationUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject playerInfoPanel;
    public Button closeButton;
    public KeyCode toggleKey = KeyCode.C;

    [Header("Progress")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI expText;
    public Slider expSlider;

    [Header("Stats")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI maxManaText;
    public TextMeshProUGUI moveSpeedText;
    public TextMeshProUGUI critRateText;
    public TextMeshProUGUI critMultiplierText;
    public TextMeshProUGUI staminaText;

    [Header("Skills")]
    public List<Image> skillIcons = new List<Image>();
    public List<TextMeshProUGUI> skillNames = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> skillDescriptions = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> skillCooldowns = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> skillManaCosts = new List<TextMeshProUGUI>();

    [Header("Weapons")]
    public List<Image> weaponIcons = new List<Image>();
    public List<TextMeshProUGUI> weaponNames = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> weaponDescriptions = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> weaponPositiveEffects = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> weaponNegativeEffects = new List<TextMeshProUGUI>();

    private Player playerGO;
    private PlayerStats playerStats;
    private SkillManager skillManager;
    private PlayerLoadout playerLoadout;

    private void Start()
    {
        CacheReferences();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        UpdatePlayerInfo();
        ClosePanel();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }
    }

    private void Update()
    {
        if (playerInfoPanel == null)
        {
            return;
        }

        if (Input.GetKeyDown(toggleKey))
        {
            playerInfoPanel.SetActive(!playerInfoPanel.activeSelf);
            if (playerInfoPanel.activeSelf)
            {
                GameManager.Instance.cursorController.UnlockCursor();
                GameManager.Instance.ChangeState(GameState.InMenu);
                UpdatePlayerInfo();
            }
        }

        if (playerInfoPanel.activeSelf)
        {
            UpdatePlayerInfo();
        }
    }

    public void UpdatePlayerInfo()
    {
        CacheReferences();

        if (playerStats == null)
        {
            return;
        }

        UpdateProgressSection();
        UpdateStatsSection();
        UpdateSkillsSection();
        UpdateWeaponsSection();
    }

    private void CacheReferences()
    {
        if (playerGO == null)
        {
            playerGO = Player.Instance;
        }

        if (playerGO == null)
        {
            return;
        }

        if (playerStats == null)
        {
            playerStats = playerGO.GetComponent<PlayerStats>();
        }

        if (skillManager == null)
        {
            skillManager = playerGO.GetComponent<SkillManager>();
        }

        if (playerLoadout == null)
        {
            playerLoadout = playerGO.GetComponent<PlayerLoadout>();
        }
    }

    private void ClosePanel()
    {
        if (playerInfoPanel != null)
        {
            GameManager.Instance.cursorController.LockCursor();
            GameManager.Instance.ChangeState(GameState.Playing);
            playerInfoPanel.SetActive(false);
        }
    }

    private void UpdateProgressSection()
    {
        SetText(levelText, $"Level: {playerStats.level}");
        SetText(expText, $"EXP: {playerStats.currentXP} / {playerStats.xpToNextLevel}");

        if (expSlider != null)
        {
            expSlider.maxValue = Mathf.Max(1, playerStats.xpToNextLevel);
            expSlider.value = playerStats.currentXP;
        }
    }

    private void UpdateStatsSection()
    {
        float maxHealth = playerStats.health != null ? playerStats.health.maxHealth : 0f;
        float currentHealth = playerStats.health != null ? playerStats.health.CurrentHealth : 0f;
        SetText(healthText, $"{Mathf.RoundToInt(currentHealth)} / {Mathf.RoundToInt(maxHealth)}");
        SetText(attackText, $"{Mathf.RoundToInt(playerStats.baseAttack)}");
        SetText(defenseText, $"{Mathf.RoundToInt(playerStats.baseDefense)}");
        SetText(maxManaText, $"{playerStats.CurrentMana} / {playerStats.maxMana}");
        SetText(moveSpeedText, $"{playerStats.moveSpeed:F1}");
        SetText(critRateText, $"{(playerStats.critRate * 100f):F1}%");
        SetText(critMultiplierText, $"{(playerStats.critMultiplier * 100f):F1}%");
        SetText(staminaText, $"{Mathf.RoundToInt(playerStats.CurrentStamina)} / {playerStats.maxStamina}");
    }

    private void UpdateSkillsSection()
    {
        List<SkillSlot> ownedSkills = skillManager != null ? skillManager.GetOwnedSkills() : null;
        int uiCount = MaxCount(
            skillIcons.Count,
            skillNames.Count,
            skillDescriptions.Count,
            skillCooldowns.Count,
            skillManaCosts.Count
        );

        for (int i = 0; i < uiCount; i++)
        {
            SkillSlot skillSlot = null;
            SkillBase skill = null;
            if (ownedSkills != null && i < ownedSkills.Count && ownedSkills[i] != null)
            {
                skillSlot = ownedSkills[i];
                skill = skillSlot.skill;
            }

            if (skill != null)
            {
                SetImage(skillIcons, i, skill.skillIcon);
                SetText(skillNames, i, skill.skillName);
                SetText(skillDescriptions, i, skill.description);
                string cooldownText = skillSlot != null && skillSlot.isOnCooldown
                    ? $"{skillSlot.currentCooldown:F1}s / {skill.cooldownTime:F1}s"
                    : $"{skill.cooldownTime:F1}s";
                SetText(skillCooldowns, i, cooldownText);
                SetText(skillManaCosts, i, $"{skill.manaCost}");
            }
            else
            {
                SetImage(skillIcons, i, null);
                SetText(skillNames, i, "Empty");
                SetText(skillDescriptions, i, "-");
                SetText(skillCooldowns, i, "-");
                SetText(skillManaCosts, i, "-");
            }
        }
    }

    private void UpdateWeaponsSection()
    {
        WeaponBase[] loadoutWeapons = playerLoadout != null ? playerLoadout.loadout : null;
        int uiCount = MaxCount(
            weaponIcons.Count,
            weaponNames.Count,
            weaponDescriptions.Count,
            weaponPositiveEffects.Count,
            weaponNegativeEffects.Count
        );

        for (int i = 0; i < uiCount; i++)
        {
            WeaponBase weapon = null;
            if (loadoutWeapons != null && i < loadoutWeapons.Length)
            {
                weapon = loadoutWeapons[i];
            }

            if (weapon != null)
            {
                string weaponNameText = weapon.weaponName;
                if (playerLoadout != null && weapon == playerLoadout.current)
                {
                    weaponNameText += " (Equipped)";
                }

                SetImage(weaponIcons, i, weapon.icon);
                SetText(weaponNames, i, weaponNameText);
                SetText(weaponDescriptions, i, string.IsNullOrWhiteSpace(weapon.description) ? "-" : weapon.description);
                SetText(weaponPositiveEffects, i, string.IsNullOrWhiteSpace(weapon.goodPropertyText) ? "-" : weapon.goodPropertyText);
                SetText(weaponNegativeEffects, i, string.IsNullOrWhiteSpace(weapon.badPropertyText) ? "-" : weapon.badPropertyText);
            }
            else
            {
                SetImage(weaponIcons, i, null);
                SetText(weaponNames, i, "Empty Slot");
                SetText(weaponDescriptions, i, "-");
                SetText(weaponPositiveEffects, i, "-");
                SetText(weaponNegativeEffects, i, "-");
            }
        }
    }

    private static int MaxCount(params int[] counts)
    {
        int max = 0;
        for (int i = 0; i < counts.Length; i++)
        {
            if (counts[i] > max)
            {
                max = counts[i];
            }
        }

        return max;
    }

    private static void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null)
        {
            label.text = value;
        }
    }

    private static void SetText(List<TextMeshProUGUI> labels, int index, string value)
    {
        if (labels == null || index < 0 || index >= labels.Count || labels[index] == null)
        {
            return;
        }

        labels[index].text = value;
    }

    private static void SetImage(List<Image> images, int index, Sprite sprite)
    {
        if (images == null || index < 0 || index >= images.Count || images[index] == null)
        {
            return;
        }

        images[index].sprite = sprite;
        images[index].color = sprite == null ? new Color(1f, 1f, 1f, 0f) : Color.white;
    }
}
