using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(EnemyStats))]
public class EnemyAI : MonoBehaviour
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
    protected EnemyStats enemyStats;
    protected float lastAttackTime;
    private Coroutine rotateCoroutine;
    private int baseDamage;
    private float baseAttackCooldown;
    private float baseDetectionRange;
    private float baseAgentSpeed;
    private float baseMaxHealth;
    private bool baseCaptured = false;

    protected virtual void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyStats = GetComponent<EnemyStats>();
        CaptureBaseStats();
        ApplyDifficultyMultipliers();
    }

    protected virtual void Update()
    {
        UpdateMovementFacing();
    }

    // Panggil dari animation event di animasi Attack
    public virtual void DealDamage()
    {
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

            health.TakeDamage(finalDamage, didCrit);
        }
    }

    public virtual void RangedAttack()
    {
        LookAtPlayer();
        if (Vector3.Distance(player.position, transform.position) <= attackRange + 0.5f)
        {
            if (projectilePrefab != null)
            {
                Vector3 dir = (player.position - transform.position).normalized;
                GameObject go = Instantiate(projectilePrefab.gameObject, transform.position + dir * 1f + Vector3.up * 1.5f, Quaternion.LookRotation(dir));
                var proj = go.GetComponent<Projectile>();
                if (proj == null) proj = go.AddComponent<Projectile>();

                proj.Init(dir, this.transform);
            }
        }
    }

    public virtual bool SeePlayer()
    {
        return Vector3.Distance(player.position, transform.position) <= detectionRange;
    }

    public virtual bool InAttackRange()
    {
        return Vector3.Distance(player.position, transform.position) <= attackRange;
    }

    public virtual void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
        agent.speed = movementSpeed;
        animator.SetFloat("WalkSpeed", 1f);
    }


    public virtual void StopChasing()
    {
        agent.isStopped = true;
        animator.SetFloat("WalkSpeed", 0f);
    }
    
    public virtual void AttackPlayer()
    {
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
        }
    }

    public virtual void LookAtPlayer()
    {
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
        if (agent == null || agent.isStopped)
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
        if (agent != null)
        {
            agent.speed = movementSpeed * multiplier;
        }
    }

    void CaptureBaseStats()
    {
        if (baseCaptured) return;
        baseDamage = damage;
        baseAttackCooldown = attackCooldown;
        baseDetectionRange = detectionRange;
        baseAgentSpeed = agent != null ? agent.speed : 3f;
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

        if (agent != null)
        {
            agent.speed = Mathf.Max(0.5f, baseAgentSpeed * snap.speed);
        }

        var h = GetComponent<Health>();
        if (h != null && baseMaxHealth > 0f)
        {
            float newMax = Mathf.Max(1f, baseMaxHealth * snap.health);
            h.SetMaxHealth(newMax, keepCurrentRatio: true, fillOnIncrease: true);
        }
    }
}
