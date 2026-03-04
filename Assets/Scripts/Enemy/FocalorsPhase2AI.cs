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
    private bool hasDoneIntro = false;
    private bool isWaitingForClone = false;
    private Health cloneHealth;

    [Header("Telegraph & General Settings")]
    [SerializeField] private Telegraph telegraphPrefab;
    [SerializeField] private float skillCooldown = 4f; // slightly faster than Phase 1
    [SerializeField] private float castWindupTime = 0.5f;
    [SerializeField] private float waitAfterCastTime = 0.5f;

    private float lastSkillTime;
    private bool isCasting = false;
    private bool canAct = true;

    [Header("Skill: Close Collapse")]
    public bool useCloseCollapse = true;
    [SerializeField] private float closeCollapseRadius = 4f;
    [SerializeField] private int closeCollapseSegments = 30;
    [SerializeField] private float closeCollapseTelegraphTime = 1f;
    [SerializeField] private GameObject closeCollapseEffectPrefab;

    [Header("Skill: Backstep Tidal Burst")]
    public bool useBackstepTidalBurst = true;
    [SerializeField] private float backstepDistance = 6f;
    [SerializeField] private float backstepDuration = 0.3f;
    [SerializeField] private float backstepRadius = 5f;
    [SerializeField] private float backstepTelegraphTime = 1.2f;

    [Header("Skill: Hydro Dash")]
    public bool useHydroDash = true;
    [SerializeField] private float dashWidth = 3f;
    [SerializeField] private float dashLength = 12f;
    [SerializeField] private float dashTelegraphTime = 1.2f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private GameObject dashEffectPrefab;

    [Header("Skill: Tidal Spear Fan")]
    public bool useTidalSpearFan = true;
    [SerializeField] private int spearCount = 3;
    [SerializeField] private float spearRange = 10f;
    [SerializeField] private float spearAngle = 20f; // Narrow cone
    [SerializeField] private float spearSpreadAngle = 35f; // Angle between each cone
    [SerializeField] private float spearTelegraphTime = 1.2f;

    [Header("Skill: Triple Pulse Ring")]
    public bool useTriplePulseRing = true;
    [SerializeField] private float pulseInitialRadius = 3f;
    [SerializeField] private float pulseRadiusIncrement = 3f;
    [SerializeField] private float pulseDelayBetween = 0.8f;
    [SerializeField] private GameObject pulseEffectPrefab;

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
            StartCoroutine(PerformRandomSkill());
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
        if (collider != null) collider.enabled = false; // Disable collider to prevent weird

        // 1. Jump up and become immune
        SetImmune(true);
        animator.SetTrigger("JumpUp"); // Make sure you have this trigger in Animator
        
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

    bool CanUseSkill()
    {
        return Time.time - lastSkillTime >= skillCooldown;
    }

    public void NotifyCloneDeath()
    {
        StartCoroutine(JumpDownRoutine());
    }

    #endregion

    #region Phase 2 Skills

    IEnumerator PerformRandomSkill()
    {
        isCasting = true;
        StopChasing();
        LookAtPlayer();

        animator.SetTrigger("Cast");
        yield return new WaitForSeconds(castWindupTime);

        List<int> availableSkills = new List<int>();
        if (useCloseCollapse) availableSkills.Add(0);
        if (useBackstepTidalBurst) availableSkills.Add(1);
        if (useHydroDash) availableSkills.Add(2);
        if (useTidalSpearFan) availableSkills.Add(3);
        if (useTriplePulseRing) availableSkills.Add(4);

        if (availableSkills.Count > 0)
        {
            int choice = availableSkills[Random.Range(0, availableSkills.Count)];
            switch (choice)
            {
                case 0: yield return StartCoroutine(CloseCollapse()); break;
                case 1: yield return StartCoroutine(BackstepTidalBurst()); break;
                case 2: yield return StartCoroutine(HydroDash()); break;
                case 3: yield return StartCoroutine(TidalSpearFan()); break;
                case 4: yield return StartCoroutine(TriplePulseRing()); break;
            }
        }

        yield return new WaitForSeconds(waitAfterCastTime);

        lastSkillTime = Time.time;
        isCasting = false;
    }

    IEnumerator CloseCollapse()
    {
        Telegraph t = Instantiate(telegraphPrefab, transform.position, Quaternion.identity);
        t.ConfigureCircle(closeCollapseRadius, closeCollapseSegments);

        yield return new WaitForSeconds(closeCollapseTelegraphTime);

        if (closeCollapseEffectPrefab != null)
        {
            GameObject effect = Instantiate(closeCollapseEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        if (Vector3.Distance(player.position, transform.position) <= closeCollapseRadius)
        {
            DealSpecialDamage();
        }

        Destroy(t.gameObject);
    }

    IEnumerator BackstepTidalBurst()
    {
        Vector3 burstCenter = transform.position;
        
        Telegraph t = Instantiate(telegraphPrefab, burstCenter, Quaternion.identity);
        t.ConfigureCircle(backstepRadius, 30);

        // Perform backstep immediately while telegraph is charging
        Vector3 backstepTarget = transform.position - (transform.forward * backstepDistance);
        
        float time = 0;
        while (time < backstepDuration)
        {
            time += Time.deltaTime;
            transform.position = Vector3.Lerp(burstCenter, backstepTarget, time / backstepDuration);
            yield return null;
        }

        // Wait remaining telegraph time
        float remainingTime = backstepTelegraphTime - backstepDuration;
        if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);

        if (Vector3.Distance(player.position, burstCenter) <= backstepRadius)
        {
            DealSpecialDamage();
        }

        Destroy(t.gameObject);
    }

    IEnumerator HydroDash()
    {
        LookAtPlayer();
        Vector3 startPos = transform.position;
        Quaternion dashRotation = transform.rotation;

        Telegraph t = Instantiate(telegraphPrefab, startPos, dashRotation);
        t.ConfigureRectangle(dashWidth, dashLength);

        yield return new WaitForSeconds(dashTelegraphTime);

        // Check Damage
        Vector3 toPlayer = player.position - startPos;
        toPlayer.y = 0f;
        Vector3 local = Quaternion.Inverse(dashRotation) * toPlayer;
        float halfWidth = dashWidth * 0.5f;

        if (local.z >= 0f && local.z <= dashLength && Mathf.Abs(local.x) <= halfWidth)
        {
            DealSpecialDamage();
        }

        Destroy(t.gameObject);

        // Dash Forward
        Vector3 endPos = startPos + (transform.forward * dashLength);
        if (dashEffectPrefab != null)
        {
            GameObject effect = Instantiate(dashEffectPrefab, transform.position, dashRotation);
            Destroy(effect, 2f);
        }

        float time = 0;
        while (time < dashDuration)
        {
            time += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, time / dashDuration);
            yield return null;
        }
    }

    IEnumerator TidalSpearFan()
    {
        List<Telegraph> spears = new List<Telegraph>();
        
        // Calculate starting angle based on odd/even count
        float startAngle = -spearSpreadAngle * (spearCount - 1) / 2f;

        for (int i = 0; i < spearCount; i++)
        {
            float currentAngle = startAngle + (i * spearSpreadAngle);
            Quaternion rotation = transform.rotation * Quaternion.Euler(0, currentAngle, 0);
            
            Telegraph t = Instantiate(telegraphPrefab, transform.position, rotation);
            t.ConfigureCone(spearRange, spearAngle, 20);
            spears.Add(t);
        }

        yield return new WaitForSeconds(spearTelegraphTime);

        // Calculate hits
        foreach (var t in spears)
        {
            if (Vector3.Distance(player.position, transform.position) <= spearRange)
            {
                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                float angle = Vector3.Angle(t.transform.forward, dirToPlayer);

                if (angle <= spearAngle * 0.5f)
                {
                    DealSpecialDamage();
                    // We shouldn't deal damage multiple times if they overlap, so break after one hit
                    break; 
                }
            }
        }

        foreach (var t in spears)
        {
            Destroy(t.gameObject);
        }
    }

    IEnumerator TriplePulseRing()
    {

        for (int i = 0; i < 3; i++)
        {
            float currentRadius = pulseInitialRadius + (i * pulseRadiusIncrement);
            
            Vector3 pulseCenter = player.position; // Center on player at time of cast

            Telegraph t = Instantiate(telegraphPrefab, pulseCenter, Quaternion.identity);
            t.ConfigureCircle(currentRadius, 40);

            yield return new WaitForSeconds(pulseDelayBetween);

            if (pulseEffectPrefab != null)
            {
                GameObject effect = Instantiate(pulseEffectPrefab, pulseCenter, Quaternion.identity);
                // Scale effect to match radius
                effect.transform.localScale = new Vector3(currentRadius, currentRadius, currentRadius);
                Destroy(effect, 2f);
            }

            if (Vector3.Distance(player.position, pulseCenter) <= currentRadius)
            {
                DealSpecialDamage();
            }

            Destroy(t.gameObject);
        }
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