using UnityEngine;
using System;

using DDAMAPEKitFramework;

    public class CombatWaveStats
    {
        public float meleeDamage;
        public float rangedDamage;
        public float skillDamage;

        public float damageTaken;

        public int dodgeAttempts;
        public int successfulDodges;
        public float manaUsed;

        public float healingUsed;

        public void Reset()
        {
            meleeDamage = 0;
            rangedDamage = 0;
            skillDamage = 0;
            damageTaken = 0;

            dodgeAttempts = 0;
            successfulDodges = 0;
            manaUsed = 0;

            healingUsed = 0;
        }
    }

    public class CombatMetricCollector : MonoBehaviour
    {
        private CombatWaveStats waveStats = new CombatWaveStats();
        private PlayerModel playerModel;

        void Start()
        {
            playerModel = DDAMAPEKit.Instance.GetPlayerModel();

            CombatEventManager.OnMeleeAttack += OnMeleeAttack;
            CombatEventManager.OnRangedAttack += OnRangedAttack;
            CombatEventManager.OnSkillAttack += OnSkillAttack;
            CombatEventManager.OnDodgeAttempt += OnDodgeAttempt;
            CombatEventManager.OnSuccessfulDodge += OnSuccessfulDodge;
            CombatEventManager.OnDamageTaken += OnDamageTaken;
            CombatEventManager.OnHeal += OnHeal;
            CombatEventManager.OnManaUsed += OnManaUsed;
        }

        void OnDestroy()
        {
            CombatEventManager.OnMeleeAttack -= OnMeleeAttack;
            CombatEventManager.OnRangedAttack -= OnRangedAttack;
            CombatEventManager.OnSkillAttack -= OnSkillAttack;
            CombatEventManager.OnDodgeAttempt -= OnDodgeAttempt;
            CombatEventManager.OnSuccessfulDodge -= OnSuccessfulDodge;
            CombatEventManager.OnDamageTaken -= OnDamageTaken;
            CombatEventManager.OnHeal -= OnHeal;
            CombatEventManager.OnManaUsed -= OnManaUsed;
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
        }

        void OnHeal(float heal)
        {
            waveStats.healingUsed += heal;
        }

        void OnManaUsed(float mana)
        {
            waveStats.manaUsed += mana;
        }

        /// Called by your wave manager when combat wave ends
        public void FinalizeWaveMetrics()
        {
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

            if (waveStats.dodgeAttempts > 0)
            {
                dodgeRate = (float)waveStats.successfulDodges / waveStats.dodgeAttempts;
            }

            // Send to player model
            playerModel.SetProfilingMetric(PlayerMetricType.MeleeUsage, meleeRatio);
            playerModel.SetProfilingMetric(PlayerMetricType.RangedUsage, rangedRatio);
            playerModel.SetProfilingMetric(PlayerMetricType.SkillUsage, skillRatio);
            playerModel.SetProfilingMetric(PlayerMetricType.DodgeRate, dodgeRate);

            playerModel.SetProfilingMetric(PlayerMetricType.DamageTaken, waveStats.damageTaken);
            playerModel.SetProfilingMetric(PlayerMetricType.HealingUsed, waveStats.healingUsed);
            playerModel.SetProfilingMetric(PlayerMetricType.ManaUsed, waveStats.manaUsed);
            waveStats.Reset();

            Debug.Log($"[CombatMetricCollector] Wave finalized. Melee: {meleeRatio:P1}, Ranged: {rangedRatio:P1}, Skill: {skillRatio:P1}, Dodge Rate: {dodgeRate:P1}, Damage Taken: {waveStats.damageTaken}, Healing Used: {waveStats.healingUsed}, Mana Used: {waveStats.manaUsed}");
        }
    }
