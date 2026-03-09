using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossCloneAI : EnemyAI
{
    public List<CloneSequence> sequences;

    public float mirrorChance = 0.25f;
    public float cooldownTime = 0.8f;

    private Dictionary<CloneProfileTag, float> profileDistribution;
    private int strafeDirection = 1;

    protected override void Start()
    {
        base.Start();

        LoadProfileDistribution();

        StartCoroutine(CloneLoop());
    }

    void LoadProfileDistribution()
    {
        profileDistribution = new Dictionary<CloneProfileTag, float>();

        var dist = DDAMAPEKitFramework.DDAMAPEKit.Instance
            .GetPlayerModel()
            .GetProfileDistribution();

        foreach(var kv in dist)
        {
            CloneProfileTag tag = ConvertProfile(kv.Key.name);
            profileDistribution[tag] = kv.Value;
        }
    }

    CloneProfileTag ConvertProfile(string name)
    {
        switch(name)
        {
            case "Melee Lover": return CloneProfileTag.Melee;
            case "Ranged Lover": return CloneProfileTag.Ranged;
            case "SkillCaster": return CloneProfileTag.SkillCaster;
            case "Acrobat": return CloneProfileTag.Acrobat;
            case "Defensive": return CloneProfileTag.Defensive;
        }

        return CloneProfileTag.Melee;
    }

    IEnumerator CloneLoop()
    {
        while(true)
        {
            if(Random.value < mirrorChance)
            {
                yield return MirrorPlayerAction();
            }
            else
            {
                CloneSequence seq = ChooseSequence();
                yield return ExecuteSequence(seq);
            }

            yield return new WaitForSeconds(cooldownTime);
        }
    }

    CloneSequence ChooseSequence()
    {
        float totalWeight = 0;

        foreach(var seq in sequences)
        {
            float profileWeight = 0.2f;

            if(profileDistribution.ContainsKey(seq.profileTag))
                profileWeight = profileDistribution[seq.profileTag];

            seq.cachedWeight = seq.baseWeight * profileWeight;

            totalWeight += seq.cachedWeight;
        }

        float r = Random.value * totalWeight;

        float sum = 0;

        foreach(var seq in sequences)
        {
            sum += seq.cachedWeight;

            if(r <= sum)
                return seq;
        }

        return sequences[0];
    }

    IEnumerator ExecuteSequence(CloneSequence seq)
    {
        foreach(var action in seq.actions)
        {
            Debug.Log($"Executing clone action: {action.type}");
            yield return ExecuteAction(action);
        }
    }

    IEnumerator ExecuteAction(CloneAction action)
    {
        switch(action.type)
        {
            case CloneActionType.DashToPlayer:
                agent.speed = action.speed;
                agent.SetDestination(player.position);
                break;

            case CloneActionType.StrafePlayer:
                yield return StrafeAroundPlayer(action.duration, action.speed, 3f);
                yield break;

            case CloneActionType.Retreat:

                Vector3 retreat = (transform.position - player.position).normalized;
                agent.SetDestination(transform.position + retreat * 4f);

                break;

            case CloneActionType.MeleeAttack:
                animator.SetTrigger("Attack");
                break;

            case CloneActionType.RangedAttack:
                animator.SetTrigger("RangedAttack");
                break;

            case CloneActionType.SkillCast:
                animator.SetTrigger("Cast");
                break;

            case CloneActionType.Dodge:
                animator.SetTrigger("Dodge");
                break;
        }

        yield return new WaitForSeconds(action.duration);
    }

    IEnumerator StrafeAroundPlayer(float duration, float speed, float radius)
    {
        float orbitRadius = Mathf.Max(1f, radius);
        float sideStepDistance = Mathf.Max(0.5f, orbitRadius * 0.6f);
        float elapsed = 0f;

        int direction = strafeDirection;
        strafeDirection *= -1;

        agent.speed = speed > 0f ? speed : movementSpeed;

        while (elapsed < duration)
        {
            if (player == null || !agent.enabled) yield break;

            Vector3 radial = transform.position - player.position;
            radial.y = 0f;
            if (radial.sqrMagnitude <= 0.0001f)
            {
                radial = transform.right;
            }

            radial = radial.normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, radial).normalized * direction;
            Vector3 target = player.position + radial * orbitRadius + tangent * sideStepDistance;

            agent.SetDestination(target);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator MirrorPlayerAction()
    {
        CloneActionType action = PlayerActionTracker.Instance.lastPlayerAction;

        float delay = Random.Range(0.4f,0.8f);

        yield return new WaitForSeconds(delay);

        CloneAction mirror = new CloneAction();
        Debug.Log("Mirroring player action: " + action);
        mirror.type = action;

        yield return ExecuteAction(mirror);
    }
}
