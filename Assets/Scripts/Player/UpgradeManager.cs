using UnityEngine;
using System.Collections.Generic;

public class UpgradeManager : MonoBehaviour
{
    public float hpGrowthPerLevel = 0.1f; // 10% increase per level
    public float attackGrowthPerLevel = 0.1f; // 10% increase per level
    public float defenseGrowthPerLevel = 0.1f; // 10% increase per level
    public float maxManaGrowthPerLevel = 0.1f; // 10% increase per level
    public float manaRegenGrowthPerLevel = 0.1f; // 10% increase per level
    public float moveSpeedGrowthPerLevel = 0.05f; // 5%
    public float staminaGrowthPerLevel = 0.05f; // 5%
    public float critRateGrowthPerLevel = 0.025f; // 2.5% increase per level
    public float critMultiplierGrowthPerLevel = 0.05f; // 5% increase per level

    private PlayerStats playerStats;
    private int currentLevel = 1;
    private float baseHealth;
    private float baseAttack;
    private float baseDefense;
    private int baseMaxMana;
    private float baseManaRegen;
    private float baseMoveSpeed;
    private float baseStamina;
    private float baseCritRate;
    private float baseCritMultiplier;

    private void Start()
    {
        playerStats = PlayerStats.Instance;

        // Store base values
        baseHealth = playerStats.health.maxHealth;
        baseAttack = playerStats.baseAttack;
        baseDefense = playerStats.baseDefense;
        baseMaxMana = playerStats.maxMana;
        baseManaRegen = playerStats.manaRegenPerSecond;
        baseMoveSpeed = playerStats.moveSpeed;
        baseStamina = playerStats.maxStamina;
        baseCritRate = playerStats.critRate;
        baseCritMultiplier = playerStats.critMultiplier;
        baseManaRegen = playerStats.manaRegenPerSecond;
    }

    
    public float GetHealthUpgradeAmount()
    {
        float healthIncrease = baseHealth * 0.1f;
        baseHealth += healthIncrease; // Update base for next level
        return healthIncrease;
    }

    public float GetAttackUpgradeAmount()
    {
        float attackIncrease = baseAttack * 0.1f;
        baseAttack += attackIncrease; // Update base for next level
        return attackIncrease;
    }

    public float GetDefenseUpgradeAmount()
    {
        float defenseIncrease = baseDefense * 0.1f;
        baseDefense += defenseIncrease; // Update base for next level
        return defenseIncrease;
    }

    public int GetMaxManaUpgradeAmount()
    {
        int manaIncrease = Mathf.RoundToInt(baseMaxMana * 0.1f);
        baseMaxMana += manaIncrease; // Update base for next level
        return manaIncrease;
    }

    public float GetManaRegenUpgradeAmount()
    {
        float manaRegenIncrease = baseManaRegen * 0.1f;
        baseManaRegen += manaRegenIncrease; // Update base for next level
        return manaRegenIncrease;
    }

    public float GetMoveSpeedUpgradeAmount()
    {
        float moveSpeedIncrease = baseMoveSpeed * 0.1f;
        baseMoveSpeed += moveSpeedIncrease; // Update base for next level
        return moveSpeedIncrease;
    }

    public float GetStaminaUpgradeAmount()
    {
        float staminaIncrease = baseStamina * staminaGrowthPerLevel; // Scale stamina increase based on stamina growth per level
        baseStamina += staminaIncrease; // Update base for next level
        return staminaIncrease;
    }

    public float GetCritRateUpgradeAmount()
    {
        return critRateGrowthPerLevel; // Return as percentage
    }

    public float GetCritMultiplierUpgradeAmount()
    {
        return critMultiplierGrowthPerLevel; // Return as percentage
    }


}