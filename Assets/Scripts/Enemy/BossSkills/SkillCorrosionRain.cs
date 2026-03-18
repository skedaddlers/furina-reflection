using UnityEngine;
using System.Collections;

public class SkillCorrosionRain : BossSkill
{
    [Header("Telegraph")]
    [SerializeField] private float telegraphTime = 1f;
    [SerializeField] private float telegraphRadius = 6f;
    [SerializeField] private int telegraphSegments = 48;

    [Header("Zone")]
    [SerializeField] private float zoneRadius = 6f;
    [SerializeField] private float zoneDuration = 7f;
    [SerializeField] private bool spawnAtPlayerSnapshot = true;
    [SerializeField] private bool followPlayer = false;
    [SerializeField] private float followSpeed = 1.5f;
    [SerializeField] private Vector3 zoneOffset = new Vector3(0f, 0.05f, 0f);
    [SerializeField] private GameObject zoneEffectPrefab;

    [Header("Debuff")]
    [SerializeField] [Range(0f, 1f)] private float healingMultiplier = 0.5f;
    [SerializeField] private GameObject debuffVfxPrefab;

    [Header("Damage Over Time")]
    [SerializeField] [Range(0f, 1f)] private float maxHealthDamagePerSecondPercent = 0.03f;
    [SerializeField] private float damageTickInterval = 0.4f;

    public override IEnumerator ExecuteRoutine()
    {
        if (boss == null) yield break;

        Vector3 zoneCenter = ResolveZoneCenter();

        Telegraph telegraph = null;
        if (boss.telegraphPrefab != null)
        {
            telegraph = Instantiate(boss.telegraphPrefab, zoneCenter, Quaternion.identity);
            telegraph.ConfigureCircle(telegraphRadius, telegraphSegments, telegraphTime);
            Destroy(telegraph.gameObject, telegraphTime + 0.1f);
        }

        boss.Animator.SetTrigger(animationTrigger);
        if (telegraphTime > 0f)
            yield return new WaitForSeconds(telegraphTime);

        if (telegraph != null)
            Destroy(telegraph.gameObject);

        SpawnZone(zoneCenter);
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
            : new GameObject("CorrosionRainZone");

        CorrosionRainArea area = zoneObject.GetComponent<CorrosionRainArea>();
        if (area == null)
            area = zoneObject.AddComponent<CorrosionRainArea>();

        area.Initialize(
            boss.TargetPlayer,
            zoneRadius,
            zoneDuration,
            maxHealthDamagePerSecondPercent,
            damageTickInterval,
            healingMultiplier,
            followPlayer,
            followSpeed,
            debuffVfxPrefab
        );
    }
}
