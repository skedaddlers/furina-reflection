using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class SkillHydroDash : BossSkill
{
    [Header("Hydro Dash Settings")]
    [SerializeField] private float dashWidth = 3f;
    [SerializeField] private float dashLength = 5f;
    [SerializeField] private float dashTelegraphTime = 1.2f;
    [SerializeField] private float dashDuration = 0.6f;
    [SerializeField] private GameObject dashEffectPrefab;
    [SerializeField] private string endAnimationTrigger = "StopDash";

    public override IEnumerator ExecuteRoutine()
    {
        if (boss.TargetPlayer == null) yield break;
        
        // 1. Snapshot dash origin and direction once so telegraph/movement/hitbox stay in sync.
        Vector3 startPos = boss.transform.position;
        Vector3 toPlayer = boss.TargetPlayer.position - startPos;
        toPlayer.y = 0f;
        Vector3 dashDirection = toPlayer.sqrMagnitude > 0.0001f
            ? toPlayer.normalized
            : boss.transform.forward;
        Quaternion dashRotation = Quaternion.LookRotation(dashDirection, Vector3.up);
        boss.transform.rotation = dashRotation;

        Telegraph t = Instantiate(boss.telegraphPrefab, startPos, dashRotation);
        t.ConfigureRectangle(dashWidth, dashLength);
        Destroy(t.gameObject, dashTelegraphTime + 0.1f); // Destroy slightly after telegraph time to ensure it disappears

        yield return new WaitForSeconds(dashTelegraphTime);

        // 2. Perform Dash Movement & Effects
        Vector3 endPos = startPos + (dashDirection * dashLength);
        if (dashEffectPrefab != null)
        {
            GameObject effect = Instantiate(dashEffectPrefab, boss.transform.position, dashRotation);
            Destroy(effect, 2f);
        }

        float time = 0;
        boss.Animator.SetTrigger(animationTrigger);
        PlayCastSound();
        while (time < dashDuration)
        {
            time += Time.deltaTime;
            boss.transform.position = Vector3.Lerp(startPos, endPos, time / dashDuration);
            yield return null;
        }
        boss.Animator.SetTrigger(endAnimationTrigger);

        // Keep NavMeshAgent state aligned after manual transform movement.
        NavMeshAgent nav = boss.GetComponent<NavMeshAgent>();
        if (nav != null && nav.enabled)
        {
            nav.Warp(boss.transform.position);
        }

         // 2. Check Damage
        Vector3 toPlayerNow = boss.TargetPlayer.position - startPos;
        toPlayerNow.y = 0f;
        Vector3 local = Quaternion.Inverse(dashRotation) * toPlayerNow;
        float halfWidth = dashWidth * 0.5f;

        if (local.z >= 0f && local.z <= dashLength && Mathf.Abs(local.x) <= halfWidth)
        {
            boss.DealSpecialDamage(baseDamage, causesStagger, staggerDuration, causesKnockback, knockbackDistance);
        }
    }
}
