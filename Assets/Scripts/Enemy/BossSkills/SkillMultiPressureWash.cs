using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SkillMultiPressureWash : BossSkill
{
    [Header("Blast Shape")]
    [SerializeField] private float blastWidth = 2.5f;
    [SerializeField] private float blastLength = 10f;
    [SerializeField] private float telegraphTime = 0.35f;
    [SerializeField] private GameObject blastEffectPrefab;
    [SerializeField] private Vector3 blastEffectOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Pattern")]
    [SerializeField] private int blastCount = 5;
    [SerializeField] private int repetitions = 3;
    [SerializeField] private float angleBetweenBlasts = 90f;
    [SerializeField] private float startAngleOffset = 0f;
    [SerializeField] private float angleIncrement = 25f;
    [SerializeField] private float delayBetweenBlasts = 0.15f;

    public override IEnumerator ExecuteRoutine()
    {
        if (boss == null || boss.TargetPlayer == null) yield break;

        float currentAngle = startAngleOffset;

        for (int i = 0; i < repetitions; i++)
        {
            boss.LookAtPlayer();
            Vector3 origin = boss.transform.position;
            Quaternion rotation = boss.transform.rotation * Quaternion.Euler(0f, currentAngle, 0f);
            Quaternion startingRotation = rotation;
            List<Telegraph> telegraphs = new List<Telegraph>();
            for (int j = 0; j < blastCount; j++)
            {
                Telegraph t = Instantiate(boss.telegraphPrefab, origin, startingRotation);
                t.ConfigureRectangle(blastWidth, blastLength, telegraphTime);
                telegraphs.Add(t);
                Destroy(t.gameObject, telegraphTime + 0.1f);
                startingRotation *= Quaternion.Euler(0f, angleBetweenBlasts, 0f);
            }

            boss.Animator.SetTrigger(animationTrigger);

            yield return new WaitForSeconds(telegraphTime);            
            PlayCastSound();
            Quaternion blastRotation = rotation;
            for (int j = 0; j < blastCount; j++)
            {
                Vector3 spawnPos =
                    origin +
                    blastRotation * Vector3.forward +
                    blastEffectOffset;
                if (blastEffectPrefab != null)
                {
                    GameObject fx = Instantiate(blastEffectPrefab, spawnPos, blastRotation);
                    Destroy(fx, telegraphTime + 0.1f);
                }
                if (IsTargetInsideRectangle(telegraphs[j].transform.position, telegraphs[j].transform.rotation))
                    boss.DealSpecialDamage(baseDamage, causesStagger, staggerDuration, causesKnockback, knockbackDistance);
                
                blastRotation *= Quaternion.Euler(0f, angleBetweenBlasts, 0f);
            }

            currentAngle += angleIncrement;
            if (i < repetitions - 1 && delayBetweenBlasts > 0f)
                yield return new WaitForSeconds(delayBetweenBlasts);
        }
    }

    private bool IsTargetInsideRectangle(Vector3 origin, Quaternion rotation)
    {
        if (boss.TargetPlayer == null) return false;

        Vector3 toPlayer = boss.TargetPlayer.position - origin;
        toPlayer.y = 0f;
        Vector3 local = Quaternion.Inverse(rotation) * toPlayer;
        float halfWidth = blastWidth * 0.5f;

        return local.z >= 0f && local.z <= blastLength && Mathf.Abs(local.x) <= halfWidth;
    }
}
