using UnityEngine;
using System;
using System.Collections.Generic;
using DDAMAPEKitFramework;

public class CombatMetricCollector : MonoBehaviour
{
    public List<PlayerMetric> profilingMetrics = new List<PlayerMetric>();
    [SerializeField] private float distanceSampleInterval = 0.25f;
    [SerializeField] private float maxExpectedCombatDistance = 12f;

    private CombatWaveStats waveStats = new CombatWaveStats();
    private PlayerModel playerModel;
    private PlayerStats playerStats;
    private Transform playerTransform;
    private bool isCombatActive;
    private float nextDistanceSampleTime;
    private UpgradeChoiceStats upgradeChoiceStats = new UpgradeChoiceStats();

    void Start()
    {
        playerModel = DDAMAPEKit.Instance.GetPlayerModel();
        EnsurePlayerStatsReference();

        CombatEventManager.OnMeleeAttack += OnMeleeAttack;
        CombatEventManager.OnRangedAttack += OnRangedAttack;
        CombatEventManager.OnSkillAttack += OnSkillAttack;
        CombatEventManager.OnSkillCast += OnSkillCast;
        CombatEventManager.OnDodgeAttempt += OnDodgeAttempt;
        CombatEventManager.OnSuccessfulDodge += OnSuccessfulDodge;
        CombatEventManager.OnSuccessfulDodgeDamageAvoided += OnSuccessfulDodgeDamageAvoided;
        CombatEventManager.OnDamageTaken += OnDamageTaken;
        CombatEventManager.OnHeal += OnHeal;
        CombatEventManager.OnManaUsed += OnManaUsed;
        CombatEventManager.OnShieldDamageAbsorbed += OnShieldDamageAbsorbed;
        Room.OnRoomCombatStarted += OnRoomCombatStarted;
        Room.OnRoomCleared += OnRoomCleared;

        UpdateUpgradePreferenceMetrics();
    }

    void Update()
    {
        EnsurePlayerStatsReference();

        if (!isCombatActive || playerTransform == null || Time.time < nextDistanceSampleTime)
            return;

        nextDistanceSampleTime = Time.time + distanceSampleInterval;
        SampleDistanceToNearestEnemy();
    }

    void OnDestroy()
    {
        CombatEventManager.OnMeleeAttack -= OnMeleeAttack;
        CombatEventManager.OnRangedAttack -= OnRangedAttack;
        CombatEventManager.OnSkillAttack -= OnSkillAttack;
        CombatEventManager.OnSkillCast -= OnSkillCast;
        CombatEventManager.OnDodgeAttempt -= OnDodgeAttempt;
        CombatEventManager.OnSuccessfulDodge -= OnSuccessfulDodge;
        CombatEventManager.OnSuccessfulDodgeDamageAvoided -= OnSuccessfulDodgeDamageAvoided;
        CombatEventManager.OnDamageTaken -= OnDamageTaken;
        CombatEventManager.OnHeal -= OnHeal;
        CombatEventManager.OnManaUsed -= OnManaUsed;
        CombatEventManager.OnShieldDamageAbsorbed -= OnShieldDamageAbsorbed;
        Room.OnRoomCombatStarted -= OnRoomCombatStarted;
        Room.OnRoomCleared -= OnRoomCleared;

        if (playerStats != null)
        {
            playerStats.onUpgradeChoiceSelected -= OnUpgradeChoiceSelected;
        }
    }

    void OnMeleeAttack(float damage)
    {
        waveStats.meleeDamage += damage;
    }

    void OnRangedAttack(float damage)
    {
        waveStats.rangedDamage += damage;
    }

    void OnSkillAttack(float damage)
    {
        waveStats.skillDamage += damage;
    }

    void OnSkillCast(float castCount)
    {
        waveStats.skillCasts += castCount;
        AccumulateProfilingMetric(PlayerMetricType.SkillCastRate, castCount);
    }

