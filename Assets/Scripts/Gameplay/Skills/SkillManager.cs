using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;


public class SkillManager : MonoBehaviour
{
    [Header("Skill Inventory")]
    [SerializeField] private List<SkillSlot> ownedSkills = new List<SkillSlot>();
    [SerializeField] private int maxActiveSkills = 4; // Max equipped skills
    
    [Header("Active Skills")]
    [SerializeField] private SkillSlot[] activeSkillSlots = new SkillSlot[4];
    public SkillSlot[] ActiveSkillSlots => activeSkillSlots;
    
    [Header("References")]
    private PlayerStats playerStats;
    private PlayerCombat playerCombat;
    
    [Header("Skill Execution")]
    private Dictionary<int, Coroutine> activeSkillCoroutines = new Dictionary<int, Coroutine>();
    
    // Events
    public delegate void SkillEvent(SkillBase skill);
    public event SkillEvent OnSkillPurchased;
    public event SkillEvent OnSkillActivated;
    public event SkillEvent OnSkillEnded;
    public event SkillEvent OnSkillEquipped;
    
    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        playerCombat = GetComponent<PlayerCombat>();
        // Initialize skill slots
        // for (int i = 0; i < activeSkillSlots.Length; i++)
        // {
        //     activeSkillSlots[i] = new SkillSlot();
        // }
    }
    
    #region Skill Management
    
    // Purchase a new skill (called from shop)
    public bool PurchaseSkill(SkillBase skill)
    {
        // Check if player has enough coins
        // if (playerStats.GetCoins() < skill.purchasePrice)
        // {
        //     Debug.Log($"Not enough coins to purchase {skill.skillName}");
        //     return false;
        // }
        
        // Check level requirement
        // if (playerStats.GetLevel() < skill.levelRequired)
        // {
        //     Debug.Log($"Level {skill.levelRequired} required for {skill.skillName}");
        //     return false;
        // }
        
        // Check if already owned
        if (HasSkill(skill))
        {
            Debug.Log($"Already own skill: {skill.skillName}");
            return false;
        }
        
        // Purchase the skill
        // playerStats.SpendCoins(skill.purchasePrice);
        AddSkill(skill);
        
        OnSkillPurchased?.Invoke(skill);
        Debug.Log($"Purchased skill: {skill.skillName}");
        return true;
    }
    
    // Add skill to inventory
    public void AddSkill(SkillBase skill)
    {
        SkillSlot newSlot = new SkillSlot
        {
            skill = skill,
            currentCooldown = 0f,
            isOnCooldown = false,
            level = 1
        };
        
        ownedSkills.Add(newSlot);
        
        // Auto-equip if there's an empty active slot
        for (int i = 0; i < activeSkillSlots.Length; i++)
        {
            if (activeSkillSlots[i].skill == null)
            {
                EquipSkill(skill, i);
                break;
            }
        }
    }
    
    // Equip skill to active slot
    public void EquipSkill(SkillBase skill, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= activeSkillSlots.Length)
        {
            Debug.LogError("Invalid skill slot index");
            return;
        }
        
        SkillSlot ownedSlot = ownedSkills.FirstOrDefault(s => s.skill == skill);
        if (ownedSlot == null)
        {
            Debug.LogError("Trying to equip skill not owned");
            return;
        }
        activeSkillSlots[slotIndex] = ownedSlot;
        UIManager.Instance.skillsUI.UpdateSkillsUI(activeSkillSlots);
        OnSkillEquipped?.Invoke(skill);
    }
    
    // Check if player has a specific skill
    public bool HasSkill(SkillBase skill)
    {
        return ownedSkills.Any(s => s.skill == skill);
    }
    
    #endregion
    
    #region Skill Execution
    
    // Try to use skill in slot (called by input system)
    public bool TryUseSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= activeSkillSlots.Length)
            return false;
        
        SkillSlot slot = activeSkillSlots[slotIndex];
        if (slot == null || slot.skill == null)
        {
            Debug.Log("No skill equipped in slot " + slotIndex);
            return false;
        }
        
        // Check cooldown
        if (slot.isOnCooldown)
        {
            Debug.Log($"{slot.skill.skillName} is on cooldown: {slot.currentCooldown:F1}s remaining");
            return false;
        }
        
        // Check mana
        if (playerStats.CurrentMana < slot.skill.manaCost)
        {
            Debug.Log($"Not enough mana for {slot.skill.skillName}");
            return false;
        }
        
        // Check custom conditions
        if (!slot.skill.CanUseSkill(gameObject))
        {
            Debug.Log($"Cannot use {slot.skill.skillName} right now");
            return false;
        }
        
        // Execute the skill
        ExecuteSkill(slot, slotIndex);
        return true;
    }
    
    private void ExecuteSkill(SkillSlot slot, int slotIndex)
    {
        SkillBase skill = slot.skill;
        
        // Consume mana
        playerStats.UseMana(skill.manaCost);
        
        // Start cooldown
        slot.isOnCooldown = true;
        slot.currentCooldown = skill.cooldownTime;
        
        // Trigger animation if exists
        if (!string.IsNullOrEmpty(skill.animationTrigger))
        {
            GetComponent<Animator>()?.SetTrigger(skill.animationTrigger);
        }
        
        // Play cast sound
        if (skill.castSound != null)
        {
            AudioSource.PlayClipAtPoint(skill.castSound, transform.position);
        }
        

        // Call custom activation
        skill.OnSkillActivate(gameObject);
        
        // If skill has duration, start coroutine to end it
        if (skill.duration > 0)
        {
            if (activeSkillCoroutines.ContainsKey(slotIndex))
            {
                StopCoroutine(activeSkillCoroutines[slotIndex]);
            }
            activeSkillCoroutines[slotIndex] = StartCoroutine(SkillDurationCoroutine(skill, slotIndex));
        }
        
        OnSkillActivated?.Invoke(skill);
    }
    
    private void ApplySelfSkill(SkillBase skill)
    {
        // Apply healing
        if (skill.healAmount > 0)
        {
            // playerStats.Heal(skill.healAmount);
        }
        
        // Apply shield
        if (skill.shieldAmount > 0)
        {
            // playerStats.AddShield(skill.shieldAmount);
        }
        
        // Apply stat modifiers
        foreach (var modifier in skill.statModifiers)
        {
            // playerStats.ApplyStatModifier(modifier);
        }
    }
    
    private void ApplyAOESkill(SkillBase skill)
    {
        // Find all enemies in range
        Collider[] colliders = Physics.OverlapSphere(transform.position, 10f); // Default AOE radius
        
        foreach (var collider in colliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null && skill.damageAmount > 0)
            {
                // enemy.TakeDamage(skill.damageAmount * playerStats.GetAttackMultiplier());
            }
        }
    }
    
    private void LaunchProjectileSkill(SkillBase skill)
    {
        // This would integrate with your projectile system
        // For now, creating a placeholder
        if (skill.effectPrefab != null)
        {
            Vector3 spawnPos = transform.position + transform.forward * 1.5f + Vector3.up;
            GameObject projectile = Instantiate(skill.effectPrefab, spawnPos, transform.rotation);
            
            // Add projectile behavior
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb == null) rb = projectile.AddComponent<Rigidbody>();
            
            rb.linearVelocity = transform.forward * 20f; // Default projectile speed
            
            // Destroy after time
            Destroy(projectile, 5f);
        }
    }
    
    private void ExecuteSummonSkill(SkillBase skill)
    {
        // Summon creatures/helpers
        if (skill.effectPrefab != null)
        {
            for (int i = 0; i < 3; i++) // Default 3 summons
            {
                Vector3 spawnPos = transform.position + Random.insideUnitSphere * 3f;
                spawnPos.y = transform.position.y;
                
                GameObject summon = Instantiate(skill.effectPrefab, spawnPos, Quaternion.identity);
                
                // Destroy summons after duration
                if (skill.duration > 0)
                {
                    Destroy(summon, skill.duration);
                }
            }
        }
    }
    
    private IEnumerator SkillDurationCoroutine(SkillBase skill, int slotIndex)
    {
        yield return new WaitForSeconds(skill.duration);
        
        // End skill effects
        skill.OnSkillEnd(gameObject);
        
        // Remove stat modifiers if any
        foreach (var modifier in skill.statModifiers)
        {
            // playerStats.RemoveStatModifier(modifier);
        }
        
        OnSkillEnded?.Invoke(skill);
        activeSkillCoroutines.Remove(slotIndex);
    }
    
    #endregion
    
    #region Cooldown Management
    
    private void Update()
    {
        // Update all cooldowns
        foreach (var slot in activeSkillSlots)
        {
            if (slot != null && slot.skill != null && slot.isOnCooldown)
            {
                slot.currentCooldown -= Time.deltaTime;
                if (slot.currentCooldown <= 0)
                {
                    slot.currentCooldown = 0;
                    slot.isOnCooldown = false;
                    UIManager.Instance.skillsUI.UpdateSkillsUI(activeSkillSlots);
                }
            }
        }
    }
    
    #endregion
    
    #region Getters
    
    public SkillSlot[] GetActiveSkills()
    {
        return activeSkillSlots;
    }
    
    public List<SkillSlot> GetOwnedSkills()
    {
        return ownedSkills;
    }
    
    public float GetSkillCooldownPercent(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= activeSkillSlots.Length)
            return 0;
        
        SkillSlot slot = activeSkillSlots[slotIndex];
        if (slot == null || slot.skill == null || !slot.isOnCooldown)
            return 0;
        
        return slot.currentCooldown / slot.skill.cooldownTime;
    }
    
    #endregion
}

[System.Serializable]
public class SkillSlot
{
    public SkillBase skill;
    public float currentCooldown;
    public bool isOnCooldown;
    public int level = 1;
}