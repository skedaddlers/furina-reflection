using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using DDAMAPEKitFramework;

public class FocalorsPhase2AI : EnemyAI
{
    [Header("Phase 2 Intro Mechanics")]
    public bool useCloneMechanic = true;
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
    public List<Transform> arenaPoints; // Predefined points in the arena for repositioning
    public List<BossSequence> sequences; // List of possible skill sequences to choose from
    public Telegraph telegraphPrefab; 
    [SerializeField] private float sequenceCooldown = 4f; 
    [SerializeField] private float castWindupTime = 0.5f;
    [SerializeField] private float waitAfterCastTime = 0.5f;

    [Header("SFX & VFX")]
    public AudioClip cloneSpawnSound;
    public GameObject cloneSpawnEffectPrefab;

    private float lastSequenceTime;
    private Coroutine currentActionRoutine;
    private bool isCasting = false;
    private bool canAct = true;
    private bool isDyingOrDead = false;
    private int strafeDirection = 1;
    private int configuredEnemyLevel = -1;

    private BossSkillManager skillManager;

    // Expose protected EnemyAI variables to the skills
    public Transform TargetPlayer => player;

    void Start()
    {
        base.Start();
        List<BossSequence> librarySequences = Library.Instance.bossSequences;
        if (librarySequences != null && librarySequences.Count > 0)
        {
            sequences = new List<BossSequence>();

            foreach (var seq in librarySequences)
            {
                BossSequence clone = Instantiate(seq); // 🔥 penting
                clone.usageCount = 0;
                clone.lastUsedTime = -999f;
                sequences.Add(clone);
            }
        }
        if (arenaPoints == null || arenaPoints.Count == 0)
        {
            GetComponentInParent<Room>()?.arenaPoints?.ForEach(p => arenaPoints.Add(p));
        }
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
        if (isDyingOrDead) return;
        if (IsStaggered) return;

        // 1. Check if we need to do the Phase 2 intro (jump & clone)
        if (canAct && !hasDoneIntro)
        {
            if(useCloneMechanic)
            {
                StartCoroutine(Phase2IntroRoutine());
            }
            else
            {
                hasDoneIntro = true;
            }
            return;
        }

        // Wait until clone is dead and intro is totally finished
        if (player == null || isCasting || !canAct || isWaitingForClone) return;

        float distance = Vector3.Distance(player.position, transform.position);

        if (CanUseSkill())
        {
            BossSequence chosenSequence = ChooseSequence();
            if (chosenSequence != null)
                currentActionRoutine = StartCoroutine(ExecuteSequence(chosenSequence));
            return;
        }

        // if (distance <= attackRange)
        // {
        //     StopChasing();
        //     LookAtPlayer();
        //     AttackPlayer();
        // }
        // else
        // {
        //     ChasePlayer();
        // }
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
        if (cloneSpawnSound != null)
        {
            AudioManager.Instance?.PlaySFXNoOverlap(cloneSpawnSound);
        }
        if (cloneSpawnEffectPrefab != null)
        {
            GameObject effect = Instantiate(cloneSpawnEffectPrefab, spawnPos, transform.rotation);
            Destroy(effect, 2f);
        }
        ApplyConfiguredEnemyLevel(clone);
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
        if (agent != null)
        {
            agent.enabled = true; // Re-enable NavMeshAgent
            agent.isStopped = false; // Ensure movement can resume after intro
        }

        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = true; // Re-enable collider

        SetImmune(false);
        isWaitingForClone = false;
        lastSequenceTime = Time.time; // Reset sequence timer so she doesn't instantly cast
    }

    #endregion

    #region General Control Flow

