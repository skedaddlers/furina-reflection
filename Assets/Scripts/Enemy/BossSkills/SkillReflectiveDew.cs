using UnityEngine;
using System.Collections;
using DDAMAPEKitFramework;

public class SkillReflectiveDew : BossSkill
{
    [Header("Additional Settings")]
    public float defenseIncreaseMultiplier = 0.5f; // Multiplies the boss defense by this amount during the skill
    [Header("Duration")]
    [SerializeField] private float reflectiveDuration = 6f;
    [SerializeField] [Min(0f)] private float mirroredHealMultiplier = 1f;

    [Header("Effects")]
    [SerializeField] private GameObject activeEffectPrefab;
    [SerializeField] private float activeEffectLifetime = 8f;
    [SerializeField] private GameObject mirrorPulseEffectPrefab;
    [SerializeField] private float mirrorPulseEffectLifetime = 1.2f;

    private Health bossHealth;
    private bool isListening;
    private bool hadListener;

    public override IEnumerator ExecuteRoutine()
    {
        if (boss == null) yield break;

        bossHealth = boss.GetComponent<Health>();
        if (bossHealth == null) yield break;
        float originalDefense = boss.GetComponent<EnemyStats>().defense;
        boss.GetComponent<EnemyStats>().defense += originalDefense * defenseIncreaseMultiplier;

        StopListening();

        boss.Animator.SetBool(animationTrigger, true);
        UIManager.Instance.ShowNotification(notificationText, notificationDuration);
        GameObject activeFx = null;
        if (activeEffectPrefab != null)
        {
            activeFx = Instantiate(activeEffectPrefab, boss.transform.position, Quaternion.identity, boss.transform);
            if (activeEffectLifetime > 0f)
                Destroy(activeFx, activeEffectLifetime);
        }

        CombatEventManager.OnHeal += OnPlayerHealed;
        hadListener = true;
        isListening = true;

        float duration = Mathf.Max(0f, reflectiveDuration);
        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        boss.Animator.SetBool(animationTrigger, false);
        boss.GetComponent<EnemyStats>().defense = originalDefense;
        StopListening();

        if (activeFx != null)
            Destroy(activeFx);
    }

    private void OnPlayerHealed(float healedAmount)
    {
        if (!isListening || bossHealth == null) return;
        if (healedAmount <= 0f) return;

        float mirroredHeal = healedAmount * Mathf.Max(0f, mirroredHealMultiplier);
        if (mirroredHeal <= 0f) return;

        bossHealth.Heal(mirroredHeal);

        if (mirrorPulseEffectPrefab != null && boss != null)
        {
            GameObject pulseFx = Instantiate(mirrorPulseEffectPrefab, boss.transform.position, Quaternion.identity);
            Destroy(pulseFx, mirrorPulseEffectLifetime);
        }
    }

    private void StopListening()
    {
        isListening = false;

        if (!hadListener) return;
        CombatEventManager.OnHeal -= OnPlayerHealed;
        hadListener = false;
    }

    private void OnDisable()
    {
        StopListening();
    }

    private void OnDestroy()
    {
        StopListening();
    }
}
