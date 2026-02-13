using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class EliteLawachurl : EnemyAI
{
    public float specialAttackCooldown = 5f;
    private float lastSpecialAttackTime;

    [Header("Charge Settings")]
    public float chargeDuration = 1.0f;
    public float chargeSpeedMultiplier = 2.5f;
    public float heavyAttackDamageMultiplier = 2f;

    private float originalSpeed;

    protected override void Awake()
    {
        base.Awake();
        if (agent != null)
            originalSpeed = agent.speed;
        // Additional initialization for Elite Lawachurl if needed
    }
    public override void SpecialAttack()
    {
        if (!CanPerformSpecialAttack())
            return;
        StopChasing();

        StartCoroutine(DoChargeAttack());    
    }

    private IEnumerator DoChargeAttack()
    {
        isPerformingSpecialAttack = true;
        lastSpecialAttackTime = Time.time;

        // Trigger anim khusus kalau ada
        if (animator != null)
            animator.SetBool("IsCharging", true);

        // Naikkan speed dan charge ke arah player
        if (agent != null && player != null)
        {
            float prevSpeed = agent.speed;
            agent.speed = prevSpeed * chargeSpeedMultiplier;
            agent.isStopped = false;
            agent.SetDestination(player.position);

            float t = 0f;
            while (t < chargeDuration)
            {
                t += Time.deltaTime;
                yield return null;
            }

            // Heavy hit di akhir charge
            if (Vector3.Distance(player.position, transform.position) <= attackRange + 1.0f)
            {
                var hp = player.GetComponent<Health>();
                if (hp != null)
                {
                    float baseDamage = damage * heavyAttackDamageMultiplier;
                    var playerStats = player.GetComponent<PlayerStats>();
                    var enemyStats = GetComponent<EnemyStats>();
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
                        baseDamage,
                        defense,
                        critChance,
                        critMultiplier,
                        levelDiff,
                        1f,
                        out didCrit
                    );

                    hp.TakeDamage(finalDamage, didCrit);
                }
            }

            agent.speed = prevSpeed;
            agent.isStopped = false;
        }
        if (animator != null)
            animator.SetBool("IsCharging", false);

        isPerformingSpecialAttack = false;
    }

    public override bool CanPerformSpecialAttack()
    {
        return !isPerformingSpecialAttack &&
               Time.time - lastSpecialAttackTime >= specialAttackCooldown &&
               player != null;
    }
}
