using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatsUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI manaText;
    public Slider manaSlider;
    public TextMeshProUGUI healthText;
    public Slider healthSlider;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI xpText;
    public Slider xpSlider;


    public void UpdateManaUI(int currentMana = -1, int maxMana = -1)
    {
        manaText.text = $"Mana: {currentMana} / {maxMana}";

        manaSlider.maxValue = maxMana;
        manaSlider.value = currentMana;
    }

    public void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        healthText.text = $"{currentHealth} / {maxHealth}";

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

    public void UpdateLevelUI(int level)
    {
        levelText.text = $"Level: {level}";
    }

    public void UpdateGoldUI(int gold)
    {
        goldText.text = $"Gold: {gold}";
    }

    public void UpdateXPUI(int currentXP, int xpToNextLevel)
    {
        xpText.text = $"XP: {currentXP} / {xpToNextLevel}";

        xpSlider.maxValue = xpToNextLevel;
        xpSlider.value = currentXP;
    }
}