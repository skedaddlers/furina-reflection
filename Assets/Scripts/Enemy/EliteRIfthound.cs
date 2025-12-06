using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EliteRifthound : EnemyAI
{
    [Header("Special Attack Settings")]
    public float specialAttackCooldown = 8f;
    private float lastSpecialAttackTime;

    [Tooltip("Seberapa dekat dia teleport dari player")]
    public float teleportDistanceFromPlayer = 1.5f;

    [Header("Bleed Settings")]
    public int bleedDamagePerTick = 3;
    public float bleedTickInterval = 1f;
    public int bleedTicks = 5;

    public override bool CanPerformSpecialAttack()
    {
        return !isPerformingSpecialAttack &&
               Time.time - lastSpecialAttackTime >= specialAttackCooldown &&
               player != null;
    }

    public override void SpecialAttack()
    {
        if (!CanPerformSpecialAttack())
            return;

        StopChasing();
        StartCoroutine(DoRifthoundSpecial());
    }

    private IEnumerator DoRifthoundSpecial()
    {
        isPerformingSpecialAttack = true;

        // Anim khusus kalau ada
        if (animator != null)
            animator.SetTrigger("SpecialAttack");

        // Sedikit delay telegraph
        yield return new WaitForSeconds(0.2f);

        // 1. Teleport di belakang / dekat player
        if (agent != null && player != null)
        {
            Vector3 targetPos = player.position - player.forward * teleportDistanceFromPlayer;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 2f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                transform.rotation = Quaternion.LookRotation(player.position - transform.position);
            }
            else
            {
                // fallback: teleport ke posisi sekarang aja
                agent.Warp(transform.position);
            }
        }

        // 2. Hit awal (melee biasa)
        DealDamage();

        // 3. Apply bleed (damage over time)
        if (player != null)
        {
            var hp = player.GetComponent<Health>();
            if (hp != null)
            {
                for (int i = 0; i < bleedTicks; i++)
                {
                    yield return new WaitForSeconds(bleedTickInterval);
                    hp.TakeDamage(bleedDamagePerTick);
                }
            }
        }

        lastSpecialAttackTime = Time.time;
        isPerformingSpecialAttack = false;
    }
}
