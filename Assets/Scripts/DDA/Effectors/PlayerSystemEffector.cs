
using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Effector for adjusting player-related variables
/// </summary>
public class PlayerSystemEffector : Effector
{
    private PlayerStats playerStats;
    private PlayerCombat playerCombat;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
            playerCombat = player.GetComponent<PlayerCombat>();
        }
    }

    public override void Apply(string variable, float value)
    {
        if (playerStats == null) return;

        switch (variable)
        {
            case "playerDamageBonus":
                AdjustPlayerDamage(value);
                break;
            case "playerDefenseBonus":
                AdjustPlayerDefense(value);
                break;
            case "playerSpeedBonus":
                AdjustPlayerSpeed(value);
                break;
            case "playerManaRegen":
                AdjustManaRegen(value);
                break;
        }
    }

    private void AdjustPlayerDamage(float adjustment)
    {
        if (playerStats != null)
        {
            playerStats.baseAttack += adjustment;
            playerStats.baseAttack = Mathf.Max(5f, playerStats.baseAttack);
            Debug.Log($"[PlayerSystemEffector] Player damage adjusted to: {playerStats.baseAttack}");
        }
    }

    private void AdjustPlayerDefense(float adjustment)
    {
        if (playerStats != null)
        {
            playerStats.baseDefense += adjustment;
            playerStats.baseDefense = Mathf.Max(0f, playerStats.baseDefense);
            Debug.Log($"[PlayerSystemEffector] Player defense adjusted to: {playerStats.baseDefense}");
        }
    }

    private void AdjustPlayerSpeed(float adjustment)
    {
        if (playerStats != null)
        {
            playerStats.moveSpeed += adjustment * 0.5f;
            playerStats.moveSpeed = Mathf.Clamp(playerStats.moveSpeed, 3f, 12f);
            Debug.Log($"[PlayerSystemEffector] Player speed adjusted to: {playerStats.moveSpeed}");
        }
    }

    private void AdjustManaRegen(float adjustment)
    {
        if (playerStats != null)
        {
            playerStats.manaRegenPerSecond += adjustment * 0.1f;
            playerStats.manaRegenPerSecond = Mathf.Max(0.5f, playerStats.manaRegenPerSecond);
            Debug.Log($"[PlayerSystemEffector] Mana regen adjusted to: {playerStats.manaRegenPerSecond}");
        }
    }
}