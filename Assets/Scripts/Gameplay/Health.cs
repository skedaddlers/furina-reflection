using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    public float maxHealth = 100;
    private float currentHealth;

    // on death event
    public Action onDeath;
    public Action<float, float> onHealthChanged; // (current, max)


    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        Debug.Log($"{gameObject.name} took {amount} damage.");
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
        if (currentHealth < 0)
            currentHealth = 0;
        if (GetComponent<PlayerStats>() != null)
        {
            onHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    void Die()
    {
        onDeath?.Invoke();
        // kalau ini enemy → destroy
        // kalau ini player → trigger game over
        if (CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
        else if (CompareTag("Player"))
        {
            Debug.Log("Player Died!");
            // TODO: implement game over UI
        }
    }


}