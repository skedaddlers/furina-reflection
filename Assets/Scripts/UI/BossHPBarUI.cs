using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossHPBarUI : MonoBehaviour
{
    public Slider healthSlider;
    public Slider decayingHealthSlider;
    public GameObject bossHPBarCanvas;
    public float fillSpeed = 2f;

    public float decayingSpeed = 1f;
    private float targetValue;
    private Coroutine fillCoroutine;
    private bool isInitializing = false;

    public void Initialize(BossHealthBar bossHealthBar)
    {
        float max = bossHealthBar.Health.maxHealth;
        float current = bossHealthBar.Health.CurrentHealth;

        healthSlider.maxValue = max;
        healthSlider.value = current;

        decayingHealthSlider.maxValue = max;
        decayingHealthSlider.value = current;
    }

    public void SetActive(bool isActive)
    {
        bossHPBarCanvas.SetActive(isActive);
    }

    public void InitForBossFight(BossHealthBar bossHealthBar)
    {
        if(fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }

        isInitializing = true;
        // slowly fill the health bar from 0 to current health for dramatic effect
        healthSlider.value = 0;
        decayingHealthSlider.value = 0;
        float max = bossHealthBar.Health.maxHealth;
        float current = bossHealthBar.Health.CurrentHealth;
        healthSlider.maxValue = max;
        decayingHealthSlider.maxValue = max;
        targetValue = current;
        SetActive(true);
        Debug.Log($"Initializing Boss HP Bar: Max={max}, Current={current}");
        fillCoroutine = StartCoroutine(FillHealthBar(current, fillSpeed));
    }

    private IEnumerator FillHealthBar(float target, float duration)
    {
        float elapsed = 0f;
        float startValue = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float value = Mathf.Lerp(startValue, target, t);
            healthSlider.value = value;
            decayingHealthSlider.value = value;

            yield return null;
        }
        Debug.Log("Finished filling Boss HP Bar");
        isInitializing = false;
        healthSlider.value = target;
        decayingHealthSlider.value = target;
    }

    public void UpdateHealth(float current, float max)
    {
        healthSlider.maxValue = max;
        if (!isInitializing)
        {
            healthSlider.value = current;
            targetValue = current;
        }

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