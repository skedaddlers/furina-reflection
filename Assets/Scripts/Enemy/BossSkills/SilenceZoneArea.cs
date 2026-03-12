using UnityEngine;
using System.Collections;

public class SilenceZoneArea : MonoBehaviour
{
    private Transform target;
    private PlayerStats playerStats;
    private SkillManager skillManager;
    private float radius;
    private float duration;
    private float manaRegenMultiplier;
    private float cooldownDurationMultiplier;
    private bool followPlayer;
    private float followSpeed;
    private GameObject debuffVfxPrefab;

    private bool debuffApplied;
    private Coroutine zoneRoutine;
    private int sourceId;
    private GameObject activeDebuffVfx;

    public void Initialize(
        Transform targetPlayer,
        float zoneRadius,
        float zoneDuration,
        float manaRegenMult,
        float cooldownDurationMult,
        bool shouldFollowPlayer,
        float zoneFollowSpeed,
        GameObject playerDebuffVfxPrefab
    )
    {
        target = targetPlayer;
        if (target == null && PlayerStats.Instance != null)
            target = PlayerStats.Instance.transform;

        playerStats = target != null ? target.GetComponent<PlayerStats>() : PlayerStats.Instance;
        skillManager = target != null ? target.GetComponent<SkillManager>() : null;
        if (skillManager == null && playerStats != null)
            skillManager = playerStats.GetComponent<SkillManager>();

        radius = Mathf.Max(0.1f, zoneRadius);
        duration = Mathf.Max(0f, zoneDuration);
        manaRegenMultiplier = Mathf.Clamp01(manaRegenMult);
        cooldownDurationMultiplier = Mathf.Max(1f, cooldownDurationMult);
        followPlayer = shouldFollowPlayer;
        followSpeed = Mathf.Max(0f, zoneFollowSpeed);
        debuffVfxPrefab = playerDebuffVfxPrefab;
        sourceId = GetInstanceID();

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
            UpdateDebuffState();
            yield return null;
        }

        RemoveDebuff();
        Destroy(gameObject);
    }

    private void UpdateDebuffState()
    {
        bool inside = IsTargetInside();
        if (inside)
            ApplyDebuff();
        else
            RemoveDebuff();
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

        Vector3 targetPos = target.position;
        targetPos.y = transform.position.y;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, followSpeed * Time.deltaTime);
    }

    private void ApplyDebuff()
    {
        if (debuffApplied) return;

        playerStats?.SetExternalManaRegenMultiplier(sourceId, manaRegenMultiplier);
        skillManager?.SetExternalCooldownDurationMultiplier(sourceId, cooldownDurationMultiplier);

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

        playerStats?.ClearExternalManaRegenMultiplier(sourceId);
        skillManager?.ClearExternalCooldownDurationMultiplier(sourceId);

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