    void OnDodgeAttempt()
    {
        waveStats.dodgeAttempts++;
    }

    void OnSuccessfulDodge()
    {
        waveStats.successfulDodges++;
    }

    void OnDamageTaken(float damage)
    {
        waveStats.damageTaken += damage;
        AccumulateProfilingMetric(PlayerMetricType.DamageTaken, damage);
    }

    void OnSuccessfulDodgeDamageAvoided(float damageAvoided)
    {
        waveStats.successfulDodgeDamageAvoided += damageAvoided;
    }

    void OnHeal(float heal)
    {
        waveStats.healingUsed += heal;
        AccumulateProfilingMetric(PlayerMetricType.HealingUsed, heal);
    }

    void OnManaUsed(float mana)
    {
        waveStats.manaUsed += mana;
        AccumulateProfilingMetric(PlayerMetricType.ManaUsed, mana);
    }

    void OnShieldDamageAbsorbed(float damageAbsorbed)
    {
        waveStats.damageAbsorbedByShield += damageAbsorbed;
        AccumulateProfilingMetric(PlayerMetricType.DamageAbsorbedByShield, damageAbsorbed);
    }

    void OnRoomCombatStarted(Room room)
    {
        isCombatActive = true;
        nextDistanceSampleTime = Time.time;
        waveStats.ResetDistanceTracking();
    }

    void OnRoomCleared(Room room)
    {
        isCombatActive = false;
    }

    void OnUpgradeChoiceSelected(PlayerUpgradeChoiceType choiceType)
    {
        switch (choiceType)
        {
            case PlayerUpgradeChoiceType.Health:
                upgradeChoiceStats.healthChoices++;
                break;
            case PlayerUpgradeChoiceType.Attack:
                upgradeChoiceStats.attackChoices++;
                break;
            case PlayerUpgradeChoiceType.Defense:
                upgradeChoiceStats.defenseChoices++;
                break;
            case PlayerUpgradeChoiceType.Mana:
                upgradeChoiceStats.manaChoices++;
                break;
            case PlayerUpgradeChoiceType.MoveSpeed:
                upgradeChoiceStats.speedChoices++;
                break;
            case PlayerUpgradeChoiceType.Crit:
                upgradeChoiceStats.critChoices++;
                break;
        }

        UpdateUpgradePreferenceMetrics();
    }

    /// Called by your wave manager when combat wave ends
    public void FinalizeWaveMetrics()
    {
        PublishCurrentCombatMetrics(true);
    }

