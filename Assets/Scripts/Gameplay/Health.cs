using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        Debug.Log($"{gameObject.name} took {amount} damage.");
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
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
