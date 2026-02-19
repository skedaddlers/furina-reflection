using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class LevelUpUI : MonoBehaviour
{
    public GameObject levelUpButton;

    public GameObject levelUpPanel;
    public Button healthUpgradeButton;
    public Button attackUpgradeButton;
    public Button defenseUpgradeButton;
    public Button maxManaUpgradeButton;
    public Button moveSpeedUpgradeButton;
    public Button critUpgradeButton;

    public TextMeshProUGUI upgradeAmountText;

    private int upgradeAmount = 1; 

    private PlayerStats playerStats;

    void Start()
    {
        playerStats = PlayerStats.Instance;

        healthUpgradeButton.onClick.AddListener(() => UpgradeHealth());
        attackUpgradeButton.onClick.AddListener(() => UpgradeAttack());
        defenseUpgradeButton.onClick.AddListener(() => UpgradeDefense());
        maxManaUpgradeButton.onClick.AddListener(() => UpgradeMaxMana());
        moveSpeedUpgradeButton.onClick.AddListener(() => UpgradeMoveSpeed());
        critUpgradeButton.onClick.AddListener(() => UpgradeCrit());

        levelUpButton.SetActive(false);
        levelUpPanel.SetActive(false);
    }

    public void ShowLevelUp(int upgradesToBeDone)
    {
        levelUpButton.SetActive(true);
        upgradeAmount = upgradesToBeDone;
        upgradeAmountText.text = "You can upgrade " + upgradeAmount + " more time(s)";
    }


    void Update()
    {
        if (levelUpButton.activeSelf && Input.GetKeyDown(KeyCode.L))
        {
            GameManager.Instance.cursorController.UnlockCursor();
            GameManager.Instance.ChangeState(GameState.InMenu);
            levelUpButton.SetActive(false);
            levelUpPanel.SetActive(true);
            SetupButtonTexts();
        }
    }

    private void SetupButtonTexts()
    {
        healthUpgradeButton.GetComponentInChildren<TextMeshProUGUI>().text = 
        $"Increase health by {playerStats.upgradeManager.hpGrowthPerLevel * 100}%";

        attackUpgradeButton.GetComponentInChildren<TextMeshProUGUI>().text = 
        $"Increase attack by {playerStats.upgradeManager.attackGrowthPerLevel * 100}%";

        defenseUpgradeButton.GetComponentInChildren<TextMeshProUGUI>().text =
        $"Increase defense by {playerStats.upgradeManager.defenseGrowthPerLevel * 100}%";

        maxManaUpgradeButton.GetComponentInChildren<TextMeshProUGUI>().text =
        $"Increase max mana by {playerStats.upgradeManager.maxManaGrowthPerLevel * 100}%";

        moveSpeedUpgradeButton.GetComponentInChildren<TextMeshProUGUI>().text =
        $"Increase move speed by {playerStats.upgradeManager.moveSpeedGrowthPerLevel * 100}%";

        critUpgradeButton.GetComponentInChildren<TextMeshProUGUI>().text =
        $"Increase crit rate by {playerStats.upgradeManager.critRateGrowthPerLevel * 100}% and crit damage by {playerStats.upgradeManager.critMultiplierGrowthPerLevel * 100}%";
    }

    private void UpgradeHealth()
    {
        playerStats.UpgradeHealth();
        DeductUpgradeToBeDone();
    }

    private void UpgradeAttack()
    {
        playerStats.UpgradeAttack();
        DeductUpgradeToBeDone();
    }

    private void UpgradeDefense()
    {
        playerStats.UpgradeDefense();
        DeductUpgradeToBeDone();
    }

    private void UpgradeMaxMana()
    {
        playerStats.UpgradeMaxMana();
        DeductUpgradeToBeDone();
    }

    private void UpgradeMoveSpeed()
    {
        playerStats.UpgradeMoveSpeed();
        DeductUpgradeToBeDone();
    }

    private void UpgradeCrit()
    {
        playerStats.UpgradeCrit();
        DeductUpgradeToBeDone();
    }

    private void DeductUpgradeToBeDone()
    {
        upgradeAmount--;
        upgradeAmountText.text = "You can upgrade " + upgradeAmount + " more time(s)";
        if (upgradeAmount <= 0)
        {
            GameManager.Instance.cursorController.LockCursor();
            GameManager.Instance.ChangeState(GameState.Playing);
            levelUpPanel.SetActive(false);
            playerStats.levelManager.ResetUpgradeAmount();
        }
    }
}