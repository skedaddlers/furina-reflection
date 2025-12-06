using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    public float maxHealth = 100;
    public float shieldAmount = 0;
    public float maxShield = 100;
    private float currentHealth;

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

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return;

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
            Debug.Log("Player Died!");
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }
}