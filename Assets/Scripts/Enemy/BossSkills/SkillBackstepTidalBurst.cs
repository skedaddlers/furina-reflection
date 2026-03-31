using UnityEngine;
using System.Collections;

public class SkillBackstepTidalBurst : BossSkill
{
    [Header("Backstep Tidal Burst Settings")]
    [SerializeField] private float backstepDistance = 6f;
    [SerializeField] private float backstepDuration = 0.3f;
    [SerializeField] private float backstepRadius = 5f;
    [SerializeField] private float backstepTelegraphTime = 1.2f;
    [SerializeField] private GameObject backstepEffectPrefab;

    public override IEnumerator ExecuteRoutine()
    {
        // Snapshot the original position to center the attack
        Vector3 burstCenter = boss.transform.position;
        
        Telegraph t = Instantiate(boss.telegraphPrefab, burstCenter, Quaternion.identity);
        t.ConfigureCircle(backstepRadius, 30);
        Destroy(t.gameObject, backstepTelegraphTime + 0.1f); // Destroy slightly after telegraph time to ensure it disappears

        // Perform backstep immediately while telegraph is charging
        Vector3 backstepTarget = boss.transform.position - (boss.transform.forward * backstepDistance);
        
        float time = 0;
        boss.Animator.SetTrigger(animationTrigger);
        while (time < backstepDuration)
        {
            time += Time.deltaTime;
            boss.transform.position = Vector3.Lerp(burstCenter, backstepTarget, time / backstepDuration);
            // add vertical motion for a more dynamic backstep
            boss.transform.position += Vector3.up * Mathf.Sin((time / backstepDuration) * Mathf.PI) * 0.5f; // small hop effect
            yield return null;
        }

        // Wait the remaining telegraph time
        float remainingTime = backstepTelegraphTime - backstepDuration;
        if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);

        PlayCastSound();
        if (backstepEffectPrefab != null)
        {
            GameObject effect = Instantiate(backstepEffectPrefab, burstCenter, Quaternion.identity);
            Destroy(effect, 2f);
        }

        if (Vector3.Distance(boss.TargetPlayer.position, burstCenter) <= backstepRadius)
        {
            boss.DealSpecialDamage(baseDamage, causesStagger, staggerDuration, causesKnockback, knockbackDistance);
        }
    }
}