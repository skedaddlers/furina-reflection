using UnityEngine;
using UnityEngine.UI;

public class BossHPBarUI : MonoBehaviour
{
    public Slider healthSlider;
    public Slider decayingHealthSlider;
    public GameObject bossHPBarCanvas;

    private float targetValue;
    public float decayingSpeed = 1f;

    public void Initialize(BossHealthBar bossHealthBar)
    {
        float max = bossHealthBar.Health.maxHealth;
        float current = bossHealthBar.Health.CurrentHealth;

        healthSlider.maxValue = max;
        healthSlider.value = current;

        decayingHealthSlider.maxValue = max;
        decayingHealthSlider.value = current;

        bossHPBarCanvas.SetActive(true);
    }

    public void UpdateHealth(float current, float max)
    {
        healthSlider.maxValue = max;
        healthSlider.value = current;

        targetValue = current;
    }

    void Update()
    {
        if (decayingHealthSlider.value > targetValue)
        {
            decayingHealthSlider.value =
                Mathf.MoveTowards(decayingHealthSlider.value, targetValue, Time.deltaTime * decayingSpeed);
        }
        else
        {
            decayingHealthSlider.value = targetValue;
        }
    }

    public void DisableHPBar()
    {
        bossHPBarCanvas.SetActive(false);
    }
}