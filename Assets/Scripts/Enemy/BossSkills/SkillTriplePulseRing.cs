using UnityEngine;
using System.Collections;

public class SkillTriplePulseRing : BossSkill
{
    [Header("Triple Pulse Ring Settings")]
    [SerializeField] private float pulseInitialRadius = 2f;
    [SerializeField] private float pulseRadiusIncrement = 1f;
    [SerializeField] private float pulseDelayBetween = 0.8f;
    [SerializeField] private GameObject pulseEffectPrefab;

    public override IEnumerator ExecuteRoutine()
    {
        for (int i = 0; i < 3; i++)
        {
            float currentRadius = pulseInitialRadius + (i * pulseRadiusIncrement);
            
            // Snapshot the player's position at the start of THIS specific pulse
            Vector3 pulseCenter = boss.TargetPlayer.position; 

            Telegraph t = Instantiate(boss.telegraphPrefab, pulseCenter, Quaternion.identity);
            t.ConfigureCircle(currentRadius, 40);
            Destroy(t.gameObject, pulseDelayBetween + 0.1f); // Destroy slightly after delay to ensure it disappears

            yield return new WaitForSeconds(pulseDelayBetween);

            if (pulseEffectPrefab != null)
            {
                GameObject effect = Instantiate(pulseEffectPrefab, pulseCenter, Quaternion.identity);
                // Scale effect to visually match the growing radius
                effect.transform.localScale = new Vector3(currentRadius, currentRadius, currentRadius);
                Destroy(effect, 2f);
            }

            if (Vector3.Distance(boss.TargetPlayer.position, pulseCenter) <= currentRadius)
            {
                boss.DealSpecialDamage();
            }

            Destroy(t.gameObject);
        }
    }
}