using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DDAMAPEKitFramework;

public class BossCloneAI : EnemyAI
{
    public List<CloneSequence> sequences;

    public float mirrorChance = 0.25f;
    public float cooldownTime = 0.8f;
    public Telegraph telegraphPrefab;
    public float castSoundVolume = 1f;

    [Header("Verdict Arc")]
    public bool enableVerdictArc = true;
    [SerializeField] private float verdictArcDamage = 12f;
    [SerializeField] private float verdictArcRange = 8f;
    [SerializeField] private float verdictArcAngle = 70f;
    [SerializeField] private int verdictArcSegments = 30;
    [SerializeField] private float verdictArcTelegraphTime = 1.2f;
    [SerializeField] private GameObject verdictArcEffectPrefab;
    [SerializeField] private Vector3 verdictArcEffectRotationOffset;
    [SerializeField] private AudioClip verdictArcSound;

    [Header("Ripple Court")]
    public bool enableRippleCourt = true;
    [SerializeField] private float rippleDamage = 10f;
    [SerializeField] private int rippleCount = 3;
    [SerializeField] private float rippleSpawnRadius = 3f;
    [SerializeField] private float rippleCircleRadius = 3f;
    [SerializeField] private int rippleSegments = 30;
    [SerializeField] private float rippleTelegraphTime = 1f;
    [SerializeField] private GameObject rippleEffectPrefab;
    [SerializeField] private AudioClip rippleCourtSound;

    [Header("Judicial Line")]
    public bool enableJudicialLine = true;
    [SerializeField] private float lineDamage = 15f;
    [SerializeField] private float lineWidth = 2f;
    [SerializeField] private float lineLength = 10f;
    [SerializeField] private float lineTelegraphTime = 1f;
    [SerializeField] private GameObject lineEffectPrefab;
    [SerializeField] private Vector3 lineEffectOffset;
    [SerializeField] private AudioClip judicialLineSound;

    [Header("Self Heal")]
    [SerializeField] private float healAmount = 18f;
    [SerializeField] private float healCastTime = 1.5f;
    [SerializeField] private string healAnimationTrigger = "Cast";
    [SerializeField] private GameObject healEffectPrefab;
    [SerializeField] private float healEffectLifetime = 2f;
    [SerializeField] private AudioClip healSound;

    [Header("Player-Like Skills")]
    [SerializeField] private WaterAspirations waterAspirationsSkill;
    [SerializeField] private AuraOfTheFormerArchon auraOfTheFormerArchonSkill;
    [SerializeField] private SalonSolitaire salonSolitaireSkill;

    [Header("Dodge Settings")]
    [SerializeField] private float dodgeDistance = 3f;
    [SerializeField] private float dodgeReadWindow = 1.1f;
    [SerializeField] private float dodgeThreatRange = 8f;
    [SerializeField] [Range(20f, 180f)] private float dodgeThreatAngle = 110f;

    [Header("Other SFX")]
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip dodgeSound;
    [SerializeField] private AudioClip meleeAttackSound;
    [SerializeField] private AudioClip rangedAttackSound;


    private Dictionary<CloneProfileTag, float> profileDistribution;
    private readonly List<SkillBase> cloneSkillPool = new List<SkillBase>();
    private int strafeDirection = 1;
    private Coroutine cloneLoopRoutine;
    private bool isInSequence = false;

    protected override void Start()
    {
        base.Start();

        LoadProfileDistribution();
        InitializeCloneSkillPool();

        cloneLoopRoutine = StartCoroutine(CloneLoop());
    }

    protected override void Update()
    {
        base.Update();
        if (IsStaggered) return;
        if (player == null || isInSequence) return;
        if (agent != null && !agent.isStopped) return;

        LookAtPlayer();
    }

