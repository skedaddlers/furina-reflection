using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class FocalorsPhase1AI : EnemyAI
{
    [Header("Telegraph")]
    [SerializeField] private Telegraph telegraphPrefab;

    [Header("General Skill Settings")]
    [SerializeField] private float skillCooldown = 5f;
    [SerializeField] private float castWindupTime = 0.5f;
    [SerializeField] private float waitAfterCastTime = 0.5f;

    private float lastSkillTime;
    private bool isCasting = false;

    [Header("Verdict Arc")]
    public bool useVerdictArc = true;
    [SerializeField] private float verdictArcRange = 8f;
    [SerializeField] private float verdictArcAngle = 70f;
    [SerializeField] private int verdictArcSegments = 30;
    [SerializeField] private float verdictArcTelegraphTime = 1.2f;
    [SerializeField] private GameObject verdictArcEffectPrefab;
    [SerializeField] private Vector3 verdictArcEffectRotationOffset;
    public AudioClip verdictArcSound;

    [Header("Ripple Court")]
    public bool useRippleCourt = true;
    [SerializeField] private int rippleCount = 3;
    [SerializeField] private float rippleSpawnRadius = 3f;
    [SerializeField] private float rippleCircleRadius = 3f;
    [SerializeField] private int rippleSegments = 30;
    [SerializeField] private float rippleTelegraphTime = 1f;
    [SerializeField] private GameObject rippleEffectPrefab;
    public AudioClip rippleCourtSound;

    [Header("Judicial Line")]
    public bool useJudicialLine = true;
    [SerializeField] private float lineWidth = 2f;
    [SerializeField] private float lineLength = 10f;
    [SerializeField] private float lineTelegraphTime = 1f;
    [SerializeField] private GameObject lineEffectPrefab;
    [SerializeField] private Vector3 lineEffectOffset;
    public AudioClip judicialLineSound;

    private bool canAct = true;
    protected override void Update()
    {
        if (IsStaggered) return;
        if (player == null || isCasting) return;
        if (!canAct)
        {
            LookAtPlayer();
            return;
        }
        float distance = Vector3.Distance(player.position, transform.position);

        if (!SeePlayer())
        {
            StopChasing();
            return;
        }

        if (CanUseSkill())
        {
            StartCoroutine(PerformRandomSkill());
            return;
        }

        if (distance <= attackRange)
        {
            StopChasing();
            LookAtPlayer();
            AttackPlayer();
        }
        else
        {
            ChasePlayer();
        }
    }

    public void SetCanAct(bool value)
    {
        canAct = value;
        if (!value)
        {
            StopChasing();
            isCasting = false;
            animator.ResetTrigger("Cast");
        }
        return;
    }

    public void SetImmune(bool value)
    {
        var health = GetComponent<Health>();
        if (health != null)
            health.SetImmune(value);
    }

    bool CanUseSkill()
    {
        return Time.time - lastSkillTime >= ScaleAbilityCooldown(skillCooldown);
    }

    IEnumerator PerformRandomSkill()
    {
        isCasting = true;
        StopChasing();
        LookAtPlayer();

        animator.SetTrigger("Cast");
        yield return new WaitForSeconds(castWindupTime);

        List<int> availableSkills = new List<int>();
        if (useVerdictArc) availableSkills.Add(0);
        if (useRippleCourt) availableSkills.Add(1);
        if (useJudicialLine) availableSkills.Add(2);

        if (availableSkills.Count > 0)
        {
            int choice = availableSkills[Random.Range(0, availableSkills.Count)];
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


        yield return new WaitForSeconds(waitAfterCastTime);

        lastSkillTime = Time.time;
        isCasting = false;
    }

    IEnumerator VerdictArc()
    {
        Telegraph t = Instantiate(telegraphPrefab, transform.position, transform.rotation);
        t.ConfigureCone(verdictArcRange, verdictArcAngle, verdictArcSegments);

        Destroy(t.gameObject, verdictArcTelegraphTime + 0.1f); // Destroy slightly after telegraph time to ensure it disappears
        yield return new WaitForSeconds(verdictArcTelegraphTime);
        GameObject effect = Instantiate(verdictArcEffectPrefab, transform.position, transform.rotation * Quaternion.Euler(verdictArcEffectRotationOffset));
        Destroy(effect, 2f);
        if (verdictArcSound != null)
        {
            AudioManager.Instance?.PlaySFXNoOverlap(verdictArcSound);
        }

        if (Vector3.Distance(player.position, transform.position) <= verdictArcRange)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToPlayer);

            if (angle <= verdictArcAngle * 0.5f)
            {
                DealSpecialDamage();
            }
        }
    }

    IEnumerator RippleCourt()
    {
        Telegraph[] circles = new Telegraph[rippleCount];

        for (int i = 0; i < rippleCount; i++)
        {
            Vector3 randomPos = player.position + Random.insideUnitSphere * rippleSpawnRadius;
            randomPos.y = transform.position.y;

            Telegraph t = Instantiate(telegraphPrefab, randomPos, Quaternion.identity);
            t.ConfigureCircle(rippleCircleRadius, rippleSegments);
            circles[i] = t;
            Destroy(t.gameObject, rippleTelegraphTime + 0.1f); // Destroy slightly after telegraph time to ensure it disappears
        }

        yield return new WaitForSeconds(rippleTelegraphTime);
        if (rippleCourtSound != null)
        {
            AudioManager.Instance?.PlaySFXNoOverlap(rippleCourtSound);
        }
        foreach (var t in circles)
        {
            GameObject effect = Instantiate(rippleEffectPrefab, t.transform.position, Quaternion.identity);
            Destroy(effect, 2f);
            if (Vector3.Distance(player.position, t.transform.position) <= rippleCircleRadius)
            {
                DealSpecialDamage();
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
        GameObject effect = Instantiate(lineEffectPrefab, lineOrigin + lineEffectOffset, lineRotation);
        Destroy(effect, 2f);
        if (judicialLineSound != null)
        {
            AudioManager.Instance?.PlaySFXNoOverlap(judicialLineSound);
        }

        Vector3 toPlayer = player.position - lineOrigin;
        toPlayer.y = 0f;
        Vector3 local = Quaternion.Inverse(lineRotation) * toPlayer;
        float halfWidth = lineWidth * 0.5f;

        if (local.z >= 0f && local.z <= lineLength && Mathf.Abs(local.x) <= halfWidth)
        {
            DealSpecialDamage();
        }
    }

    public void DealSpecialDamage()
    {
        if (IsStaggered) return;
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
            damage,
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

    protected override void OnStaggerStarted()
    {
        isCasting = false;
        if (animator != null)
        {
            animator.ResetTrigger("Cast");
            animator.SetFloat("WalkSpeed", 0f);
        }
    }
}
