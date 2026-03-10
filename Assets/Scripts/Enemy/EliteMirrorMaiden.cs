using UnityEngine;
using System.Collections;
using UnityEngine.AI;

// special attack: teleports away and imprisons the player for a short duration
public class EliteMirrorMaiden : EnemyAI
{
    public float specialAttackCooldown = 5f;
    private float lastSpecialAttackTime;

    [Header("Special Attack Settings")]
    public float imprisonDuration = 2.0f;
    public ParticleSystem imprisonEffect;
    private PlayerController imprisonedPlayerController;

    protected override void Awake()
    {
        base.Awake();
        // Additional initialization for Elite Lawachurl if needed
    }

    private void OnDisable()
    {
        ReleaseImprisonedPlayer();
    }

    public override void SpecialAttack()
    {
        if (!CanPerformSpecialAttack())
            return;
        StopChasing();
        LookAtPlayer();

        DoImprisonAttack();
    }

    private void DoImprisonAttack()
    {
        isPerformingSpecialAttack = true;
        lastSpecialAttackTime = Time.time;

        // teleports away from player
        if (agent != null && player != null)
        {
            Vector3 directionAwayFromPlayer = (transform.position - player.position).normalized;
            Vector3 targetPos = transform.position + directionAwayFromPlayer * 5f; // teleport 5 units away
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 2f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                // fallback: teleport to current position
                agent.Warp(transform.position);
            }
        }

        // Trigger anim khusus kalau ada
        if (animator != null)
            animator.SetTrigger("SpecialAttack");

        // Naikkan speed dan charge ke arah player
    }

    // imprison method through animation event
    public void ImprisonPlayer()
    {
        if (player != null)
        {
            StartCoroutine(DoImprisonEffect());
        }
    }
    private IEnumerator DoImprisonEffect()
    {
        // Play imprison effect
        if (imprisonEffect != null)
        {
            GameObject effectInstance = Instantiate(imprisonEffect.gameObject, player.position, Quaternion.identity);
            effectInstance.transform.parent = player.transform;
            Destroy(effectInstance, imprisonDuration);
        }

        // Disable player movement
        imprisonedPlayerController = player.GetComponent<PlayerController>();
        if (imprisonedPlayerController != null)
        {
            imprisonedPlayerController.speedMultiplier = 0.1f; 
        }

        // Wait for duration
        yield return new WaitForSeconds(imprisonDuration);

        // Enable player movement
        ReleaseImprisonedPlayer();
        isPerformingSpecialAttack = false;
    }

    public override bool CanPerformSpecialAttack()
    {
        return !isPerformingSpecialAttack &&
               Time.time - lastSpecialAttackTime >= specialAttackCooldown &&
               player != null;
    }

    private void ReleaseImprisonedPlayer()
    {
        if (imprisonedPlayerController != null)
        {
            imprisonedPlayerController.speedMultiplier = 1f;
            imprisonedPlayerController = null;
        }
    }

    protected override void OnStaggerStarted()
    {
        ReleaseImprisonedPlayer();
    }
}
