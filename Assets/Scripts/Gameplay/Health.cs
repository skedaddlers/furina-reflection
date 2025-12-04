using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    public float maxHealth = 100;
    private float currentHealth;

    // on death event
    public Action onDeath;
    public Action<float, float> onHealthChanged; // (current, max)

    // Shield system - returns remaining damage after absorption
    public Func<float, float> onTakeDamage;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return;

        float finalDamage = amount;

        // Let shield absorb damage first
        if (onTakeDamage != null)
        {
            finalDamage = onTakeDamage.Invoke(amount);
            Debug.Log($"Shield absorbed! Original: {amount}, After shield: {finalDamage}");
        }

        // Apply remaining damage to health
        if (finalDamage > 0)
        {
            currentHealth -= finalDamage;
            Debug.Log($"{gameObject.name} took {finalDamage} damage. Health: {currentHealth}");
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

    public void Heal(float amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"{gameObject.name} healed {amount}. Current health: {currentHealth}");
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
        if (GetComponent<PlayerStats>() != null)
        {
            onHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    void Die()
    {
        onDeath?.Invoke();
        if (CompareTag("Player"))
        {
            Debug.Log("Player Died!");
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }
}