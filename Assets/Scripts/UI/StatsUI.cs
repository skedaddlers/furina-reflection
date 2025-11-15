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
}