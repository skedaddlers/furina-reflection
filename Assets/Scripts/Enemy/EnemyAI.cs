using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(EnemyStats))]
public class EnemyAI : MonoBehaviour, IStaggerable
{
    public bool isRanged = false;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    public float movementSpeed = 3f;
    public int damage = 10;
    public float detectionRange = 10f;
    public float turnSpeed = 8f;
    public Projectile projectilePrefab;

    public bool isPerformingSpecialAttack = false;
    protected Transform player;
    protected NavMeshAgent agent;
    protected Animator animator;
    public Animator Animator => animator; // Expose animator to skills that might need it
    protected EnemyStats enemyStats;
    protected float lastAttackTime;
    private Coroutine rotateCoroutine;
    private int baseDamage;
    private float baseAttackCooldown;
    private float baseDetectionRange;
    private float baseMovementSpeed;
    private float baseMaxHealth;
    private bool baseCaptured = false;
    private float currentSpeedModifier = 1f;
    [Header("Stagger Settings")]
    public bool canBeStaggered = true;
    [SerializeField] private string staggerTrigger = "Hit";
    protected bool isStaggered = false;
    private Coroutine staggerRoutine;
    public bool IsStaggered => isStaggered;
    [Header("Audio")]
    public AudioClip walkingSFX;
    public AudioClip attackSFX;

    
    protected virtual void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyStats = GetComponent<EnemyStats>();
        CaptureBaseStats();
        ApplyDifficultyMultipliers();
    }

    protected virtual void Start()
    {
        // Base enemy AI doesn't do anything in Start, but this allows derived classes to call base.Start() if needed
    }

    protected virtual void Update()
    {
        if (isStaggered)
            return;

        UpdateMovementFacing();
    }

    // Panggil dari animation event di animasi Attack
    public virtual void DealDamage()
    {
        if (isStaggered)
            return;

        if (Vector3.Distance(player.position, transform.position) <= attackRange + 0.5f)
        {
            // further implementation include defense, resistances, etc.
            // but for now just directly reduce health
            var health = player.GetComponent<Health>();
            if (health == null) return;

            var playerStats = player.GetComponent<PlayerStats>();
            float defense = playerStats != null ? playerStats.baseDefense : 0f;
            float critChance = enemyStats != null ? enemyStats.critRate : 0f;
            float critMultiplier = enemyStats != null ? enemyStats.critMultiplier : 1f;
            int levelDiff = 0;
            if (enemyStats != null && playerStats != null)
            {
                levelDiff = enemyStats.level - playerStats.level;
            }

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

            health.TakeDamage(
                finalDamage,
                didCrit,
                DamageSource.Melee,
                applyStagger: false,
                staggerDuration: -1f,
                causesKnockback: false,
                knockbackDistance: 1f,
                hitInstigator: transform
            );
        }
    }

    public virtual void RangedAttack()
    {
        if (isStaggered)
            return;

        LookAtPlayer();
        if (Vector3.Distance(player.position, transform.position) <= attackRange + 0.5f)
        {
            if (projectilePrefab != null)
            {
                Vector3 dir = (player.position - transform.position).normalized;
                GameObject go = Instantiate(projectilePrefab.gameObject, transform.position + dir * 1f + Vector3.up * 1.5f, Quaternion.LookRotation(dir));
                var proj = go.GetComponent<Projectile>();
                if (proj == null) proj = go.AddComponent<Projectile>();

                proj.Init(dir, projectilePrefab.speed, projectilePrefab.lifeTime, damage, transform, projectilePrefab.hitMask);
            }
        }
    }

    public virtual bool SeePlayer()
    {
        if (isStaggered)
            return false;

        return Vector3.Distance(player.position, transform.position) <= detectionRange;
    }

    public virtual bool InAttackRange()
    {
        if (isStaggered)
            return false;

        return Vector3.Distance(player.position, transform.position) <= attackRange;
    }

    public virtual void ChasePlayer()
    {
        if (isStaggered)
            return;

        agent.isStopped = false;
        agent.SetDestination(player.position);
        ApplyEffectiveSpeed();
        animator.SetFloat("WalkSpeed", 1f);
    }


    public virtual void StopChasing()
    {
        if (agent == null)
            return;

        agent.isStopped = true;
        animator.SetFloat("WalkSpeed", 0f);
    }
    
    public virtual void AttackPlayer()
    {
        if (isStaggered)
            return;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            if(isRanged)
            {
                animator.SetTrigger("RangedAttack");
            }
            else
            {
                animator.SetTrigger("Attack");
            }
            lastAttackTime = Time.time;
            if (attackSFX != null && Random.value < 0.6f) // 60% chance to play attack SFX to avoid spamming
            {
                AudioManager.Instance?.PlayClipAtPoint(attackSFX, transform.position);
            }
        }
    }

    public virtual void LookAtPlayer()
    {
        if (isStaggered)
            return;

        if (player == null)
            return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // keep only horizontal rotation
        if (direction.sqrMagnitude > 0.0001f)
        {
            if (rotateCoroutine != null)
            {
                StopCoroutine(rotateCoroutine);
            }
            rotateCoroutine = StartCoroutine(RotateTowards(direction.normalized));
        }
    }

    protected bool SnapLookAtPlayer()
    {
        if (isStaggered || player == null)
            return false;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        return SnapFaceDirection(direction);
    }

    protected bool SnapFaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return false;

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }

        transform.rotation = Quaternion.LookRotation(direction.normalized);
        return true;
    }

    private IEnumerator RotateTowards(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.5f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            yield return null;
        }
        transform.rotation = targetRotation;
        rotateCoroutine = null;
    }

    protected void UpdateMovementFacing()
    {
        if (agent == null || agent.isStopped || !agent.updateRotation)
            return;

        Vector3 direction = agent.desiredVelocity.sqrMagnitude > 0.0001f
            ? agent.desiredVelocity
            : agent.velocity;

        if (direction.sqrMagnitude <= 0.0001f && player != null)
        {
            direction = player.position - transform.position;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
    }

    public virtual void SpecialAttack()
    {
        // to be overridden by subclasses
    }

    public virtual bool CanPerformSpecialAttack()
    {
        // Default: common enemy nggak punya special
        return false;
    }

    public void ApplySpeedModifier(float multiplier)
    {
        currentSpeedModifier = Mathf.Max(0f, multiplier);
        ApplyEffectiveSpeed();
    }

    public virtual void ApplyStagger(StaggerInfo info)
    {
        if (!canBeStaggered || !isActiveAndEnabled || info.duration <= 0f)
            return;

        isStaggered = true;
        StopAllCoroutines();
        rotateCoroutine = null;
        isPerformingSpecialAttack = false;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null)
        {
            animator.SetFloat("WalkSpeed", 0f);
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("RangedAttack");
            animator.ResetTrigger("SpecialAttack");
            animator.ResetTrigger("Cast");
            animator.SetBool("IsCharging", false);
            animator.SetBool("Stance", false);
            animator.SetBool("Retreat", false);
            if (!string.IsNullOrEmpty(staggerTrigger))
                animator.SetTrigger(staggerTrigger);
        }

        OnStaggerStarted();
        staggerRoutine = StartCoroutine(StaggerRoutine(info));
    }

    private IEnumerator StaggerRoutine(StaggerInfo info)
    {
        isStaggered = true;

        float elapsed = 0f;
        float knockbackDuration = info.causesKnockback && info.knockbackDistance > 0f
            ? Mathf.Min(0.12f, info.duration)
            : 0f;
        Vector3 knockbackDirection = info.ResolveKnockbackDirection(transform);
        float knockbackSpeed = knockbackDuration > 0f
            ? info.knockbackDistance / knockbackDuration
            : 0f;

        while (elapsed < info.duration)
        {
            float dt = Time.deltaTime;
            if (knockbackDuration > 0f && elapsed < knockbackDuration)
            {
                ApplyKnockbackStep(knockbackDirection, knockbackSpeed * dt);
            }

            elapsed += dt;
            yield return null;
        }

        isStaggered = false;
        staggerRoutine = null;
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
        }
        OnStaggerEnded();
    }

    protected virtual void OnStaggerStarted()
    {
    }

    protected virtual void OnStaggerEnded()
    {
    }

    private void ApplyKnockbackStep(Vector3 direction, float distanceStep)
    {
        if (direction.sqrMagnitude <= 0.0001f || distanceStep <= 0f)
            return;

        Vector3 target = transform.position + direction.normalized * distanceStep;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(target, out hit, distanceStep + 0.5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                return;
            }
        }

        transform.position = target;
    }

    void CaptureBaseStats()
    {
        if (baseCaptured) return;
        baseDamage = damage;
        baseAttackCooldown = attackCooldown;
        baseDetectionRange = detectionRange;
        baseMovementSpeed = movementSpeed;
        var h = GetComponent<Health>();
        if (h != null) baseMaxHealth = h.maxHealth;
        baseCaptured = true;
    }

    public void ApplyDifficultyMultipliers()
    {
        var diff = GlobalDifficultyState.Instance;
        if (diff == null) return;
        var snap = diff.GetEnemyDifficultySnapshot();

        damage = Mathf.RoundToInt(baseDamage * snap.damage);
        attackCooldown = Mathf.Max(0.1f, baseAttackCooldown / Mathf.Max(0.01f, snap.attackSpeed));
        detectionRange = baseDetectionRange * snap.aggro;

        movementSpeed = Mathf.Max(0.5f, baseMovementSpeed * snap.speed);
        ApplyEffectiveSpeed();

        var h = GetComponent<Health>();
        if (h != null && baseMaxHealth > 0f)
        {
            float newMax = Mathf.Max(1f, baseMaxHealth * snap.health);
            h.SetMaxHealth(newMax, keepCurrentRatio: true, fillOnIncrease: true);
        }
    }

    protected EnemyDifficultySnapshot GetEnemyDifficultySnapshot()
    {
        var diff = GlobalDifficultyState.Instance;
        if (diff == null)
        {
            return new EnemyDifficultySnapshot
            {
                damage = 1f,
                health = 1f,
                speed = 1f,
                attackSpeed = 1f,
                aggro = 1f
            };
        }

        return diff.GetEnemyDifficultySnapshot();
    }

    protected float ScaleSkillDamage(float baseDamage)
    {
        return Mathf.Max(0f, baseDamage * GetEnemyDifficultySnapshot().damage);
    }

    protected float ScaleAbilityCooldown(float baseCooldown)
    {
        if (baseCooldown <= 0f)
            return 0f;

        float attackSpeedMultiplier = Mathf.Max(0.01f, GetEnemyDifficultySnapshot().attackSpeed);
        return Mathf.Max(0.05f, baseCooldown / attackSpeedMultiplier);
    }

    protected float ScaleActionSpeed(float authoredSpeed)
    {
        if (authoredSpeed <= 0f)
            return movementSpeed;

        return Mathf.Max(0.1f, authoredSpeed * GetEnemyDifficultySnapshot().speed);
    }

    protected void RestoreDefaultAgentSpeed()
    {
        ApplyEffectiveSpeed();
    }

    private void ApplyEffectiveSpeed()
    {
        if (agent == null) return;
        float effectiveSpeed = Mathf.Max(0f, movementSpeed * currentSpeedModifier);
        agent.speed = effectiveSpeed;
    }

    // through animation event
    public void PlayWalkingSFX()
    {
        if (walkingSFX != null)
        {
            AudioManager.Instance?.PlayClipAtPoint(walkingSFX, transform.position);
        }
    }
}
