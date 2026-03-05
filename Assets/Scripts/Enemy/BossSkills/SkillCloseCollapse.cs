using UnityEngine;
using System.Collections;

public class SkillCloseCollapse : BossSkill
{
    [Header("Close Collapse Settings")]
    [SerializeField] private float closeCollapseRadius = 4f;
    [SerializeField] private int closeCollapseSegments = 30;
    [SerializeField] private float closeCollapseTelegraphTime = 1f;
    [SerializeField] private GameObject closeCollapseEffectPrefab;

    public override IEnumerator ExecuteRoutine()
    {
        Telegraph t = Instantiate(boss.telegraphPrefab, boss.transform.position, Quaternion.identity);
        t.ConfigureCircle(closeCollapseRadius, closeCollapseSegments);
        Destroy(t.gameObject, closeCollapseTelegraphTime + 0.1f); // Destroy slightly after telegraph time to ensure it disappears
        yield return new WaitForSeconds(closeCollapseTelegraphTime);

        if (closeCollapseEffectPrefab != null)
        {
            GameObject effect = Instantiate(closeCollapseEffectPrefab, boss.transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        if (Vector3.Distance(boss.TargetPlayer.position, boss.transform.position) <= closeCollapseRadius)
        {
            boss.DealSpecialDamage();
        }
    }
}