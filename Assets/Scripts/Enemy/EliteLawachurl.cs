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
    public float chargeOvershootDistance = 1.5f;
    public float maxChargeDistance = 10f;

    [Header("Cone Telegraph")]
    public GameObject telegraphPrefab;
    private GameObject activeTelegraph;
    public float telegraphDuration = 0.6f;
    public Telegraph.TelegraphShape telegraphShape = Telegraph.TelegraphShape.Rectangle;

    [Header("Telegraph Shape Settings")]
    [Range(1f, 360f)] public float telegraphAngle = 45f;
    public int telegraphSegments = 30;
    public float telegraphWidth = 3f;

    private bool heavyAttackTriggered = false;


    private float originalSpeed;

    protected override void Awake()
    {
        base.Awake();

        if (agent != null)
            originalSpeed = agent.speed;

    }

    private void OnDisable()
    {
        HideTelegraph();
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
        heavyAttackTriggered = false;
        lastSpecialAttackTime = Time.time;

        if (agent == null || player == null)
        {
            isPerformingSpecialAttack = false;
            yield break;
        }

        Vector3 startPosition = transform.position;
        Vector3 toPlayer = player.position - startPosition;
        toPlayer.y = 0f;

        Vector3 chargeDirection = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : transform.forward;
        float distanceToPlayer = toPlayer.magnitude;

        float estimatedMaxTravel = (originalSpeed > 0f ? originalSpeed : movementSpeed) * chargeSpeedMultiplier * chargeDuration;
        float intendedDistance = Mathf.Clamp(distanceToPlayer + chargeOvershootDistance, attackRange + 1f, Mathf.Max(maxChargeDistance, estimatedMaxTravel));
        Vector3 chargeTarget = startPosition + chargeDirection * intendedDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(chargeTarget, out hit, 1.5f, NavMesh.AllAreas))
        {
            chargeTarget = hit.position;
        }

        transform.rotation = Quaternion.LookRotation(chargeDirection);
        if (animator != null)
            animator.SetBool("Stance", true);
        ShowTelegraph(startPosition, chargeDirection, intendedDistance);
        yield return new WaitForSeconds(telegraphDuration);
        HideTelegraph();

        if (animator != null)
        {
            animator.SetBool("IsCharging", true);
            animator.SetBool("Stance", false);
        }

        float prevSpeed = agent.speed;
        agent.speed = prevSpeed * chargeSpeedMultiplier;
        agent.isStopped = false;
        agent.SetDestination(chargeTarget);

        float t = 0f;
        while (t < chargeDuration)
        {
            t += Time.deltaTime;
            if (!heavyAttackTriggered && PlayerInChargePath())
            {
                heavyAttackTriggered = true;
                break;
            }
            yield return null;
        }


        agent.isStopped = true;
        agent.ResetPath();

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

        if (animator != null)
            animator.SetBool("IsCharging", false);

        isPerformingSpecialAttack = false;
    }

    private bool PlayerInChargePath()
    {
        // with the forward dir enemy is facing, check if player is within a certain angle and distance
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distanceToPlayer = toPlayer.magnitude;
        if (distanceToPlayer > maxChargeDistance + 1f)
            return false;

        Vector3 directionToPlayer = toPlayer.normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        return angleToPlayer <= telegraphAngle / 2f && distanceToPlayer <= attackRange;
    }



    public override bool CanPerformSpecialAttack()
    {
        return !isPerformingSpecialAttack &&
               Time.time - lastSpecialAttackTime >= specialAttackCooldown &&
               player != null;
    }

    private void ShowTelegraph(Vector3 startPos, Vector3 direction, float distance)
    {
        if (telegraphPrefab == null)
            return;

        activeTelegraph = Instantiate(telegraphPrefab, startPos + Vector3.up * 0.05f, Quaternion.identity);

        activeTelegraph.transform.rotation = Quaternion.LookRotation(direction);

        Telegraph telegraph = activeTelegraph.GetComponent<Telegraph>();
        if (telegraph != null)
        {
            switch (telegraphShape)
            {
                case Telegraph.TelegraphShape.Cone:
                    telegraph.ConfigureCone(distance, telegraphAngle, telegraphSegments);
                    break;
                case Telegraph.TelegraphShape.Circle:
                    telegraph.ConfigureCircle(distance, telegraphSegments);
                    break;
                case Telegraph.TelegraphShape.Rectangle:
                    telegraph.ConfigureRectangle(telegraphWidth, distance);
                    break;
            }
        }

        Destroy(activeTelegraph, telegraphDuration + 0.1f); // Destroy slightly after telegraph time to ensure it disappears
    }

    private void HideTelegraph()
    {
        if (activeTelegraph != null)
            Destroy(activeTelegraph);
    }


}
