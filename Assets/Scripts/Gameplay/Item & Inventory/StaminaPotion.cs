using UnityEngine;

public class StaminaPotion : Item
{
    public float staminaRestoreAmount = 5f;
    public AudioClip useSound;

    public override bool TryUse(GameObject user)
    {
        base.TryUse(user);

        PlayerStats playerStats = user.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            if(playerStats.CurrentStamina >= playerStats.maxStamina)
            {
                UIManager.Instance.ShowNotification("Stamina is already full!", 2f);
                return false; // Do not consume the potion if Stamina is full
            }
            playerStats.AddStamina(staminaRestoreAmount);
            UIManager.Instance.ShowNotification($"Restored {staminaRestoreAmount} Stamina!", 2f);
            // Play use sound
            if (useSound != null)
            {
                AudioManager.Instance.PlayClipAtPoint(useSound, user.transform.position);
            }
            return true;
        }
        else
        {
            Debug.LogWarning("StaminaPotion: No PlayerStats component found on user!");
        }
        return false;
    }
}