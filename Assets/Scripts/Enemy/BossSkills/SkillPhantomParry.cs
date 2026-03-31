using UnityEngine;
using System.Collections;

public class SkillPhantomParry : BossSkill
{
    [Header("Parry Stance")]
    [SerializeField] private float stanceDuration = 1.25f;
    [SerializeField] private bool immuneDuringStance = true;
    [SerializeField] private string stanceBool = "ParryStance";
    [SerializeField] private float triggerRange = 3f;
    [SerializeField] private float triggerAngle = 140f;
    [SerializeField] private float meleeRegisterWindow = 0.25f;

    [Header("Counter Cone")]
    [SerializeField] private float counterRange = 4.5f;
    [SerializeField] private float counterAngle = 70f;
    [SerializeField] private int counterSegments = 20;
    [SerializeField] private float counterTelegraphTime = 0.2f;
    [SerializeField] private GameObject counterEffectPrefab;
    [SerializeField] private string counterAnimationTrigger = "ParryCounter";

    public override IEnumerator ExecuteRoutine()
    {
        if (boss == null || boss.TargetPlayer == null) yield break;

        float stanceStartTime = Time.time;
        bool didCounter = false;

        if (immuneDuringStance)
            boss.SetImmune(true);

        if (!string.IsNullOrEmpty(stanceBool))
            boss.Animator.SetBool(stanceBool, true);

        float elapsed = 0f;
        while (elapsed < stanceDuration)
        {
            if (DidPlayerMeleeDuringStance(stanceStartTime))
            {
                didCounter = true;
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!string.IsNullOrEmpty(stanceBool))
            boss.Animator.SetBool(stanceBool, false);

        if (immuneDuringStance)
            boss.SetImmune(false);

        if (!didCounter) yield break;

        boss.LookAtPlayer();
        if (!string.IsNullOrEmpty(counterAnimationTrigger))
            boss.Animator.SetTrigger(counterAnimationTrigger);

        Telegraph t = Instantiate(boss.telegraphPrefab, boss.transform.position, boss.transform.rotation);
        t.ConfigureCone(counterRange, counterAngle, counterSegments, counterTelegraphTime);
        Destroy(t.gameObject, counterTelegraphTime + 0.1f);
        yield return new WaitForSeconds(counterTelegraphTime);
        PlayCastSound();
        if (counterEffectPrefab != null)
        {
            GameObject fx = Instantiate(counterEffectPrefab, boss.transform.position, boss.transform.rotation);
            Destroy(fx, 1f);
        }

        if (IsTargetInsideCone(counterRange, counterAngle))
            boss.DealSpecialDamage(baseDamage, causesStagger, staggerDuration, causesKnockback, knockbackDistance);
    }

    private bool DidPlayerMeleeDuringStance(float stanceStartTime)
    {
        if (boss.TargetPlayer == null) return false;

        PlayerActionTracker tracker = PlayerActionTracker.Instance;
        if (tracker == null) return false;
        if (tracker.LastMeleeTime <= stanceStartTime) return false;
        if (Time.time - tracker.LastMeleeTime > meleeRegisterWindow) return false;

        Vector3 toBoss = boss.transform.position - boss.TargetPlayer.position;
        toBoss.y = 0f;
        if (toBoss.sqrMagnitude > triggerRange * triggerRange) return false;

        Vector3 playerForward = boss.TargetPlayer.forward;
        playerForward.y = 0f;
        if (playerForward.sqrMagnitude <= 0.0001f) return false;

        float angleToBoss = Vector3.Angle(playerForward.normalized, toBoss.normalized);
        return angleToBoss <= triggerAngle * 0.5f;
    }

    private bool IsTargetInsideCone(float range, float angle)
    {
        if (boss.TargetPlayer == null) return false;

        Vector3 toPlayer = boss.TargetPlayer.position - boss.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > range * range) return false;

        float playerAngle = Vector3.Angle(boss.transform.forward, toPlayer.normalized);
        return playerAngle <= angle * 0.5f;
    }
}
