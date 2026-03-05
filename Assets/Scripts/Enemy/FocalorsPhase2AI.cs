using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class FocalorsPhase2AI : EnemyAI
{
    [Header("Phase 2 Intro Mechanics")]
    public GameObject clonePrefab;
    public float jumpHeight = 5f;
    public float jumpDuration = 1f;
    public Transform cloneSpawnPoint; // Optional: where the clone appears
    public System.Action onCloneDies; // Event to notify BossManager that clone has died

    [Header("Phase 2 Outro Mechanics")]
    public GameObject phase2DeathEffectPrefab;
    public System.Action onPhase2Death; // Event to notify BossManager that phase 2 death animation has finished

    private bool hasDoneIntro = false;
    private bool isWaitingForClone = false;
    private Health cloneHealth;

    [Header("Core Skill Dependencies")]
    public Telegraph telegraphPrefab; 
    [SerializeField] private float skillCooldown = 4f; 
    [SerializeField] private float castWindupTime = 0.5f;
    [SerializeField] private float waitAfterCastTime = 0.5f;

    private float lastSkillTime;
    private bool isCasting = false;
    private bool canAct = true;

    private BossSkillManager skillManager;

    // Expose protected EnemyAI variables to the skills
    public Transform TargetPlayer => player;

    void Start()
    {
        base.Start();
        
        // Initialize the Skill Manager
        skillManager = GetComponent<BossSkillManager>();
        if (skillManager != null)
        {
            skillManager.Initialize(this);
        }
        else
        {
            Debug.LogError("BossSkillManager missing from FocalorsPhase2AI or its children!");
        }
    }

    protected override void Update()
    {
        // 1. Check if we need to do the Phase 2 intro (jump & clone)
        if (canAct && !hasDoneIntro)
        {
            StartCoroutine(Phase2IntroRoutine());
            return;
        }

        // Wait until clone is dead and intro is totally finished
        if (player == null || isCasting || !canAct || isWaitingForClone) return;

        float distance = Vector3.Distance(player.position, transform.position);

        if (!SeePlayer())
        {
            StopChasing();
            return;
        }

        if (CanUseSkill())
        {
            StartCoroutine(PerformSkillSequence());
            return;
        }

        if (distance <= attackRange)
        {
            StopChasing();
            LookAtPlayer();
            AttackPlayer();
        }
        else
        {
            ChasePlayer();
        }
    }

    #region Intro & Clone Mechanics

    private IEnumerator Phase2IntroRoutine()
    {
        hasDoneIntro = true;
        isWaitingForClone = true;
        StopChasing();

        var agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false; // Disable NavMeshAgent during intro

        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false; // Disable collider to prevent weird behavior

        // 1. Jump up and become immune
        SetImmune(true);
        animator.SetTrigger("JumpUp"); // Make sure you have this trigger in Animator
        
        // wait for the jump animation to reach the point where she should be at the peak (you can use an Animation Event for this, or just wait a fixed time)
        yield return new WaitForSeconds(jumpDuration * 0.5f); // Assuming the peak is at half the jump duration, adjust as needed
        Vector3 startPos = transform.position;
        Vector3 peakPos = startPos + Vector3.up * jumpHeight;
        
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / jumpDuration;
            transform.position = Vector3.Lerp(startPos, peakPos, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        // 2. Summon Clone
        Vector3 spawnPos = cloneSpawnPoint != null ? cloneSpawnPoint.position : startPos;
        GameObject clone = Instantiate(clonePrefab, spawnPos, transform.rotation);
        cloneHealth = clone.GetComponent<Health>();
        
        if (cloneHealth != null)
        {
            cloneHealth.onDeath += OnCloneDied;
        }
        else
        {
            Debug.LogWarning("Clone prefab is missing a Health component! Boss will be stuck.");
        }
        
        // Wait here until the event fires
    }

    private void OnCloneDied()
    {
        if (cloneHealth != null) cloneHealth.onDeath -= OnCloneDied;
        onCloneDies?.Invoke(); 
    }

    private IEnumerator JumpDownRoutine()
    {
        
        animator.SetTrigger("JumpDown");
        Vector3 startPos = transform.position;
        // Raycast down to find ground, or just subtract jumpHeight
        Vector3 groundPos = startPos - Vector3.up * jumpHeight; 

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / (jumpDuration * 0.5f); // Fall faster than rising
            transform.position = Vector3.Lerp(startPos, groundPos, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        var agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = true; // Re-enable NavMeshAgent

        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = true; // Re-enable collider

        SetImmune(false);
        isWaitingForClone = false;
        lastSkillTime = Time.time; // Reset skill timer so she doesn't instantly cast
    }

    #endregion

    #region General Control Flow

    public void SetCanAct(bool value)
    {
        canAct = value;
        if (!value)
        {
            StopChasing();
            isCasting = false;
            animator.ResetTrigger("Cast");
        }
    }

    public void SetImmune(bool value)
    {
        var health = GetComponent<Health>();
        if (health != null) health.SetImmune(value);
    }

    public void NotifyCloneDeath()
    {
        StartCoroutine(JumpDownRoutine());
    }

    public void DeathAndTransform()
    {
        // Play death animation, disable boss, etc.
        SetCanAct(false);
        animator.SetTrigger("Die");
    }

    public void OnDeathAnimationEvent() // Call this from an animation event at the end of the death animation
    {
        if (phase2DeathEffectPrefab != null)
        {
            GameObject effect = Instantiate(phase2DeathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f); // Destroy the effect after 2 seconds
        }

        onPhase2Death?.Invoke();
    }


    #endregion

    #region Skill Execution

    bool CanUseSkill()
    {
        return Time.time - lastSkillTime >= skillCooldown;
    }

    IEnumerator PerformSkillSequence()
    {
        BossSkill chosenSkill = skillManager.GetRandomAvailableSkill();
        if (chosenSkill == null) yield break;

        isCasting = true;
        StopChasing();
        LookAtPlayer();

        // Let the boss wind up
        animator.SetTrigger(chosenSkill.windUpAnimationTrigger);
        yield return new WaitForSeconds(castWindupTime);

        // Execute the specific skill's logic
        yield return StartCoroutine(chosenSkill.ExecuteRoutine());

        // Wait a moment after finishing
        yield return new WaitForSeconds(waitAfterCastTime);

        lastSkillTime = Time.time;
        isCasting = false;
    }

    #endregion

    public void DealSpecialDamage()
    {
        var health = player.GetComponent<Health>();
        if (health == null) return;

        var playerStats = player.GetComponent<PlayerStats>();
        float defense = playerStats != null ? playerStats.baseDefense : 0f;
        float critChance = enemyStats != null ? enemyStats.critRate : 0f;
        float critMultiplier = enemyStats != null ? enemyStats.critMultiplier : 1f;

        int levelDiff = 0;
        if (enemyStats != null && playerStats != null)
            levelDiff = enemyStats.level - playerStats.level;

        bool didCrit;
        float finalDamage = Helpers.CalculateFinalDamage(
            damage,
            defense,
            critChance,
            critMultiplier,
            levelDiff,
            1f,
            out didCrit
        );

        health.TakeDamage(finalDamage, didCrit);
    }
}