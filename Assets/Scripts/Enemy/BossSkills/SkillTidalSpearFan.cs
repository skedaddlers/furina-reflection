using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SkillTidalSpearFan : BossSkill
{
    [Header("Tidal Spear Fan Settings")]
    [SerializeField] private int spearCount = 3;
    [SerializeField] private float spearRange = 10f;
    [SerializeField] private float spearAngle = 20f; 
    [SerializeField] private float spearSpreadAngle = 35f; 
    [SerializeField] private float spearTelegraphTime = 1.2f;
    [SerializeField] private GameObject spearEffectPrefab;

    [SerializeField] private Vector3 effectPositionOffset;
    [SerializeField] private Vector3 effectRotationOffset;

    public override IEnumerator ExecuteRoutine()
    {
        List<Telegraph> spears = new List<Telegraph>();
        
        // Calculate starting angle so the fan is perfectly centered in front of the boss
        float startAngle = -spearSpreadAngle * (spearCount - 1) / 2f;

        boss.Animator.SetTrigger(animationTrigger);
        for (int i = 0; i < spearCount; i++)
        {
            float currentAngle = startAngle + (i * spearSpreadAngle);
            Quaternion rotation = boss.transform.rotation * Quaternion.Euler(0, currentAngle, 0);
            
            Telegraph t = Instantiate(boss.telegraphPrefab, boss.transform.position, rotation);
            t.ConfigureCone(spearRange, spearAngle, 20);
            spears.Add(t);
            Destroy(t.gameObject, spearTelegraphTime + 0.1f); // Destroy slightly after telegraph time to ensure it disappears
        }

        yield return new WaitForSeconds(spearTelegraphTime);

        for (int i = 0; i < spearCount; i++)
        {
            float currentAngle = startAngle + (i * spearSpreadAngle);
            Quaternion rotation = boss.transform.rotation * Quaternion.Euler(0, currentAngle, 0);
            if (spearEffectPrefab != null)
            {
                GameObject effect = Instantiate(spearEffectPrefab, boss.transform.position + effectPositionOffset, rotation * Quaternion.Euler(effectRotationOffset));
                Destroy(effect, 2f);
            }
        }

        // Calculate hits
        foreach (var t in spears)
        {
            if (Vector3.Distance(boss.TargetPlayer.position, boss.transform.position) <= spearRange)
            {
                Vector3 dirToPlayer = (boss.TargetPlayer.position - boss.transform.position).normalized;
                float angle = Vector3.Angle(t.transform.forward, dirToPlayer);
                if (angle <= spearAngle * 0.5f)
                {
                    boss.DealSpecialDamage(baseDamage, causesStagger, staggerDuration, causesKnockback, knockbackDistance);
                    // Break so the player only takes damage once, even if caught between overlapping cones
                    break; 
                }
            }
        }

        // Cleanup
        foreach (var t in spears)        
            if (t != null)
            {
                Destroy(t.gameObject);
            }
    }
}