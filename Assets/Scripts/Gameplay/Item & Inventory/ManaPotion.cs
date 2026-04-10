using UnityEngine;

public class ManaPotion : Item
{
    public int manaRestoreAmount = 10;
    public AudioClip useSound;

    public override bool TryUse(GameObject user)
    {
        base.TryUse(user);

        PlayerStats playerStats = user.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            if(playerStats.CurrentMana >= playerStats.MaxMana)
            {
                UIManager.Instance.ShowNotification("Mana is already full!", 2f);
                return false; // Do not consume the potion if Mana is full
            }
            playerStats.AddMana(manaRestoreAmount);
            UIManager.Instance.ShowNotification($"Restored {manaRestoreAmount} Mana!", 2f);

            // Play use sound
            if (useSound != null)
            {
                AudioManager.Instance.PlayClipAtPoint(useSound, user.transform.position);
            }
            return true;
        }
        else
        {
            Debug.LogWarning("ManaPotion: No PlayerStats component found on user!");
        }
        return false;
    }
}