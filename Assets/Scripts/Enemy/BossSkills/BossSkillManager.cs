using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// --- BASE SKILL CLASS ---
[System.Serializable]
public abstract class BossSkill : MonoBehaviour
{
    [Header("Base Skill Settings")]
    public string skillName = "New Skill";
    public string animationTrigger = "CastSkill";
    public string windUpAnimationTrigger = "WindUp";
    public bool isEnabled = true;

    public bool causesStagger = false;
    public float staggerDuration = 1f;
    public bool causesKnockback = false;
    public float knockbackDistance = 1f;
    
    protected FocalorsPhase2AI boss;

    public virtual void Initialize(FocalorsPhase2AI bossInstance)
    {
        boss = bossInstance;
    }

    // Every skill must implement this routine
    public abstract IEnumerator ExecuteRoutine();
}

// --- SKILL MANAGER ---
public class BossSkillManager : MonoBehaviour
{
    public List<BossSkill> skills = new List<BossSkill>();
    private FocalorsPhase2AI boss;

    void Awake()
    {
        if (boss == null)
        {
            boss = GetComponent<FocalorsPhase2AI>();
        }
        if (boss != null)
        {
            Initialize(boss);
        }
        else
        {
            Debug.LogError("BossSkillManager could not find a FocalorsPhase2AI in its parents!");
        }
    }
    public void Initialize(FocalorsPhase2AI bossInstance)
    {
        boss = bossInstance;
        
        // Find all BossSkill components attached to this GameObject or its children
        if (skills.Count == 0)
        {
            List<BossSkill> foundSkills = new List<BossSkill>();
            List<BossSequence> bossSequences = bossInstance.sequences;
            if (bossSequences != null)
            {
                foreach (var sequence in bossSequences)
                {
                    foreach (var action in sequence.actions)
                    {
                        if (action.type == ActionType.Skill && action.skill != null && !foundSkills.Contains(action.skill))
                        {
                            foundSkills.Add(action.skill);
                            action.skill.Initialize(bossInstance);
                        }
                    }
                }
            }
        }
    }

    public BossSkill GetRandomAvailableSkill()
    {
        List<BossSkill> available = skills.FindAll(s => s.isEnabled);
        
        if (available.Count == 0)
        {
            Debug.LogWarning("No skills are enabled or attached to the BossSkillManager!");
            return null;
        }

        return available[Random.Range(0, available.Count)];
    }
}