    public void SetCanAct(bool value)
    {
        if (isDyingOrDead && value) return;
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

    public void SetEnemyLevel(int level)
    {
        configuredEnemyLevel = Mathf.Max(1, level);
        ApplyConfiguredEnemyLevel(gameObject);
    }

    public void NotifyCloneDeath()
    {
        if (isDyingOrDead) return;
        StartCoroutine(JumpDownRoutine());
    }

    public void DeathAndTransform()
    {
        isDyingOrDead = true;
        ForceCancelAllActions();
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
        return Time.time - lastSequenceTime >= ScaleAbilityCooldown(sequenceCooldown) && currentActionRoutine == null;
    }

    BossSequence ChooseSequence()
    {
        if (sequences == null || sequences.Count == 0)
            return null;

        float totalWeight = 0;
        Dictionary<string, float> profileWeights = GetProfileDistributionByName();
        bool hasProfileDistribution = profileWeights.Count > 0;

        if (!hasProfileDistribution)
            return sequences[Random.Range(0, sequences.Count)];

        List<float> adjustedWeights = new();

        foreach(var seq in sequences)
        {
            float profileWeight = GetSequenceProfileWeight(seq.profileName, profileWeights, hasProfileDistribution);
            float recencyPenalty = Mathf.Clamp01(
                (Time.time - seq.lastUsedTime) / 5f
            );
            if (seq.usageCount == 0)
            {
                recencyPenalty = 1f;
            }

            float usagePenalty = 1f / (1f + seq.usageCount * 0.5f);

            float adjusted = seq.baseWeight * profileWeight * recencyPenalty * usagePenalty;

            adjustedWeights.Add(adjusted);
            totalWeight += adjusted;
            // Debug.Log($"Sequence {seq.name}: base={seq.baseWeight}, profile={profileWeight}, recency={recencyPenalty}, usage={usagePenalty}, adjusted={adjusted}");
        }

        if (totalWeight <= 0f)
        {
            return sequences[Random.Range(0, sequences.Count)];
        }

        float r = Random.value * totalWeight;

        float sum = 0;

        for(int i=0;i<sequences.Count;i++)
        {
            sum += adjustedWeights[i];

            if(r <= sum)
                return sequences[i];
        }

        return sequences[sequences.Count-1];
    }

    private Dictionary<string, float> GetProfileDistributionByName()
    {
        Dictionary<string, float> weights = new();

        var playerModel = DDARuntimeHelper.TryGetActivePlayerModel();
        if (playerModel == null)
            return weights;

        var distribution = playerModel.GetProfileDistribution();
        foreach (var kv in distribution)
        {
            if (kv.Key == null || string.IsNullOrWhiteSpace(kv.Key.name))
                continue;

            string key = kv.Key.name.Trim().ToLowerInvariant();
            weights[key] = Mathf.Max(0f, kv.Value);
        }

        return weights;
    }

    private float GetSequenceProfileWeight(string sequenceProfileName, Dictionary<string, float> profileWeights, bool hasProfileDistribution)
    {
        if (!hasProfileDistribution)
            return 1f;

        // Empty profile name means the sequence is generic and should always stay available.
        if (string.IsNullOrWhiteSpace(sequenceProfileName))
            return 1f;

        string key = sequenceProfileName.Trim().ToLowerInvariant();
        if (profileWeights.TryGetValue(key, out float weight))
            return Mathf.Max(0f, weight);

        // If a sequence has a profile tag that is not currently present in the model, keep a small chance.
        return 0.05f;
    }

    IEnumerator ExecuteSequence(BossSequence seq)
    {
        if (seq == null)
            yield break;

        foreach(var action in seq.actions)
        {
            if(action.type == ActionType.Movement)
            {
                Debug.Log($"Executing Movement: {action.movement.movementType}");
                yield return ExecuteMovement(action.movement);
            }
            else
            {
                Debug.Log($"Executing Skill: {action.skill.name}");
                yield return ExecuteSkill(action.skill);
            }
        }
        ResetActionRoutine();
        lastSequenceTime = Time.time;
        seq.lastUsedTime = Time.time;
        LookAtPlayer();
        seq.usageCount++;
    }

    IEnumerator ExecuteSkill(BossSkill chosenSkill)
    {
        if (chosenSkill == null) yield break;
        if (isCasting) yield break;
        isCasting = true;
        StopChasing();
        LookAtPlayer();

        // Let the boss wind up
        animator.SetTrigger(chosenSkill.windUpAnimationTrigger);
        yield return new WaitForSeconds(castWindupTime);

        // Execute the specific skill's logic
        yield return chosenSkill.ExecuteRoutine();

        // Wait a moment after finishing
        yield return new WaitForSeconds(waitAfterCastTime);

        isCasting = false;
    }


    IEnumerator ExecuteMovement(MovementAction m)
    {
        if (agent == null || !agent.enabled) yield break;
        float angularSpeedBackup = agent.angularSpeed;
        bool updateRotationBackup = agent.updateRotation;
        // Skill execution calls StopChasing(), so movement actions must explicitly resume the agent.
        agent.isStopped = false;
        bool shouldWarpAfterMovement = false;

        try
        {
            switch(m.movementType)
            {
                case MovementType.DashToPlayer:
                    yield return DashToPlayer(m);
                    break;

                case MovementType.StrafePlayer:
                    yield return StrafeAroundPlayer(m);
                    break;

                case MovementType.Retreat:
                    LookAtPlayer();
                    agent.updateRotation = false; // Prevent NavMeshAgent from rotating the boss, we'll handle it manually
                    agent.angularSpeed = 0f; // Stop automatic rotation
                    shouldWarpAfterMovement = true;
                    yield return RetreatFromPlayer(m);
                    break;

                case MovementType.Reposition:
                    yield return Reposition(m);
                    break;

                default:
                    yield return new WaitForSeconds(m.duration);
                    break;
            }
        }
        finally
        {
            animator.SetFloat("WalkSpeed", 0f);
            animator.SetBool("Retreat", false);
            RestoreDefaultAgentSpeed();

            if (agent != null)
            {
                agent.angularSpeed = angularSpeedBackup;
                agent.updateRotation = updateRotationBackup;

                if (agent.enabled)
                {
                    agent.ResetPath();

                    if (shouldWarpAfterMovement)
                        agent.Warp(transform.position);
                }
            }
        }
    }
    

    private IEnumerator DashToPlayer(MovementAction movement)
    {
        if (agent == null || !agent.enabled || player == null) yield break;
        LookAtPlayer();
        agent.speed = ScaleActionSpeed(movement.speed);
        float stopDistance = Mathf.Max(agent.stoppingDistance, Mathf.Max(0.1f, movement.distanceToStop));
        float elapsed = 0f;
        animator.SetFloat("WalkSpeed", 2f);

        while (elapsed < movement.duration)
        {
            if (player == null || !agent.enabled) yield break;

            agent.SetDestination(player.position);
            if (Vector3.Distance(transform.position, player.position) <= stopDistance)
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (agent.enabled)
            agent.ResetPath();
    }

    private IEnumerator StrafeAroundPlayer(MovementAction movement)
    {
        float orbitRadius = Mathf.Max(1f, movement.distance);
        float sideStepDistance = Mathf.Max(0.5f, orbitRadius * 0.6f);
        float speed = ScaleActionSpeed(movement.speed);
        float elapsed = 0f;

        int direction = strafeDirection;
        strafeDirection *= -1;

        agent.speed = speed;
        animator.SetFloat("WalkSpeed", 1f);

        while (elapsed < movement.duration)
        {
            if (player == null || !agent.enabled) yield break;

            Vector3 radial = transform.position - player.position;
            radial.y = 0f;
            if (radial.sqrMagnitude <= 0.0001f)
            {
                radial = transform.right;
            }

            radial = radial.normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, radial).normalized * direction;
            Vector3 target = player.position + radial * orbitRadius + tangent * sideStepDistance;

            agent.SetDestination(target);
            Debug.DrawLine(transform.position, target, Color.blue, Time.deltaTime);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator RetreatFromPlayer(MovementAction movement)
    {
        if (agent == null || !agent.enabled) yield break;
        agent.speed = ScaleActionSpeed(movement.speed);
        Vector3 retreat = (transform.position - player.position).normalized;
        agent.SetDestination(transform.position + retreat * movement.distance);
        animator.SetFloat("WalkSpeed", 1f);
        yield return new WaitForSeconds(movement.duration);
    }

    private IEnumerator Reposition(MovementAction movement)
    {
        if (agent == null || !agent.enabled || arenaPoints.Count == 0) yield break;

        Transform targetPoint;

        if (movement.randomizeReposition)
        {
            targetPoint = arenaPoints[Random.Range(0, arenaPoints.Count)];
        }
        else
        {
            if (movement.repostionIndex < 0 || movement.repostionIndex >= arenaPoints.Count)
            {
                Debug.LogWarning("Invalid reposition index on movement action, defaulting to random");
                targetPoint = arenaPoints[Random.Range(0, arenaPoints.Count)];
            }
            else
            {
                targetPoint = arenaPoints[movement.repostionIndex];
            }
        }

        agent.speed = ScaleActionSpeed(movement.speed);
        float stopDistance = Mathf.Max(agent.stoppingDistance, Mathf.Max(0.05f, movement.distanceToStop));
        agent.SetDestination(targetPoint.position);
        animator.SetFloat("WalkSpeed", 2f);

        while (!HasReachedDestination(stopDistance))
        {
            if (!agent.enabled) yield break;
            yield return null;
        }

        if (agent.enabled)
            agent.ResetPath();
    }

    private bool HasReachedDestination(float stopDistance)
    {
        if (agent == null || !agent.enabled) return true;
        if (agent.pathPending) return false;
        if (agent.remainingDistance > stopDistance) return false;
        if (agent.hasPath && agent.velocity.sqrMagnitude > 0.0001f) return false;
        return true;
    }

    void ResetActionRoutine()
    {
        if (currentActionRoutine != null)
        {
            StopCoroutine(currentActionRoutine);
            currentActionRoutine = null;
        }
    }

    void ForceCancelAllActions()
    {
        StopAllCoroutines();
        currentActionRoutine = null;
        isCasting = false;
        isWaitingForClone = false;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.updateRotation = true;
        }

        Projectile[] projectiles = FindObjectsOfType<Projectile>();
        foreach (var projectile in projectiles)
        {
            if (projectile != null && projectile.owner == transform)
            {
                Destroy(projectile.gameObject);
            }
        }

        if (animator != null)
        {
            animator.ResetTrigger("Cast");
            animator.SetFloat("WalkSpeed", 0f);
            animator.SetBool("Retreat", false);
        }
    }

    private void ApplyConfiguredEnemyLevel(GameObject target)
    {
        if (target == null || configuredEnemyLevel < 1)
            return;

        var stats = target.GetComponent<EnemyStats>();
        if (stats != null)
        {
            stats.level = configuredEnemyLevel;
        }
    }

    #endregion

    public void DealSpecialDamage(float baseDamage,bool causesStagger = true, float staggerDuration = 1f, bool causesKnockback = true, float knockbackDistance = 1.2f)
    {
        if (isDyingOrDead || !canAct || IsStaggered) return;
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
            ScaleSkillDamage(baseDamage),
            defense,
            critChance,
            critMultiplier,
            levelDiff,
            1f,
            out didCrit
        );

        health.TakeDamage(
            finalDamage,
            didCrit,
            DamageSource.Skill,
            applyStagger: causesStagger,
            staggerDuration: staggerDuration,
            causesKnockback: causesKnockback,
            knockbackDistance: knockbackDistance,
            hitInstigator: transform
        );
    }

    protected override void OnStaggerStarted()
    {
        ResetActionRoutine();
        isCasting = false;
        if (animator != null)
        {
            animator.ResetTrigger("Cast");
            animator.SetFloat("WalkSpeed", 0f);
            animator.SetBool("Retreat", false);
        }
    }

    List<BossSequence> SelectForProfile(List<BossSequence> allSequences)
    {
        List<BossSequence> selected = new();
        foreach(var seq in allSequences)
        {
            if (seq != null)
            {
                selected.Add(seq);
            }
        }

        return selected;
    }
}