    public bool PublishCurrentCombatMetrics(bool resetAccumulatedStats = false)
    {
        if (playerModel == null)
        {
            playerModel = DDAMAPEKit.Instance.GetPlayerModel();
        }

        if (playerModel == null)
            return false;

        float totalDamage =
            waveStats.meleeDamage +
            waveStats.rangedDamage +
            waveStats.skillDamage;

        float meleeRatio = 0;
        float rangedRatio = 0;
        float skillRatio = 0;

        if (totalDamage > 0)
        {
            meleeRatio = waveStats.meleeDamage / totalDamage;
            rangedRatio = waveStats.rangedDamage / totalDamage;
            skillRatio = waveStats.skillDamage / totalDamage;
        }

        float dodgeRate = 0;
        float theoreticalDamageTaken = waveStats.damageTaken + waveStats.successfulDodgeDamageAvoided;

        if (theoreticalDamageTaken > 0)
        {
            dodgeRate = waveStats.successfulDodgeDamageAvoided / theoreticalDamageTaken;
        }

        float damageTaken = GetNormalizedMetricValue(PlayerMetricType.DamageTaken, resetAccumulatedStats);
        float healingUsed = GetNormalizedMetricValue(PlayerMetricType.HealingUsed, resetAccumulatedStats);
        float manaUsed = GetNormalizedMetricValue(PlayerMetricType.ManaUsed, resetAccumulatedStats);
        float skillCastRate = GetNormalizedMetricValue(PlayerMetricType.SkillCastRate, resetAccumulatedStats);
        float damageAbsorbedByShield = GetNormalizedMetricValue(PlayerMetricType.DamageAbsorbedByShield, resetAccumulatedStats);

        float averageDistance = 0f;
        if (waveStats.distanceSampleCount > 0)
        {
            averageDistance = waveStats.totalNearestEnemyDistance / waveStats.distanceSampleCount;
        }

        float averageDistanceNormalized = Mathf.Clamp01(
            averageDistance / Mathf.Max(0.01f, maxExpectedCombatDistance)
        );

        bool hasCombatSignal =
            totalDamage > 0f ||
            waveStats.dodgeAttempts > 0 ||
            waveStats.distanceSampleCount > 0 ||
            skillCastRate > 0f ||
            damageTaken > 0f ||
            healingUsed > 0f ||
            manaUsed > 0f ||
            damageAbsorbedByShield > 0f;

        if (!hasCombatSignal && !resetAccumulatedStats)
            return false;

        UpdateUpgradePreferenceMetrics();

        // Send to player model
        playerModel.SetProfilingMetric(PlayerMetricType.MeleeUsage, meleeRatio);
        playerModel.SetProfilingMetric(PlayerMetricType.RangedUsage, rangedRatio);
        playerModel.SetProfilingMetric(PlayerMetricType.SkillUsage, skillRatio);
        playerModel.SetProfilingMetric(PlayerMetricType.SkillCastRate, skillCastRate);
        playerModel.SetProfilingMetric(PlayerMetricType.DodgeRate, dodgeRate);
        playerModel.SetProfilingMetric(PlayerMetricType.AverageDistance, averageDistanceNormalized);
        playerModel.SetProfilingMetric(PlayerMetricType.DamageTaken, damageTaken);
        playerModel.SetProfilingMetric(PlayerMetricType.HealingUsed, healingUsed);
        playerModel.SetProfilingMetric(PlayerMetricType.ManaUsed, manaUsed);
        playerModel.SetProfilingMetric(PlayerMetricType.DamageAbsorbedByShield, damageAbsorbedByShield);

        if (resetAccumulatedStats)
        {
            waveStats.Reset();
        }

        Debug.Log(
            $"[CombatMetricCollector] {(resetAccumulatedStats ? "Wave finalized" : "Live profiling published")}. " +
            $"Melee: {meleeRatio:P1}, Ranged: {rangedRatio:P1}, Skill: {skillRatio:P1}, Skill Cast Rate: {skillCastRate}, " +
            $"Dodge Rate: {dodgeRate:P1}, Avg Distance: {averageDistance:F2} ({averageDistanceNormalized:F2}), " +
            $"Damage Taken: {damageTaken}, Healing Used: {healingUsed}, Mana Used: {manaUsed}, " +
            $"Shield Absorbed: {damageAbsorbedByShield}"
        );

        return true;
    }

    private void EnsurePlayerStatsReference()
    {
        if (playerStats != null)
        {
            if (playerTransform == null)
                playerTransform = playerStats.transform;
            return;
        }

        playerStats = PlayerStats.Instance != null ? PlayerStats.Instance : FindObjectOfType<PlayerStats>();
        if (playerStats == null)
            return;

        playerTransform = playerStats.transform;
        playerStats.onUpgradeChoiceSelected -= OnUpgradeChoiceSelected;
        playerStats.onUpgradeChoiceSelected += OnUpgradeChoiceSelected;
    }

    private void SampleDistanceToNearestEnemy()
    {
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
        if (enemies == null || enemies.Length == 0)
            return;

        float nearestDistance = float.MaxValue;
        Vector3 playerPosition = playerTransform.position;
        playerPosition.y = 0f;

        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                continue;

            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null && enemyHealth.GetCurrentHealth() <= 0f)
                continue;

