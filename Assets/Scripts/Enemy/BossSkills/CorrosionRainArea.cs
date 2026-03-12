using UnityEngine;
using System.Collections;

public class CorrosionRainArea : MonoBehaviour
{
    private Transform target;
    private Health playerHealth;
    private float radius;
    private float duration;
    private float damagePerSecond;
    private float damageTickInterval;
    private float healingMultiplier;
    private bool followPlayer;
    private float followSpeed;
    private GameObject debuffVfxPrefab;

    private bool debuffApplied;
    private Coroutine zoneRoutine;
    private float damageTickTimer;
    private int sourceId;
    private GameObject activeDebuffVfx;

    public void Initialize(
        Transform targetPlayer,
        float zoneRadius,
        float zoneDuration,
        float dotDamagePerSecond,
        float dotTickInterval,
        float healMultiplier,
        bool shouldFollowPlayer,
        float zoneFollowSpeed,
        GameObject playerDebuffVfxPrefab
    )
    {
        target = targetPlayer;
        if (target == null && PlayerStats.Instance != null)
            target = PlayerStats.Instance.transform;

        playerHealth = target != null ? target.GetComponent<Health>() : null;

        radius = Mathf.Max(0.1f, zoneRadius);
        duration = Mathf.Max(0f, zoneDuration);
        damagePerSecond = Mathf.Max(0f, dotDamagePerSecond);
        damageTickInterval = Mathf.Max(0f, dotTickInterval);
        healingMultiplier = Mathf.Clamp01(healMultiplier);
        followPlayer = shouldFollowPlayer;
        followSpeed = Mathf.Max(0f, zoneFollowSpeed);
        debuffVfxPrefab = playerDebuffVfxPrefab;
        sourceId = GetInstanceID();
        damageTickTimer = 0f;

        if (zoneRoutine != null)
            StopCoroutine(zoneRoutine);
        zoneRoutine = StartCoroutine(ZoneRoutine());
    }

    private IEnumerator ZoneRoutine()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            UpdateMovement();

            bool inside = IsTargetInside();
            if (inside)
            {
                ApplyDebuff();
                ApplyDamageTick(Time.deltaTime);
            }
            else
            {
                damageTickTimer = 0f;
                RemoveDebuff();
            }

            yield return null;
        }

        RemoveDebuff();
        Destroy(gameObject);
    }

    private bool IsTargetInside()
    {
        if (target == null) return false;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        return toTarget.sqrMagnitude <= radius * radius;
    }

    private void UpdateMovement()
    {
        if (!followPlayer || target == null || followSpeed <= 0f)
            return;

        Vector3 followPosition = target.position;
        followPosition.y = transform.position.y;
        transform.position = Vector3.MoveTowards(transform.position, followPosition, followSpeed * Time.deltaTime);
    }

    private void ApplyDamageTick(float deltaTime)
    {
        if (playerHealth == null || damagePerSecond <= 0f) return;

        if (damageTickInterval <= 0f)
        {
            float frameDamage = damagePerSecond * deltaTime;
            if (frameDamage > 0f)
            {
                playerHealth.TakeDamage(
                    frameDamage,
                    isCrit: false,
                    source: DamageSource.Skill,
                    applyStagger: false
                );
            }
            return;
        }

        damageTickTimer += deltaTime;
        float tickDamage = damagePerSecond * damageTickInterval;
        while (damageTickTimer >= damageTickInterval)
        {
            damageTickTimer -= damageTickInterval;
            if (tickDamage <= 0f) continue;

            playerHealth.TakeDamage(
                tickDamage,
                isCrit: false,
                source: DamageSource.Skill,
                applyStagger: false
            );
        }
    }

    private void ApplyDebuff()
    {
        if (debuffApplied) return;

        playerHealth?.SetExternalHealingMultiplier(sourceId, healingMultiplier);

        if (activeDebuffVfx == null && debuffVfxPrefab != null && target != null)
        {
            activeDebuffVfx = Instantiate(debuffVfxPrefab, target.position, Quaternion.identity, target);
            activeDebuffVfx.transform.localPosition = Vector3.zero;
        }

        debuffApplied = true;
    }

    private void RemoveDebuff()
    {
        if (!debuffApplied) return;

        playerHealth?.ClearExternalHealingMultiplier(sourceId);

        if (activeDebuffVfx != null)
        {
            Destroy(activeDebuffVfx);
            activeDebuffVfx = null;
        }

        debuffApplied = false;
    }

    private void OnDisable()
    {
        RemoveDebuff();
    }

    private void OnDestroy()
    {
        RemoveDebuff();
    }
}
