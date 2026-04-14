using UnityEngine;
using System;
using System.Collections.Generic;

namespace DDAMAPEKitFramework
{
    [Serializable]
    public class PlayerMetric
    {
        public PlayerMetricType type;
        public float rawAccumulated = 0f;     // resets after each profiling pass
        public float emaValue = 0f;           // smoothed (0..1)
        public float lastNormalized = 0f;     // most recent normalized raw
        public float minExpected = 0f;
        public float maxExpected = 1f;

        public PlayerMetric(PlayerMetricType t, float min = 0f, float max = 1f)
        {
            type = t;
            minExpected = min;
            maxExpected = max;
            rawAccumulated = 0f;
            emaValue = 0f;
        }

        public void Accumulate(float delta)
        {
            rawAccumulated += delta;
        }

        public float NormalizeRaw()
        {
            // Normalize rawAccumulated into 0..1 by min/max range.
            if (maxExpected - minExpected <= 0.0001f) return 0f;
            float norm = (rawAccumulated - minExpected) / (maxExpected - minExpected);
            norm = Mathf.Clamp01(norm);
            lastNormalized = norm;
            return norm;
        }

        public void ResetRaw()
        {
            rawAccumulated = 0f;
        }
    }

    public static class CombatEventManager
    {
        public static event Action<float> OnMeleeAttack;    // float = damage
        public static event Action<float> OnRangedAttack;   // float = damage
        public static event Action<float> OnSkillAttack;       // float = mana or damage
        public static event Action OnDodgeAttempt;                 // dodge happened
        public static event Action OnSuccessfulDodge;              // successful dodge happened
        public static event Action<float> OnSuccessfulDodgeDamageAvoided; // float = damage avoided
        public static event Action<float> OnDamageTaken;    // float = damage taken
        public static event Action<float> OnHeal;           // float = heal amount
        public static event Action<float> OnManaUsed;       // float = mana used

        public static void RaiseMeleeAttack(float damage) => OnMeleeAttack?.Invoke(damage);
        public static void RaiseRangedAttack(float damage) => OnRangedAttack?.Invoke(damage);
        public static void RaiseSkillAttack(float amount) => OnSkillAttack?.Invoke(amount);
        public static void RaiseDodgeAttempt() => OnDodgeAttempt?.Invoke();
        public static void RaiseSuccessfulDodge(float avoidedDamage = 0f)
        {
            OnSuccessfulDodge?.Invoke();
            OnSuccessfulDodgeDamageAvoided?.Invoke(Mathf.Max(0f, avoidedDamage));
        }
        public static void RaiseDamageTaken(float damage) => OnDamageTaken?.Invoke(damage);
        public static void RaiseHeal(float heal) => OnHeal?.Invoke(heal);
        public static void RaiseManaUsed(float mana) => OnManaUsed?.Invoke(mana);
    }
}