            Vector3 enemyPosition = enemy.transform.position;
            enemyPosition.y = 0f;
            float distance = Vector3.Distance(playerPosition, enemyPosition);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
            }
        }

        if (nearestDistance == float.MaxValue)
            return;

        waveStats.totalNearestEnemyDistance += nearestDistance;
        waveStats.distanceSampleCount++;
    }

    private void UpdateUpgradePreferenceMetrics()
    {
        if (playerModel == null)
        {
            playerModel = DDAMAPEKit.Instance.GetPlayerModel();
        }

        if (playerModel == null)
            return;

        int totalChoices = upgradeChoiceStats.TotalChoices;
        float defensivePreference = 0f;
        float offensivePreference = 0f;
        float manaPreference = 0f;
        float speedPreference = 0f;

        if (totalChoices > 0)
        {
            defensivePreference = (float)(
                upgradeChoiceStats.healthChoices + upgradeChoiceStats.defenseChoices
            ) / totalChoices;

            offensivePreference = (float)(
                upgradeChoiceStats.attackChoices + upgradeChoiceStats.critChoices
            ) / totalChoices;

            manaPreference = (float)upgradeChoiceStats.manaChoices / totalChoices;
            speedPreference = (float)upgradeChoiceStats.speedChoices / totalChoices;
        }

        playerModel.SetProfilingMetric(PlayerMetricType.DefensiveUpgradePreference, defensivePreference);
        playerModel.SetProfilingMetric(PlayerMetricType.OffensiveUpgradePreference, offensivePreference);
        playerModel.SetProfilingMetric(PlayerMetricType.ManaUpgradePreference, manaPreference);
        playerModel.SetProfilingMetric(PlayerMetricType.SpeedUpgradePreference, speedPreference);

        Debug.Log(
            $"[CombatMetricCollector] Upgrade preferences updated. " +
            $"Defensive: {defensivePreference:P1}, Offensive: {offensivePreference:P1}, " +
            $"Mana: {manaPreference:P1}, Speed: {speedPreference:P1}"
        );
    }

    private void AccumulateProfilingMetric(PlayerMetricType metricType, float amount)
    {
        foreach (PlayerMetric metric in profilingMetrics)
        {
            if (metric.type != metricType)
                continue;

            metric.Accumulate(amount);
            break;
        }
    }

    private float GetNormalizedMetricValue(PlayerMetricType metricType, bool resetAccumulatedStats)
    {
        foreach (PlayerMetric metric in profilingMetrics)
        {
            if (metric.type != metricType)
                continue;

            float normalizedValue = metric.NormalizeRaw();
            if (resetAccumulatedStats)
            {
                metric.ResetRaw();
            }

            return normalizedValue;
        }

        return 0f;
    }
}

public class CombatWaveStats
{
    public float meleeDamage;
    public float rangedDamage;
    public float skillDamage;
    public float skillCasts;

    public float damageTaken;
    public float damageAbsorbedByShield;
    public float successfulDodgeDamageAvoided;

    public int dodgeAttempts;
    public int successfulDodges;
    public float manaUsed;

    public float healingUsed;
    public float totalNearestEnemyDistance;
    public int distanceSampleCount;

    public void Reset()
    {
        meleeDamage = 0;
        rangedDamage = 0;
        skillDamage = 0;
        skillCasts = 0;
        damageTaken = 0;
        damageAbsorbedByShield = 0;
        successfulDodgeDamageAvoided = 0;

        dodgeAttempts = 0;
        successfulDodges = 0;
        manaUsed = 0;

        healingUsed = 0;
        ResetDistanceTracking();
    }

    public void ResetDistanceTracking()
    {
        totalNearestEnemyDistance = 0f;
        distanceSampleCount = 0;
    }
}

public class UpgradeChoiceStats
{
    public int healthChoices;
    public int attackChoices;
    public int defenseChoices;
    public int manaChoices;
    public int speedChoices;
    public int critChoices;

    public int TotalChoices =>
        healthChoices + attackChoices + defenseChoices + manaChoices + speedChoices + critChoices;
}
