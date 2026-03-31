using UnityEngine;
using System.Collections;

public class SkillEchoStrike : BossSkill
{
    [Header("Cone Shape")]
    [SerializeField] private float coneRange = 7f;
    [SerializeField] private float coneAngle = 70f;
    [SerializeField] private int coneSegments = 24;

    [Header("Timing")]
    [SerializeField] private float firstTelegraphTime = 0.75f;
    [SerializeField] private float ghostDelay = 1f;
    [SerializeField] private float ghostTelegraphTime = 0.35f;

    [Header("Animation")]
    [SerializeField] private string ghostAnimationTrigger = "EchoStrikeGhost";

    [Header("Effects")]
    [SerializeField] private GameObject firstStrikeEffectPrefab;
    [SerializeField] private GameObject ghostStrikeEffectPrefab;
    [SerializeField] private float strikeEffectLifetime = 2f;
    [SerializeField] private Vector3 strikeEffectRotationOffset = Vector3.zero;

    [Header("Hit Rules")]
    [SerializeField] private bool damageOnFirstStrike = true;
    [SerializeField] private bool damageOnGhostStrike = true;
    [SerializeField] private bool lockDirectionAtCast = true;

    public override IEnumerator ExecuteRoutine()
    {
        if (boss == null || boss.TargetPlayer == null) yield break;

        Vector3 origin = boss.transform.position;
        Vector3 toPlayer = boss.TargetPlayer.position - origin;
        toPlayer.y = 0f;
        Vector3 forward = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : boss.transform.forward;
        Quaternion strikeRotation = Quaternion.LookRotation(forward, Vector3.up);

        if (lockDirectionAtCast)
            boss.transform.rotation = strikeRotation;

        Telegraph firstTelegraph = SpawnConeTelegraph(origin, strikeRotation, firstTelegraphTime);

        boss.Animator.SetTrigger(animationTrigger);
        if (firstTelegraphTime > 0f)
            yield return new WaitForSeconds(firstTelegraphTime);

        if (firstTelegraph != null)
            Destroy(firstTelegraph.gameObject);
            
        PlayCastSound();
        TriggerStrikeEffect(firstStrikeEffectPrefab, origin, strikeRotation);
        if (damageOnFirstStrike && IsTargetInsideCone(origin, strikeRotation))
            boss.DealSpecialDamage(baseDamage, causesStagger, staggerDuration, causesKnockback, knockbackDistance);

        if (ghostDelay > 0f)
            yield return new WaitForSeconds(ghostDelay);

        if (!lockDirectionAtCast && boss.TargetPlayer != null)
        {
            Vector3 ghostToPlayer = boss.TargetPlayer.position - origin;
            ghostToPlayer.y = 0f;
            if (ghostToPlayer.sqrMagnitude > 0.0001f)
            {
                strikeRotation = Quaternion.LookRotation(ghostToPlayer.normalized, Vector3.up);
                boss.transform.rotation = strikeRotation;
            }
        }

        if (!string.IsNullOrEmpty(ghostAnimationTrigger))
            boss.Animator.SetTrigger(ghostAnimationTrigger);

        Telegraph ghostTelegraph = SpawnConeTelegraph(origin, strikeRotation, ghostTelegraphTime);

        if (ghostTelegraphTime > 0f)
            yield return new WaitForSeconds(ghostTelegraphTime);

        if (ghostTelegraph != null)
            Destroy(ghostTelegraph.gameObject);

        PlayCastSound();
        TriggerStrikeEffect(ghostStrikeEffectPrefab, origin, strikeRotation);
        if (damageOnGhostStrike && IsTargetInsideCone(origin, strikeRotation))
            boss.DealSpecialDamage(baseDamage, causesStagger, staggerDuration, causesKnockback, knockbackDistance);
    }

    private Telegraph SpawnConeTelegraph(Vector3 origin, Quaternion rotation, float duration)
    {
        if (boss.telegraphPrefab == null) return null;

        Telegraph telegraph = Instantiate(boss.telegraphPrefab, origin, rotation);
        telegraph.ConfigureCone(coneRange, coneAngle, coneSegments, duration);
        Destroy(telegraph.gameObject, duration + 0.1f);
        return telegraph;
    }

    private void TriggerStrikeEffect(GameObject effectPrefab, Vector3 origin, Quaternion rotation)
    {
        if (effectPrefab == null) return;
        Quaternion finalRotation = rotation * Quaternion.Euler(strikeEffectRotationOffset);
        GameObject effect = Instantiate(effectPrefab, origin, finalRotation);
        Destroy(effect, strikeEffectLifetime);
    }

    private bool IsTargetInsideCone(Vector3 origin, Quaternion rotation)
    {
        if (boss.TargetPlayer == null) return false;

        Vector3 toTarget = boss.TargetPlayer.position - origin;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > coneRange * coneRange) return false;
        if (toTarget.sqrMagnitude <= 0.0001f) return true;

        float angleToTarget = Vector3.Angle(rotation * Vector3.forward, toTarget.normalized);
        return angleToTarget <= coneAngle * 0.5f;
    }
}
