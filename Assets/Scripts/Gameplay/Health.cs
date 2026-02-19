using UnityEngine;
using System;
using System.Collections;

public class Health : MonoBehaviour
{
    public float maxHealth = 100;
    public float shieldAmount = 0;
    public float maxShield = 100;
    private float currentHealth;
    public float CurrentHealth => currentHealth;
    private bool isInvulnerable = false;

    // on death event
    public Action onDeath;
    public Action<float, float> onHealthChanged; // (current, max)

    // Shield system - returns remaining damage after absorption
    public Func<float, float> onTakeDamage;

    void Start()
    {
        currentHealth = maxHealth;
        maxShield = maxHealth;
    }

    public void TakeDamage(float amount, bool isCrit = false)
    {
        if (currentHealth <= 0) return;
        if (isInvulnerable)
        {
            Debug.Log($"{gameObject.name} is invulnerable. Damage ignored.");
            return;
        }

        float finalDamage = amount;

        // Let shield absorb damage first
        if (shieldAmount > 0)
        {
            float damageAbsorbed = Mathf.Min(shieldAmount, finalDamage);
            shieldAmount -= damageAbsorbed;
            finalDamage -= damageAbsorbed;
            Debug.Log($"{gameObject.name} shield absorbed {damageAbsorbed} damage. Remaining shield: {shieldAmount}");
        }

        // Apply remaining damage to health
        if (finalDamage > 0)
        {
            currentHealth -= finalDamage;
            Debug.Log($"{gameObject.name} took {finalDamage} damage. Health: {currentHealth}");
            Enemy enemy = GetComponent<Enemy>();
            if (enemy != null)
            {
                UIManager.Instance.damageNumberUI.ShowDamagePopup(finalDamage, enemy.healthBar.position, isCrit);
                StartCoroutine(HitFlash(enemy));
            }
            else if (CompareTag("Player"))
            {
                UIManager.Instance.damageNumberUI.ShowDamagePopup(finalDamage, transform.position + Vector3.up, isCrit);
            }
        }
        else
        {
            Debug.Log($"{gameObject.name} damage fully absorbed by shield!");
        }

        if (currentHealth < 0)
            currentHealth = 0;

        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    IEnumerator HitFlash(Enemy enemy)
    {
        if (enemy.enemyRenderer == null) yield break;

        Color originalColor = enemy.rendererColor;
        enemy.enemyRenderer.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        enemy.enemyRenderer.material.color = originalColor;
        Debug.Log($"{gameObject.name} hit flash ended.");
    }

    public void AddShield(float amount)
    {
        if (amount <= 0) return;
        shieldAmount = Mathf.Min(maxShield, shieldAmount + amount);
        Debug.Log($"{gameObject.name} gained {amount} shield. Current shield: {shieldAmount}");
    }

    public void RemoveShield()
    {
        shieldAmount = 0;
        Debug.Log($"{gameObject.name} shield removed.");
    }

    public void Heal(float amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"{gameObject.name} healed {amount}. Current health: {currentHealth}");
    }

    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
    }

    public void SetMaxHealth(float newMaxHealth, bool keepCurrentRatio = true, bool fillOnIncrease = true)
    {
        newMaxHealth = Mathf.Max(1f, newMaxHealth);
        float prevMax = maxHealth;
        float prevCurrent = currentHealth;

        maxHealth = newMaxHealth;
        maxShield = newMaxHealth;

        if (keepCurrentRatio && prevMax > 0f)
        {
            float ratio = prevCurrent / prevMax;
            currentHealth = Mathf.Clamp(ratio * maxHealth, 0f, maxHealth);
        }
        else
        {
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }

        if (!keepCurrentRatio && fillOnIncrease && newMaxHealth > prevMax)
        {
            currentHealth = maxHealth;
        }

        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // public void Heal(float amount)
    // {
    //     currentHealth += amount;
    //     if (currentHealth > maxHealth)
    //         currentHealth = maxHealth;
    //     if (GetComponent<PlayerStats>() != null)
    //     {
    //         onHealthChanged?.Invoke(currentHealth, maxHealth);
    //     }
    // }

    void Die()
    {
        onDeath?.Invoke();
        if (CompareTag("Player"))
        {
            GameManager.Instance.OnPlayerDeath();
            Debug.Log("Player Died!");
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }
}
