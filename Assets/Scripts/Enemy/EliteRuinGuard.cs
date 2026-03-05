using UnityEngine;
using System.Collections;
using System;

// special attack: throws a rock that deals aoe damage
public class EliteRuinGuard : EnemyAI
{
    public float specialAttackCooldown = 5f;
    private float lastSpecialAttackTime;

    [Header("Impact Telegraph")]
    public GameObject telegraphPrefab;
    public float telegraphDuration = 0.75f;
    public float telegraphRadiusOverride = -1f;
    public int telegraphSegments = 24;
    public float telegraphYOffset = 0.05f;
    public float trajectoryPredictionStep = 0.05f;

    private Vector3 lockedDirection;
    private Vector3 lockedSpawnPosition;
    private Vector3 lockedImpactPoint;
    private GameObject activeTelegraph;
    private Coroutine telegraphLifetimeCoroutine;

    private const float ThrowSpawnForwardOffset = 1f;
    private const float ThrowSpawnHeightOffset = 1.5f;

    protected override void Awake()
    {
        base.Awake();
    }

    private void OnDisable()
    {
        HideTelegraph();
    }

    public override void SpecialAttack()
    {
        if (!CanPerformSpecialAttack())
            return;

        if (player == null)
            return;

        StopChasing();
        LookAtPlayer();

        DoThrowRockAttack();
    }

    private void DoThrowRockAttack()
    {
        if (player == null)
        {
            isPerformingSpecialAttack = false;
            return;
        }

        isPerformingSpecialAttack = true;
        lastSpecialAttackTime = Time.time;

        // Trigger anim khusus kalau ada
        if (animator != null)
            animator.SetTrigger("SpecialAttack");

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude <= 0.0001f)
            toPlayer = transform.forward;

        lockedDirection = toPlayer.normalized;
        lockedSpawnPosition = transform.position + lockedDirection * ThrowSpawnForwardOffset + Vector3.up * ThrowSpawnHeightOffset;

        if (!TryPredictImpactPoint(lockedSpawnPosition, lockedDirection, out lockedImpactPoint))
        {
            lockedImpactPoint = player.position;
        }
        lockedImpactPoint.y = 0f;

        ShowImpactTelegraph(lockedImpactPoint);
    }

    // throw method through animation event
    public void ThrowRock()
    {
        if (!isPerformingSpecialAttack)
            return;

        if (player == null)
        {
            HideTelegraph();
            isPerformingSpecialAttack = false;
            return;
        }

        StartCoroutine(DoThrowRockEffect());
    }

    private IEnumerator DoThrowRockEffect()
    {
        HideTelegraph();

        // Buat projectile rock
        if (projectilePrefab != null)
        {
            Vector3 dir = lockedDirection.sqrMagnitude > 0.0001f ? lockedDirection : transform.forward;
            Vector3 spawnPosition = lockedSpawnPosition;

            GameObject go = Instantiate(projectilePrefab.gameObject, spawnPosition, Quaternion.LookRotation(dir));
            var proj = go.GetComponent<Projectile>();
            if (proj == null)
                proj = go.AddComponent<Projectile>();

            proj.Init(dir, this.transform);
        }

        yield return new WaitForSeconds(0.5f); // tunggu anim selesai

        isPerformingSpecialAttack = false;
    }

    private bool TryPredictImpactPoint(Vector3 spawnPosition, Vector3 direction, out Vector3 impactPoint)
    {
        impactPoint = spawnPosition + direction * 2f;
        if (projectilePrefab == null)
            return false;

        Vector3 normalizedDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        float speed = Mathf.Max(0.1f, projectilePrefab.speed);
        float lifeTime = Mathf.Max(0.1f, projectilePrefab.lifeTime);
        LayerMask hitMask = projectilePrefab.hitMask;

        if (projectilePrefab.mode == Projectile.ProjectileMode.Trajectory)
        {
            return TryPredictTrajectoryImpactPoint(
                spawnPosition,
                normalizedDirection * speed,
                lifeTime,
                projectilePrefab.gravityMultiplier,
                hitMask,
                out impactPoint
            );
        }

        float maxDistance = speed * lifeTime;
        Vector3 targetPosition = spawnPosition + normalizedDirection * maxDistance;
        RaycastHit hit;
        if (TryGetFirstValidHit(spawnPosition, targetPosition, hitMask, out hit))
        {
            impactPoint = hit.point;
            return true;
        }

        impactPoint = targetPosition;
        return false;
    }

    private bool TryPredictTrajectoryImpactPoint(
        Vector3 startPosition,
        Vector3 startVelocity,
        float maxTime,
        float gravityMultiplier,
        LayerMask hitMask,
        out Vector3 impactPoint
    )
    {
        Vector3 position = startPosition;
        Vector3 velocity = startVelocity;
        float elapsed = 0f;
        float step = Mathf.Max(0.01f, trajectoryPredictionStep);

        while (elapsed < maxTime)
        {
            float dt = Mathf.Min(step, maxTime - elapsed);
            velocity += Physics.gravity * gravityMultiplier * dt;
            Vector3 nextPosition = position + velocity * dt;

            RaycastHit hit;
            if (TryGetFirstValidHit(position, nextPosition, hitMask, out hit))
            {
                impactPoint = hit.point;
                return true;
            }

            position = nextPosition;
            elapsed += dt;
        }

        impactPoint = position;
        return false;
    }

    private bool TryGetFirstValidHit(Vector3 from, Vector3 to, LayerMask hitMask, out RaycastHit validHit)
    {
        validHit = default;
        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance <= 0.0001f)
            return false;

        RaycastHit[] hits = Physics.RaycastAll(
            from,
            delta / distance,
            distance,
            hitMask,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
            return false;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null)
                continue;

            if (col.transform == transform || col.transform.IsChildOf(transform))
                continue;

            validHit = hits[i];
            return true;
        }

        return false;
    }

    private void ShowImpactTelegraph(Vector3 impactPoint)
    {
        if (telegraphPrefab == null)
            return;

        HideTelegraph();
        activeTelegraph = Instantiate(
            telegraphPrefab,
            impactPoint + Vector3.up * telegraphYOffset,
            Quaternion.identity
        );

        Telegraph telegraph = activeTelegraph.GetComponent<Telegraph>();
        if (telegraph != null)
        {
            float radius = telegraphRadiusOverride > 0f
                ? telegraphRadiusOverride
                : (projectilePrefab != null && projectilePrefab.isAOE ? projectilePrefab.aoeRadius : 2f);
            telegraph.ConfigureCircle(radius, telegraphSegments);
        }

        Destroy(activeTelegraph, telegraphDuration + 0.1f); // Destroy slightly after telegraph time to ensure it disappears

        if (telegraphDuration > 0f)
        {
            if (telegraphLifetimeCoroutine != null)
                StopCoroutine(telegraphLifetimeCoroutine);
            telegraphLifetimeCoroutine = StartCoroutine(HideTelegraphAfterDelay(telegraphDuration));
        }
    }

    private IEnumerator HideTelegraphAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        telegraphLifetimeCoroutine = null;
        HideTelegraph();
    }

    private void HideTelegraph()
    {
        if (telegraphLifetimeCoroutine != null)
        {
            StopCoroutine(telegraphLifetimeCoroutine);
            telegraphLifetimeCoroutine = null;
        }

        if (activeTelegraph != null)
        {
            Destroy(activeTelegraph);
            activeTelegraph = null;
        }
    }

    public override bool CanPerformSpecialAttack()
    {
        return !isPerformingSpecialAttack &&
               Time.time - lastSpecialAttackTime >= specialAttackCooldown &&
               player != null;
    }
}
