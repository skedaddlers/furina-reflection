using UnityEngine;

public class HPPotion : Item
{
    public float healAmount = 5f;
    public AudioClip useSound;

    public override bool TryUse(GameObject user)
    {
        base.TryUse(user);

        PlayerStats playerStats = user.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            if (playerStats.health.GetCurrentHealth() >= playerStats.health.maxHealth)
            {
                UIManager.Instance.ShowNotification("HP is already full!", 2f);
                return false; // Do not consume the potion if HP is full
            }
            playerStats.health.Heal(healAmount);
            UIManager.Instance.ShowNotification($"Restored {healAmount} HP!", 2f);

            // Play use sound
            if (useSound != null)
            {
                AudioManager.Instance.PlayClipAtPoint(useSound, user.transform.position);
            }
            return true;
        }
        else
        {
            Debug.LogWarning("HPPotion: No PlayerStats component found on user!");
        }
        return false;
    }
}