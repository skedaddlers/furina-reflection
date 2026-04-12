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
    public GameObject bleedVFXPrefab;
    public AudioClip bleedApplySFX;

    [Header("Teleport Telegraph")]
    public GameObject telegraphPrefab;
    public float telegraphDuration = 0.4f;
    public float telegraphRadius = 1.25f;
    public int telegraphSegments = 24;
    public float telegraphYOffset = 0.05f;

    private GameObject activeTelegraph;

    public override bool CanPerformSpecialAttack()
    {
        return !isPerformingSpecialAttack &&
               Time.time - lastSpecialAttackTime >= specialAttackCooldown &&
               player != null;
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
        StartCoroutine(DoRifthoundSpecial());
    }

    private IEnumerator DoRifthoundSpecial()
    {
        isPerformingSpecialAttack = true;
        lastSpecialAttackTime = Time.time;

        if (player == null || agent == null)
        {
            isPerformingSpecialAttack = false;
            yield break;
        }

        // Anim khusus kalau ada
        if (animator != null)
            animator.SetTrigger("SpecialAttack");

        // Telegraph destination before warp
        Vector3 teleportTarget;
        bool didTeleport = false;
        if (TryGetTeleportDestination(out teleportTarget))
        {
            ShowTeleportTelegraph(teleportTarget);
            if (telegraphDuration > 0f)
            {
                yield return new WaitForSeconds(telegraphDuration);
            }
        
            agent.Warp(teleportTarget);
            FacePlayer();
            didTeleport = true;
        }

        // Hit awal (melee biasa)
        Health playerHealth = player != null ? player.GetComponent<Health>() : null;
        bool landedTeleportStrike = didTeleport &&
                                   playerHealth != null &&
                                   !playerHealth.IsInvulnerable &&
                                   Vector3.Distance(player.position, transform.position) <= attackRange + 0.5f;
        DealDamage();
        if (landedTeleportStrike && playerHealth.CurrentHealth > 0f)
        {
            ApplyBleedToPlayer(playerHealth);
        }

        isPerformingSpecialAttack = false;
    }

    private void ApplyBleedToPlayer(Health targetHealth)
    {
        if (targetHealth == null || bleedDamagePerTick <= 0 || bleedTicks <= 0)
            return;

        int sourceId = GetInstanceID();
        RifthoundBleedEffect bleedEffect = null;
        RifthoundBleedEffect[] existingEffects = targetHealth.GetComponents<RifthoundBleedEffect>();
        for (int i = 0; i < existingEffects.Length; i++)
        {
            if (existingEffects[i].SourceId == sourceId)
            {
                bleedEffect = existingEffects[i];
                break;
            }
        }

        if (bleedEffect == null)
        {
            bleedEffect = targetHealth.gameObject.AddComponent<RifthoundBleedEffect>();
        }

        if (bleedApplySFX != null)
        {
            AudioManager.Instance?.PlayClipAtPoint(bleedApplySFX, targetHealth.transform.position);
        }

        bleedEffect.Apply(sourceId, bleedDamagePerTick, bleedTickInterval, bleedTicks, transform, bleedVFXPrefab);
    }

    private bool TryGetTeleportDestination(out Vector3 destination)
    {
        destination = transform.position;
        if (player == null)
            return false;

        Vector3 desiredPosition = player.position - player.forward * teleportDistanceFromPlayer;
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(desiredPosition, out hit, 2f, NavMesh.AllAreas))
            return false;

        destination = hit.position;
        return true;
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(toPlayer.normalized);
    }

    private void ShowTeleportTelegraph(Vector3 targetPosition)
    {
        if (telegraphPrefab == null)
            return;

        HideTelegraph();
        activeTelegraph = Instantiate(telegraphPrefab, targetPosition + Vector3.up * telegraphYOffset, Quaternion.identity);

        Telegraph telegraph = activeTelegraph.GetComponent<Telegraph>();

        Destroy(activeTelegraph, telegraphDuration + 0.1f); // Destroy slightly after telegraph time to ensure it disappears
        if (telegraph != null)
        {
            telegraph.ConfigureCircle(telegraphRadius, telegraphSegments);
        }
    }

    private void HideTelegraph()
    {
        if (activeTelegraph != null)
        {
            Destroy(activeTelegraph);
            activeTelegraph = null;
        }
    }

    protected override void OnStaggerStarted()
    {
        HideTelegraph();
    }
}
