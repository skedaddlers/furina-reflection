using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class SkillHydroJudgement : BossSkill
{
    [Header("Telegraph")]
    [SerializeField] private float telegraphRadius = 2.6f;
    [SerializeField] private int telegraphSegments = 36;
    [SerializeField] private float telegraphTime = 0.9f;

    [Header("Jump")]
    [SerializeField] private float jumpDuration = 0.55f;
    [SerializeField] private float jumpHeight = 2.5f;
    [SerializeField] private float navMeshSampleRadius = 2.5f;

    [Header("Impact")]
    [SerializeField] private float impactRadius = 2.6f;
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float impactEffectLifetime = 2f;

    public override IEnumerator ExecuteRoutine()
    {
        if (boss == null || boss.TargetPlayer == null) yield break;

        Vector3 landingPoint = ResolveLandingPoint();

        Telegraph telegraph = null;
        if (boss.telegraphPrefab != null)
        {
            telegraph = Instantiate(boss.telegraphPrefab, landingPoint, Quaternion.identity);
            telegraph.ConfigureCircle(telegraphRadius, telegraphSegments, telegraphTime);
            Destroy(telegraph.gameObject, telegraphTime + 0.1f);
        }

        boss.Animator.SetTrigger(animationTrigger);
        yield return new WaitForSeconds(telegraphTime);

        if (telegraph != null)
            Destroy(telegraph.gameObject);

        yield return JumpToPoint(landingPoint);

        if (impactEffectPrefab != null)
        {
            GameObject impactFx = Instantiate(impactEffectPrefab, landingPoint, Quaternion.identity);
            Destroy(impactFx, impactEffectLifetime);
        }

        if (IsPlayerInsideImpact(landingPoint))
            boss.DealSpecialDamage();
    }

    private Vector3 ResolveLandingPoint()
    {
        Vector3 playerPos = boss.TargetPlayer.position;
        Vector3 desired = new Vector3(playerPos.x, boss.transform.position.y, playerPos.z);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(desired, out hit, navMeshSampleRadius, NavMesh.AllAreas))
            return hit.position;

        return desired;
    }

    private IEnumerator JumpToPoint(Vector3 landingPoint)
    {
        Vector3 start = boss.transform.position;
        Vector3 end = landingPoint;
        end.y = start.y;

        Vector3 horizontal = end - start;
        horizontal.y = 0f;
        if (horizontal.sqrMagnitude > 0.0001f)
            boss.transform.rotation = Quaternion.LookRotation(horizontal.normalized, Vector3.up);

        if (jumpDuration <= 0f)
        {
            boss.transform.position = end;
            SyncNavAgent();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jumpDuration);
            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
            boss.transform.position = pos;
            yield return null;
        }

        boss.transform.position = end;
        SyncNavAgent();
    }

    private bool IsPlayerInsideImpact(Vector3 center)
    {
        if (boss.TargetPlayer == null) return false;

        Vector3 delta = boss.TargetPlayer.position - center;
        delta.y = 0f;
        return delta.sqrMagnitude <= impactRadius * impactRadius;
    }

    private void SyncNavAgent()
    {
        NavMeshAgent nav = boss.GetComponent<NavMeshAgent>();
        if (nav != null && nav.enabled)
            nav.Warp(boss.transform.position);
    }
}
