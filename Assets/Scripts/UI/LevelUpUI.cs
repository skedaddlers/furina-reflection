using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

public class LevelUpUI : MonoBehaviour
{
    public GameObject levelUpButton;
    [SerializeField] private Vector3 levelUpButtonPulseScale = new Vector3(1.08f, 1.08f, 1f);
    [SerializeField] private float levelUpButtonPulseDuration = 0.45f;

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
    private Tween levelUpButtonPulseTween;
    private Vector3 levelUpButtonBaseScale;

    void Start()
    {
        playerStats = PlayerStats.Instance;

        healthUpgradeButton.onClick.AddListener(() => UpgradeHealth());
        attackUpgradeButton.onClick.AddListener(() => UpgradeAttack());
        defenseUpgradeButton.onClick.AddListener(() => UpgradeDefense());
        maxManaUpgradeButton.onClick.AddListener(() => UpgradeMaxMana());
        moveSpeedUpgradeButton.onClick.AddListener(() => UpgradeMoveSpeed());
        critUpgradeButton.onClick.AddListener(() => UpgradeCrit());

        if (levelUpButton != null)
        {
            levelUpButtonBaseScale = levelUpButton.transform.localScale;
            levelUpButton.transform.StopPulse(levelUpButtonBaseScale);
        }

        levelUpButton.SetActive(false);
        levelUpPanel.SetActive(false);
    }

    public void ShowLevelUp(int upgradesToBeDone)
    {
        levelUpButton.SetActive(true);
        StartLevelUpButtonPulse();
        upgradeAmount = upgradesToBeDone;
        upgradeAmountText.text = "You can upgrade " + upgradeAmount + " more time(s)";
    }


    void Update()
    {
        if (levelUpButton.activeSelf && Input.GetKeyDown(KeyCode.L))
        {
            GameManager.Instance.cursorController.UnlockCursor();
            GameManager.Instance.ChangeState(GameState.InMenu);
            StopLevelUpButtonPulse();
            levelUpButton.SetActive(false);
            levelUpPanel.SetActive(true);
            SetupButtonTexts();
        }
    }

    private void OnDisable()
    {
        StopLevelUpButtonPulse();
    }

    private void OnDestroy()
    {
        StopLevelUpButtonPulse();
    }

    private void StartLevelUpButtonPulse()
    {
        if (levelUpButton == null)
        {
            return;
        }

        if (levelUpButtonPulseTween != null && levelUpButtonPulseTween.IsActive())
        {
            return;
        }

        levelUpButtonPulseTween = levelUpButton.transform.PulseLoop(
            levelUpButtonBaseScale,
            levelUpButtonPulseScale,
            levelUpButtonPulseDuration
        );
    }

    private void StopLevelUpButtonPulse()
    {
        if (levelUpButton == null)
        {
            return;
        }

        levelUpButtonPulseTween = null;
        levelUpButton.transform.StopPulse(levelUpButtonBaseScale);
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
