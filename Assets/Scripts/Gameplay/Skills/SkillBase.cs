using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Furina/Skills/Base Skill")]
public class SkillBase : ScriptableObject
{
    [Header("Basic Information")]
    public string skillName;
    public string upgradeName;
    public string description;
    public Sprite skillIcon;
    public Rarity rarity;
    public int price;
    public int skillID;
    [TextArea] public string goodPropertyText;
    [TextArea] public string badPropertyText;
    
    [Header("Skill Type")]
    public SkillType skillType;
    public SkillTargetType targetType;
    
    [Header("Cost & Requirements")]
    public int manaCost = 10;
    public float cooldownTime = 5f;
    public int levelRequired = 1;
    public int purchasePrice = 100; // Cost in shop
    
    [Header("Skill Effects")]
    public float duration = 0f; // 0 for instant skills
    public float damageAmount = 0f;
    public float healAmount = 0f;
    public float shieldAmount = 0f;
    
    [Header("Visual & Audio")]
    public GameObject effectPrefab;
    public AudioClip castSound;
    public AudioClip impactSound;
    
    [Header("Skill Modifiers")]
    public List<StatModifier> statModifiers = new List<StatModifier>();
    
    // Animation trigger name for this skill
    public string animationTrigger;
    
    // Can this skill be upgraded?
    public bool isUpgradeable = false;
    public bool isUpgraded = false;
    public string upgradeDescription;
    public SkillBase nextLevelSkill; // Reference to upgraded version
    
    // Virtual method for custom skill behavior
    public virtual void OnSkillActivate(GameObject caster)
    {
        // Override in derived classes for custom behavior
    }
    
    public virtual void OnSkillEnd(GameObject caster)
    {
        // Override for cleanup when duration ends
    }
    
    public virtual bool CanUseSkill(GameObject caster)
    {
        // Override for additional conditions
        return true;
    }
}

public enum SkillType
{
    Offensive,
    Defensive,
    Support,
    Summon,
    Utility
}

public enum SkillTargetType
{
    Self,
    SingleTarget,
    AOE,
    Projectile,
    GroundTarget
}

[System.Serializable]
public class StatModifier
{
    public StatType statType;
    public float value;
    public bool isPercentage;
    
    public enum StatType
    {
        Attack,
        Defense,
        Speed,
        CritRate,
        CritDamage,
        Luck
    }
}