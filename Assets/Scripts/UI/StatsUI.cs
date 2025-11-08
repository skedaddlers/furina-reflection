using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatsUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI manaText;
    public Slider manaSlider;


    public void UpdateUI(int currentMana = -1, int maxMana = -1)
    {
        manaText.text = $"Mana: {currentMana} / {maxMana}";

        manaSlider.maxValue = maxMana;
        manaSlider.value = currentMana;
    }
}