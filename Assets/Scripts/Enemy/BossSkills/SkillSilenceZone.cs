using UnityEngine;
using System.Collections;

public class SkillSilenceZone : BossSkill
{
    [Header("Telegraph")]
    [SerializeField] private float telegraphTime = 0.8f;
    [SerializeField] private int telegraphSegments = 40;

    [Header("Zone")]
    [SerializeField] private float zoneRadius = 4f;
    [SerializeField] private float zoneDuration = 6f;
    [SerializeField] private bool spawnAtPlayerSnapshot = true;
    [SerializeField] private bool followPlayer = true;
    [SerializeField] private float followSpeed = 2.5f;
    [SerializeField] private Vector3 zoneOffset = new Vector3(0f, 0.05f, 0f);
    [SerializeField] private GameObject zoneEffectPrefab;
    [SerializeField] private GameObject debuffVfxPrefab;

    [Header("Debuff Multipliers")]
    [SerializeField] [Range(0f, 1f)] private float manaRegenMultiplier = 0.35f;
    [SerializeField] [Min(1f)] private float cooldownDurationMultiplier = 1.5f;

    public override IEnumerator ExecuteRoutine()
    {
        if (boss == null) yield break;

        Vector3 center = ResolveZoneCenter();

        Telegraph telegraph = null;
        if (boss.telegraphPrefab != null)
        {
            telegraph = Instantiate(boss.telegraphPrefab, center, Quaternion.identity);
            telegraph.ConfigureCircle(zoneRadius, telegraphSegments, telegraphTime);
            Destroy(telegraph.gameObject, telegraphTime + 0.1f);
        }
        UIManager.Instance.ShowNotification(notificationText, notificationDuration);
        boss.Animator.SetTrigger(animationTrigger);
        yield return new WaitForSeconds(telegraphTime);

        if (telegraph != null)
            Destroy(telegraph.gameObject);
        PlayCastSound();
        SpawnZone(center);
    }

    private Vector3 ResolveZoneCenter()
    {
        Vector3 center = boss.transform.position;
        if (spawnAtPlayerSnapshot && boss.TargetPlayer != null)
            center = boss.TargetPlayer.position;

        center.y = boss.transform.position.y;
        return center + zoneOffset;
    }

    private void SpawnZone(Vector3 center)
    {
        GameObject zoneObject = zoneEffectPrefab != null
            ? Instantiate(zoneEffectPrefab, center, Quaternion.identity)
            : new GameObject("SilenceZone");

        SilenceZoneArea area = zoneObject.GetComponent<SilenceZoneArea>();
        if (area == null)
            area = zoneObject.AddComponent<SilenceZoneArea>();

        area.Initialize(
            boss.TargetPlayer,
            zoneRadius,
            zoneDuration,
            manaRegenMultiplier,
            cooldownDurationMultiplier,
            followPlayer,
            followSpeed,
            debuffVfxPrefab
        );
    }
}
