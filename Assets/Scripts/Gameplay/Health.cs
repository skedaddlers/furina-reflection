using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100;
    private float currentHealth;

    // on death event
    public System.Action onDeath;

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