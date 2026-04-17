using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class BossHPBarUI : MonoBehaviour
{
    public Slider healthSlider;
    public Slider decayingHealthSlider;
    public GameObject bossHPBarCanvas;
    public TextMeshProUGUI bossNameText;
    public float fillSpeed = 2f;

    public float decayingSpeed = 1f;
    private float targetValue;
    private Coroutine fillCoroutine;
    private bool isInitializing = false;

    public void Initialize(BossHealthBar bossHealthBar)
    {
        if (bossHealthBar == null || bossHealthBar.Health == null)
            return;

        float max = bossHealthBar.Health.maxHealth;
        float current = bossHealthBar.Health.CurrentHealth;

        ApplyHealthState(current, max, snapDecaying: true);
        bossNameText.text = bossHealthBar.bossName;
    }

    public void SetActive(bool isActive)
    {
        if (!isActive && fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
            isInitializing = false;
        }

        bossHPBarCanvas.SetActive(isActive);
    }

    public void InitForBossFight(BossHealthBar bossHealthBar)
    {
        if (bossHealthBar == null || bossHealthBar.Health == null)
            return;

        if(fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }

        isInitializing = true;
        bossNameText.text = bossHealthBar.bossName;
        // slowly fill the health bar from 0 to current health for dramatic effect
        healthSlider.value = 0;
        decayingHealthSlider.value = 0;
        float max = bossHealthBar.Health.maxHealth;
        float current = bossHealthBar.Health.CurrentHealth;
        ApplyMaxValue(max);
        targetValue = Mathf.Clamp(current, 0f, healthSlider.maxValue);
        SetActive(true);

        if (fillSpeed <= 0f)
        {
            isInitializing = false;
            ApplyHealthState(targetValue, max, snapDecaying: true);
            return;
        }

        fillCoroutine = StartCoroutine(FillHealthBar(fillSpeed));
    }

    private IEnumerator FillHealthBar(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float value = Mathf.Lerp(0f, Mathf.Clamp(targetValue, 0f, healthSlider.maxValue), t);
            healthSlider.value = value;
            decayingHealthSlider.value = value;

            yield return null;
        }

        isInitializing = false;
        fillCoroutine = null;
        ApplyHealthState(targetValue, healthSlider.maxValue, snapDecaying: true);
    }

    public void UpdateHealth(float current, float max)
    {
        ApplyMaxValue(max);
        targetValue = Mathf.Clamp(current, 0f, healthSlider.maxValue);

        if (isInitializing)
        {
            if (healthSlider.value > targetValue)
            {
                healthSlider.value = targetValue;
            }

            if (decayingHealthSlider.value > targetValue)
            {
                decayingHealthSlider.value = targetValue;
            }

            return;
        }

        healthSlider.value = targetValue;
        if (decayingHealthSlider.value < targetValue)
        {
            decayingHealthSlider.value = targetValue;
        }
    }

    void Update()
    {
        if (healthSlider == null || decayingHealthSlider == null)
            return;

        decayingHealthSlider.maxValue = healthSlider.maxValue;
        float actualValue = healthSlider.value;
        float desiredValue = Mathf.Max(actualValue, Mathf.Clamp(targetValue, 0f, healthSlider.maxValue));

        if (decayingHealthSlider.value > desiredValue)
        {
            decayingHealthSlider.value =
                Mathf.MoveTowards(decayingHealthSlider.value, desiredValue, Time.deltaTime * decayingSpeed);
        }
        else
        {
            decayingHealthSlider.value = desiredValue;
        }
    }

    public void DisableHPBar()
    {
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }

        isInitializing = false;
        targetValue = 0f;
        bossHPBarCanvas.SetActive(false);
    }

    private void ApplyMaxValue(float max)
    {
        float safeMax = Mathf.Max(1f, max);
        healthSlider.maxValue = safeMax;
        decayingHealthSlider.maxValue = safeMax;
    }

    private void ApplyHealthState(float current, float max, bool snapDecaying)
    {
        ApplyMaxValue(max);
        targetValue = Mathf.Clamp(current, 0f, healthSlider.maxValue);
        healthSlider.value = targetValue;

        if (snapDecaying || decayingHealthSlider.value < targetValue)
        {
            decayingHealthSlider.value = targetValue;
        }
    }
}
