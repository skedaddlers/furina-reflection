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
    [SerializeField] private float verdictArcDamage = 12f;
    [SerializeField] private float verdictArcRange = 8f;
    [SerializeField] private float verdictArcAngle = 70f;
    [SerializeField] private int verdictArcSegments = 30;
    [SerializeField] private float verdictArcTelegraphTime = 1.2f;
    [SerializeField] private GameObject verdictArcEffectPrefab;
    [SerializeField] private Vector3 verdictArcEffectRotationOffset;
    [SerializeField] private AudioClip verdictArcSound;

    [Header("Ripple Court")]
    [SerializeField] private float rippleDamage = 10f;
    [SerializeField] private int rippleCount = 3;
    [SerializeField] private float rippleSpawnRadius = 3f;
    [SerializeField] private float rippleCircleRadius = 3f;
    [SerializeField] private int rippleSegments = 30;
    [SerializeField] private float rippleTelegraphTime = 1f;
    [SerializeField] private GameObject rippleEffectPrefab;
    [SerializeField] private AudioClip rippleCourtSound;

    [Header("Judicial Line")]
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
        if (IsStaggered) return;
        if (player == null || isInSequence) return;

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
                agent.speed = ScaleActionSpeed(action.speed);
                Vector3 retreat = (transform.position - player.position).normalized;
                agent.SetDestination(transform.position + retreat * 4f);

                break;

            case CloneActionType.MeleeAttack:
                PlaySkillCastSound(meleeAttackSound);
                animator.SetTrigger("Attack");
                break;

            case CloneActionType.RangedAttack:
                PlaySkillCastSound(rangedAttackSound);
                animator.SetTrigger("RangedAttack");
                break;

            case CloneActionType.SkillCast:
                animator.SetTrigger("Cast");
                TriggerRandomSkill();
                break;

            case CloneActionType.Dodge:
                animator.SetTrigger("Dash");
                StartCoroutine(DodgeWindow(action.duration, action.speed));
                PlaySkillCastSound(dodgeSound);
                break;

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
        float originalSpeed = agent.speed;
        agent.speed = ScaleActionSpeed(speed);
        float stopDistance = Mathf.Max(agent.stoppingDistance, Mathf.Max(0.1f, agent.radius));
        float elapsed = 0f;
        animator.SetTrigger("Dash");
        PlaySkillCastSound(dashSound);
        while (elapsed < duration)
        {
            if (player == null || !agent.enabled) yield break;

            agent.SetDestination(player.position);
            if (Vector3.Distance(transform.position, player.position) <= stopDistance)
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        animator.SetTrigger("StopDash");
        agent.speed = originalSpeed;

        if (agent.enabled)
            agent.ResetPath();
    }

    public override void RangedAttack()
    {
        if (isStaggered)
            return;

        LookAtPlayer();

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
        float orbitRadius = Mathf.Max(1f, radius);
        float sideStepDistance = Mathf.Max(0.5f, orbitRadius * 0.6f);
        float elapsed = 0f;

        int direction = strafeDirection;
        strafeDirection *= -1;

        agent.speed = ScaleActionSpeed(speed);

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
        if (TryCastPlayerLikeSkill())
            return;

        // Legacy fallback (kept intentionally)
        int choice = Random.Range(0, 3);

        switch (choice)
            {
                case 0:
                    StartCoroutine(VerdictArc());
                    break;
                case 1:
                    StartCoroutine(RippleCourt());
                    break;
                case 2:
                    StartCoroutine(JudicialLine());
                    break;
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
        LookAtPlayer();

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
        float elapsed = 0f;
        // pick random direction  from 90 - 270 degrees relative to player
        float angle = Random.Range(90f, 270f);
        Vector3 dir = Quaternion.Euler(0, angle, 0) * (player.position - transform.position).normalized;
        Vector3 dodgeTarget = transform.position + dir * dodgeDistance;
        float originalSpeed = agent.speed;
        agent.speed = ScaleActionSpeed(speed);
        while (elapsed < duration)
        {
            if (player == null) yield break;
            agent.SetDestination(dodgeTarget);
            GetComponent<Health>().SetInvulnerable(true);
            elapsed += Time.deltaTime;
            yield return null;
        }
        animator.SetTrigger("StopDash");
        GetComponent<Health>().SetInvulnerable(false);
        agent.speed = originalSpeed;
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
            AudioManager.Instance.PlaySFXWithVolume(clip, castSoundVolume);
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