    void LoadProfileDistribution()
    {
        profileDistribution = new Dictionary<CloneProfileTag, float>();

        var playerModel = DDARuntimeHelper.TryGetActivePlayerModel();
        if (playerModel == null)
            return;

        var dist = playerModel.GetProfileDistribution();

        foreach(var kv in dist)
        {
            if (kv.Key == null || string.IsNullOrWhiteSpace(kv.Key.name))
                continue;

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
            case "Skill Spam": return CloneProfileTag.SkillCaster;
            case "Dodger": return CloneProfileTag.Acrobat;
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
                if (seq != null)
                    yield return ExecuteSequence(seq);
            }

            yield return new WaitForSeconds(ScaleAbilityCooldown(cooldownTime));
        }
    }

    CloneSequence ChooseSequence()
    {
        if (sequences == null || sequences.Count == 0)
            return null;

        LoadProfileDistribution();
        if (profileDistribution == null || profileDistribution.Count == 0)
            return sequences[Random.Range(0, sequences.Count)];

        float totalWeight = 0;

        foreach(var seq in sequences)
        {
            float profileWeight = 0.2f;

            if(profileDistribution.ContainsKey(seq.profileTag))
                profileWeight = profileDistribution[seq.profileTag];

            seq.cachedWeight = seq.baseWeight * profileWeight;

            totalWeight += seq.cachedWeight;
        }

        if (totalWeight <= 0f)
            return sequences[Random.Range(0, sequences.Count)];

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
        if (seq == null)
            yield break;

        isInSequence = true;

        foreach(var action in seq.actions)
        {
            Debug.Log($"Executing clone action: {action.type}");
            yield return ExecuteAction(action);
        }

        isInSequence = false;
    }

    IEnumerator ExecuteAction(CloneAction action)
    {
        switch(action.type)
        {
            case CloneActionType.DashToPlayer:
                StartCoroutine(DashToPlayer(action.duration, action.speed));
                break;

            case CloneActionType.StrafePlayer:
                yield return StrafeAroundPlayer(action.duration, action.speed, 3f);
                yield break;

            case CloneActionType.Retreat:
                yield return RetreatFromPlayer(action.duration, action.speed);
                yield break;

            case CloneActionType.MeleeAttack:
                SnapLookAtPlayer();
                PlaySkillCastSound(meleeAttackSound);
                animator.SetTrigger("Attack");
                break;

            case CloneActionType.RangedAttack:
                SnapLookAtPlayer();
                PlaySkillCastSound(rangedAttackSound);
                animator.SetTrigger("RangedAttack");
                break;

            case CloneActionType.SkillCast:
                SnapLookAtPlayer();
                animator.SetTrigger("Cast");
                TriggerRandomSkill();
                break;

            case CloneActionType.Dodge:
                yield return WaitForPlayerAttackThenDodge(action.duration, action.speed);
                yield break;

            case CloneActionType.Heal:
                yield return PerformHeal(action.duration);
                yield break;
        }

        yield return new WaitForSeconds(action.duration);
    }

    private IEnumerator PerformHeal(float duration)
    {
        if (!string.IsNullOrEmpty(healAnimationTrigger))
            animator.SetTrigger(healAnimationTrigger);

        yield return new WaitForSeconds(healCastTime);

        PlaySkillCastSound(healSound);
        Health health = GetComponent<Health>();
        if (health != null && healAmount > 0f)
            health.Heal(healAmount);

        if (healEffectPrefab != null)
        {
            GameObject fx = Instantiate(healEffectPrefab, transform.position, Quaternion.identity, transform);
            Destroy(fx, healEffectLifetime);
        }

        float remaining = Mathf.Max(0f, duration - healCastTime);
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);
    }

    private IEnumerator DashToPlayer(float duration, float speed)
    {
        if (agent == null || !agent.enabled || player == null) yield break;
        LookAtPlayer();
        agent.speed = ScaleActionSpeed(speed);
        float stopDistance = Mathf.Max(agent.stoppingDistance, Mathf.Max(0.1f, agent.radius));
        float elapsed = 0f;
        animator.SetTrigger("Dash");
        PlaySkillCastSound(dashSound);

        try
        {
            while (elapsed < duration)
            {
                if (player == null || !agent.enabled) yield break;

                agent.SetDestination(player.position);
                if (Vector3.Distance(transform.position, player.position) <= stopDistance)
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            animator.SetTrigger("StopDash");
            RestoreDefaultAgentSpeed();

            if (agent != null && agent.enabled)
                agent.ResetPath();
        }
    }

    public override void RangedAttack()
    {
        if (isStaggered)
            return;

        SnapLookAtPlayer();

        if (projectilePrefab != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            GameObject go = Instantiate(projectilePrefab.gameObject, transform.position + dir * 1f + Vector3.up * 1.5f, Quaternion.LookRotation(dir));
            var proj = go.GetComponent<Projectile>();
            if (proj == null) proj = go.AddComponent<Projectile>();

            proj.Init(dir, this.transform);
        }
        
    }

    IEnumerator StrafeAroundPlayer(float duration, float speed, float radius)
    {
        if (agent == null || !agent.enabled || player == null) yield break;
        float orbitRadius = Mathf.Max(1f, radius);
        float sideStepDistance = Mathf.Max(0.5f, orbitRadius * 0.6f);
        float elapsed = 0f;
        animator.SetFloat("WalkSpeed", 1f);
        int direction = strafeDirection;
        strafeDirection *= -1;
        agent.speed = ScaleActionSpeed(speed);

        try
        {
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
        finally
        {
            RestoreDefaultAgentSpeed();

            if (agent != null && agent.enabled)
                agent.ResetPath();

            animator.SetFloat("WalkSpeed", 0f);
        }
    }

    private IEnumerator RetreatFromPlayer(float duration, float speed)
    {
        if (agent == null || !agent.enabled || player == null) yield break;
        animator.SetFloat("WalkSpeed", 1f);
        agent.speed = ScaleActionSpeed(speed);
        Vector3 retreat = (transform.position - player.position).normalized;
        agent.SetDestination(transform.position + retreat * 4f);

        try
        {
            yield return new WaitForSeconds(duration);
        }
        finally
        {
            RestoreDefaultAgentSpeed();

            if (agent != null && agent.enabled)
                agent.ResetPath();

            animator.SetFloat("WalkSpeed", 0f);
        }
    }

    IEnumerator MirrorPlayerAction()
    {
        isInSequence = true;
        CloneActionType action = PlayerActionTypeOrFallback();

        float delay = Random.Range(0.4f,0.8f);

        yield return new WaitForSeconds(delay);

        CloneAction mirror = new CloneAction();
        Debug.Log("Mirroring player action: " + action);
        mirror.type = action;

        yield return ExecuteAction(mirror);
        isInSequence = false;
    }

    private CloneActionType PlayerActionTypeOrFallback()
    {
        if (PlayerActionTracker.Instance == null)
            return CloneActionType.MeleeAttack;

        return PlayerActionTracker.Instance.lastPlayerAction;
    }

    private void TriggerRandomSkill()
    {
        int choiceCount = 1;
        if (enableVerdictArc) choiceCount++;
        if (enableRippleCourt) choiceCount++;
        if (enableJudicialLine) choiceCount++;

        int pick = Random.Range(0, choiceCount);
        if (TryCastPlayerLikeSkill())
        {
            return;
        }
        else if (enableVerdictArc && pick == 0)
        {
            StartCoroutine(VerdictArc());
        }
        else if (enableRippleCourt && ((enableVerdictArc && pick == 1) || (!enableVerdictArc && pick == 0)))
        {
            StartCoroutine(RippleCourt());
        }
        else if (enableJudicialLine)
        {
            StartCoroutine(JudicialLine());
        }
    }

    private void InitializeCloneSkillPool()
    {
        CleanupCloneSkillPool(endActiveSkills: false);
        AddCloneSkillInstance(waterAspirationsSkill);
        AddCloneSkillInstance(auraOfTheFormerArchonSkill);
        AddCloneSkillInstance(salonSolitaireSkill);
    }

    private void AddCloneSkillInstance(SkillBase sourceSkill)
    {
        if (sourceSkill == null)
            return;

        SkillBase runtimeSkill = Instantiate(sourceSkill);
        runtimeSkill.name = sourceSkill.name + "_CloneRuntime";
        cloneSkillPool.Add(runtimeSkill);
    }

    private bool TryCastPlayerLikeSkill()
    {
        if (cloneSkillPool.Count == 0)
            return false;

        List<int> candidateIndices = new List<int>(cloneSkillPool.Count);
        for (int i = 0; i < cloneSkillPool.Count; i++)
            candidateIndices.Add(i);

        while (candidateIndices.Count > 0)
        {
            int pick = Random.Range(0, candidateIndices.Count);
            int poolIndex = candidateIndices[pick];
            candidateIndices.RemoveAt(pick);

            SkillBase selectedSkill = cloneSkillPool[poolIndex];
            if (selectedSkill == null || !selectedSkill.CanUseSkill(gameObject))
                continue;

            selectedSkill.OnSkillActivate(gameObject);
            return true;
        }

        return false;
    }

    private void CleanupCloneSkillPool(bool endActiveSkills = true)
    {
        foreach (SkillBase skill in cloneSkillPool)
        {
            if (skill == null)
                continue;

            if (endActiveSkills)
            {
                skill.OnSkillEnd(gameObject);
            }

            Destroy(skill);
        }

        cloneSkillPool.Clear();
    }

    IEnumerator VerdictArc()
    {
        SnapLookAtPlayer();
        Telegraph t = Instantiate(telegraphPrefab, transform.position, transform.rotation);
        t.ConfigureCone(verdictArcRange, verdictArcAngle, verdictArcSegments);

        Destroy(t.gameObject, verdictArcTelegraphTime + 0.1f); // Destroy slightly after telegraph time to ensure it disappears
        yield return new WaitForSeconds(verdictArcTelegraphTime);
        GameObject effect = Instantiate(verdictArcEffectPrefab, transform.position, transform.rotation * Quaternion.Euler(verdictArcEffectRotationOffset));
        Destroy(effect, 2f);
        PlaySkillCastSound(verdictArcSound);
        if (Vector3.Distance(player.position, transform.position) <= verdictArcRange)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToPlayer);

            if (angle <= verdictArcAngle * 0.5f)
            {
                DealSpecialDamage(verdictArcDamage);
            }
        }
    }

    IEnumerator RippleCourt()
    {
        Telegraph[] circles = new Telegraph[rippleCount];

        for (int i = 0; i < rippleCount; i++)
        {
            Vector3 randomPos = player.position + new Vector3(Random.Range(-rippleSpawnRadius, rippleSpawnRadius), 0, Random.Range(-rippleSpawnRadius, rippleSpawnRadius));
            randomPos.y = transform.position.y;

            Telegraph t = Instantiate(telegraphPrefab, randomPos, Quaternion.identity);
            t.ConfigureCircle(rippleCircleRadius, rippleSegments);
            circles[i] = t;
            Destroy(t.gameObject, rippleTelegraphTime + 0.1f); // Destroy slightly after telegraph time to ensure it disappears
        }

        yield return new WaitForSeconds(rippleTelegraphTime);
        PlaySkillCastSound(rippleCourtSound);
        if (rippleEffectPrefab != null)
        {
            foreach(var t in circles)
            {
                if (t != null)
                {
                    GameObject effect = Instantiate(rippleEffectPrefab, t.transform.position, Quaternion.identity);
                    Destroy(effect, 2f);
                }
            }
        }

        foreach(var t in circles)
        {
            if (t != null && Vector3.Distance(player.position, t.transform.position) <= rippleCircleRadius)
            {
                DealSpecialDamage(rippleDamage);
            }
        }
    }

    IEnumerator JudicialLine()
    {
        SnapLookAtPlayer();

        Telegraph t = Instantiate(telegraphPrefab, transform.position, transform.rotation);
        t.ConfigureRectangle(lineWidth, lineLength);

        // Snapshot telegraph origin/rotation so damage matches what was shown.
        Vector3 lineOrigin = t.transform.position;
        Quaternion lineRotation = t.transform.rotation;

        Destroy(t.gameObject, lineTelegraphTime + 0.1f); // Destroy slightly after telegraph time to ensure it disappears
        yield return new WaitForSeconds(lineTelegraphTime);
        PlaySkillCastSound(judicialLineSound);
        GameObject effect = Instantiate(lineEffectPrefab, lineOrigin + lineEffectOffset, lineRotation);
        Destroy(effect, 2f);


        Vector3 toPlayer = player.position - lineOrigin;
        toPlayer.y = 0f;
        Vector3 local = Quaternion.Inverse(lineRotation) * toPlayer;
        float halfWidth = lineWidth * 0.5f;

        if (local.z >= 0f && local.z <= lineLength && Mathf.Abs(local.x) <= halfWidth)
        {
            DealSpecialDamage(lineDamage);
        }
    }

    IEnumerator DodgeWindow(float duration, float speed)
    {
        if (agent == null || !agent.enabled || player == null) yield break;

        float elapsed = 0f;
        Vector3 dir = GetSmartDodgeDirection();
        Vector3 dodgeTarget = transform.position + dir * dodgeDistance;
        Health health = GetComponent<Health>();
        agent.speed = ScaleActionSpeed(speed);

        if (health != null)
            health.SetInvulnerable(true);

        try
        {
            while (elapsed < duration)
            {
                if (player == null || !agent.enabled) yield break;
                agent.SetDestination(dodgeTarget);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            animator.SetTrigger("StopDash");

            if (health != null)
                health.SetInvulnerable(false);

            RestoreDefaultAgentSpeed();

            if (agent != null && agent.enabled)
                agent.ResetPath();
        }
    }

    private IEnumerator WaitForPlayerAttackThenDodge(float dodgeDuration, float dodgeSpeed)
    {
        if (agent == null || !agent.enabled || player == null)
            yield break;

        float observedAttackTime = GetLatestPlayerAttackTime();
        float elapsed = 0f;

        while (elapsed < dodgeReadWindow)
        {
            if (player == null || !agent.enabled)
                yield break;

            SnapLookAtPlayer();

            float latestAttackTime = GetLatestPlayerAttackTime();
            if (latestAttackTime > observedAttackTime && IsPlayerAttackThreatening())
            {
                animator.SetTrigger("Dash");
                PlaySkillCastSound(dodgeSound);
                yield return DodgeWindow(dodgeDuration, dodgeSpeed);
                yield break;
            }

            observedAttackTime = Mathf.Max(observedAttackTime, latestAttackTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private float GetLatestPlayerAttackTime()
    {
        PlayerActionTracker tracker = PlayerActionTracker.Instance;
        if (tracker == null)
            return -999f;

        return Mathf.Max(
            tracker.LastMeleeTime,
            Mathf.Max(tracker.LastRangedTime, tracker.LastSkillTime)
        );
    }

    private bool IsPlayerAttackThreatening()
    {
        if (player == null)
            return false;

        Vector3 toClone = transform.position - player.position;
        toClone.y = 0f;

        if (toClone.sqrMagnitude <= 0.0001f)
            return true;

        if (dodgeThreatRange > 0f && toClone.sqrMagnitude > dodgeThreatRange * dodgeThreatRange)
            return false;

        Vector3 playerForward = player.forward;
        playerForward.y = 0f;

        if (playerForward.sqrMagnitude <= 0.0001f)
            return true;

        float angleToClone = Vector3.Angle(playerForward.normalized, toClone.normalized);
        return angleToClone <= dodgeThreatAngle * 0.5f;
    }

    private Vector3 GetSmartDodgeDirection()
    {
        Vector3 awayFromPlayer = transform.position - player.position;
        awayFromPlayer.y = 0f;

        if (awayFromPlayer.sqrMagnitude <= 0.0001f)
        {
            awayFromPlayer = -player.forward;
            awayFromPlayer.y = 0f;
        }

        if (awayFromPlayer.sqrMagnitude <= 0.0001f)
            awayFromPlayer = transform.right;

        awayFromPlayer.Normalize();

        Vector3 sideStep = Vector3.Cross(Vector3.up, awayFromPlayer).normalized;
        if (Random.value < 0.5f)
            sideStep = -sideStep;

        Vector3 dodgeDirection = (sideStep + awayFromPlayer * 0.35f).normalized;
        return dodgeDirection.sqrMagnitude > 0.0001f ? dodgeDirection : awayFromPlayer;
    }

    private void DealSpecialDamage(float damage)
    {
        var health = player.GetComponent<Health>();
        if (health == null) return;

        var playerStats = player.GetComponent<PlayerStats>();
        float defense = playerStats != null ? playerStats.baseDefense : 0f;
        float critChance = enemyStats != null ? enemyStats.critRate : 0f;
        float critMultiplier = enemyStats != null ? enemyStats.critMultiplier : 1f;

        int levelDiff = 0;
        if (enemyStats != null && playerStats != null)
            levelDiff = enemyStats.level - playerStats.level;

        bool didCrit;
        float finalDamage = Helpers.CalculateFinalDamage(
            ScaleSkillDamage(damage),
            defense,
            critChance,
            critMultiplier,
            levelDiff,
            1f,
            out didCrit
        );

        health.TakeDamage(
            finalDamage,
            didCrit,
            DamageSource.Skill,
            applyStagger: true,
            staggerDuration: -1f,
            causesKnockback: true,
            knockbackDistance: 1.1f,
            hitInstigator: transform
        );
    }

    private void PlaySkillCastSound(AudioClip clip)
    {
        if (clip != null)
            AudioManager.Instance?.PlaySFXWithVolume(clip, castSoundVolume);
    }


    protected override void OnStaggerStarted()
    {
        cloneLoopRoutine = null;
    }

    protected override void OnStaggerEnded()
    {
        if (!isActiveAndEnabled || cloneLoopRoutine != null)
            return;

        cloneLoopRoutine = StartCoroutine(CloneLoop());
    }

    void OnDestroy()
    {
        CleanupCloneSkillPool();
    }
}
