using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Static-config final boss controller.
/// - Default ranged attacker (uses EnemyAI projectile + animator trigger).
/// - Can cast player skills (SkillBase) driven by phase config.
/// - Phases are based on HP percentage thresholds, configured in the prefab.
/// </summary>
public class FinalBossController : EnemyAI
{
    [Header("Boss Combat")]
    [Tooltip("Seconds between basic ranged attacks when in range.")]
    public float attackCheckInterval = 0.1f;

    [Header("Skill Casting")]
    [Tooltip("Fallback skills castable in any phase.")]
    public List<SkillBase> baseSkills = new List<SkillBase>();
    [Tooltip("Default interval between casts if phase doesn't override.")]
    public float defaultSkillInterval = 8f;

    [Serializable]
    public class BossPhase
    {
        public string name = "Phase";
        [Range(0f, 1f)] public float hpThreshold = 0.7f; // Trigger when current HP <= threshold * maxHP
        public List<SkillBase> skills = new List<SkillBase>();
        public float damageMultiplier = 1f;
        public float attackSpeedMultiplier = 1f;   // >1 = faster attacks (shorter cooldown)
        public float moveSpeedMultiplier = 1f;
        public float skillInterval = -1f;          // -1 uses defaultSkillInterval
    }

    [Header("Phases (static config)")]
    public List<BossPhase> phases = new List<BossPhase>();

    private Health _health;
    private float _nextAttackCheck;
    private float _nextSkillTime;
    private int _currentPhaseIndex = -1;

    // Baseline values so phase multipliers stack on top of DDA-adjusted stats
    private float _baseDamage;
    private float _baseAttackCooldown;
    private float _baseAgentSpeed;

    protected override void Awake()
    {
        base.Awake();
        isRanged = true; // default ranged attacker
    }

    void Start()
    {
        _health = GetComponent<Health>();
        if (_health != null)
        {
            _health.onHealthChanged += OnHealthChanged;
        }

        var nav = GetComponent<NavMeshAgent>();
        _baseDamage = damage;
        _baseAttackCooldown = attackCooldown;
        _baseAgentSpeed = nav != null ? nav.speed : 3f;

        // Evaluate starting phase (in case boss spawns with reduced HP)
        EvaluatePhase(_health != null ? _health.GetCurrentHealth() : 0f, _health != null ? _health.maxHealth : 1f);

        _nextSkillTime = Time.time + defaultSkillInterval;
    }

    protected override void Update()
    {
        base.Update();
        if (IsStaggered) return;
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= detectionRange)
        {
            if (dist > attackRange)
            {
                ChasePlayer();
            }
            else
            {
                StopChasing();
                LookAtPlayer();
                HandleBasicAttack();
            }

            HandleSkillCasting();
        }
        else
        {
            StopChasing();
        }
    }

    private void HandleBasicAttack()
    {
        if (Time.time >= _nextAttackCheck)
        {
            AttackPlayer(); // EnemyAI handles ranged/melee triggers
            _nextAttackCheck = Time.time + attackCheckInterval;
        }
    }

    private void HandleSkillCasting()
    {
        if (Time.time < _nextSkillTime) return;

        var skill = GetSkillForCurrentPhase();
        if (skill != null)
        {
            base.animator.SetTrigger("CastSkill");
            skill.OnSkillActivate(gameObject);
        }

        _nextSkillTime = Time.time + GetCurrentSkillInterval();
    }

    private SkillBase GetSkillForCurrentPhase()
    {
        var list = baseSkills;
        if (_currentPhaseIndex >= 0 && _currentPhaseIndex < phases.Count && phases[_currentPhaseIndex].skills.Count > 0)
        {
            list = phases[_currentPhaseIndex].skills;
        }

        if (list == null || list.Count == 0) return null;
        int idx = UnityEngine.Random.Range(0, list.Count);
        return list[idx];
    }

    private float GetCurrentSkillInterval()
    {
        if (_currentPhaseIndex >= 0 && _currentPhaseIndex < phases.Count)
        {
            float overrideInterval = phases[_currentPhaseIndex].skillInterval;
            if (overrideInterval > 0f) return overrideInterval;
        }
        return defaultSkillInterval;
    }

    private void OnHealthChanged(float current, float max)
    {
        EvaluatePhase(current, max);
    }

    private void EvaluatePhase(float current, float max)
    {
        if (max <= 0f) return;
        float pct = current / max;

        int targetIndex = -1;
        float lowThreshold = float.MaxValue;
        for (int i = 0; i < phases.Count; i++)
        {
            float t = phases[i].hpThreshold;
            if (pct <= t && t < lowThreshold)
            {
                lowThreshold = t;
                targetIndex = i;
            }
        }

        Debug.Log($"[FinalBoss] Evaluated phase index: {targetIndex} (Current HP%: {pct:F2})");

        if (targetIndex != -1 && targetIndex != _currentPhaseIndex)
        {
            EnterPhase(targetIndex);
        }
    }

    private void EnterPhase(int index)
    {
        _currentPhaseIndex = index;
        BossPhase phase = phases[_currentPhaseIndex];

        // Re-apply multipliers relative to baseline
        damage = Mathf.RoundToInt(_baseDamage * Mathf.Max(0.1f, phase.damageMultiplier));
        attackCooldown = Mathf.Max(0.1f, _baseAttackCooldown / Mathf.Max(0.01f, phase.attackSpeedMultiplier));

        if (agent != null)
        {
            movementSpeed = Mathf.Max(0.5f, _baseAgentSpeed * Mathf.Max(0.1f, phase.moveSpeedMultiplier));
            agent.speed = movementSpeed;
        }

        // Small delay before next skill to telegraph phase change
        _nextSkillTime = Time.time + 1.5f;
        Debug.Log($"[FinalBoss] Entered phase '{phase.name}' at index {index}");
    }

    void OnDestroy()
    {
        if (_health != null)
        {
            _health.onHealthChanged -= OnHealthChanged;
        }
    }
}
