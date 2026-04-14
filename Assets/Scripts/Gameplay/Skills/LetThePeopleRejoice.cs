using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "LetThePeopleRejoice", menuName = "Furina/Skills/Let The People Rejoice")]
public class LetThePeopleRejoice : SkillBase
{
    [Header("Buff Settings")]
    public float attackBonus = 20f;
    public float healthDrainPerSecond = 5f;
    public float movementSpeedBonusPercent = 0.2f;

    private GameObject activeCaster;
    private PlayerStats activePlayerStats;
    private float appliedMovementSpeedBonus;
    private float appliedAttackBonus;
    private bool isActive = false;

    private void OnEnable()
    {
        // Reset state when ScriptableObject is loaded (fixes editor play mode issues)
        isActive = false;
        activeCaster = null;
        activePlayerStats = null;
        appliedAttackBonus = 0f;
        appliedMovementSpeedBonus = 0f;
    }

    public override void OnSkillActivate(GameObject caster)
    {
        base.OnSkillActivate(caster);

        if (isActive)
        {
            Debug.LogWarning($"{skillName}: Already active, cannot stack!");
            return;
        }

        activeCaster = caster;
        activePlayerStats = caster.GetComponent<PlayerStats>();

        if (activePlayerStats == null || activePlayerStats.health == null)
        {
            Debug.LogWarning($"{skillName}: Missing PlayerStats or Health component!");
            return;
        }

        // Play cast sound
        if (castSound != null)
        {
            AudioManager.Instance?.PlayVoiceLine(castSound);
        }

        // Spawn effect prefab
        if (effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(effectPrefab, caster.transform.position, Quaternion.identity, caster.transform);
            Object.Destroy(effect, duration);
        }

        if (ongoingSound != null)
        {
            AudioManager.Instance?.PlaySFXNoOverlap(ongoingSound, randomizePitch: false, duration: duration);
        }

        // Store original attack and apply bonus
        appliedAttackBonus = attackBonus;
        activePlayerStats.baseAttack += appliedAttackBonus;
        if(isUpgraded)
        {
            appliedMovementSpeedBonus = activePlayerStats.moveSpeed * movementSpeedBonusPercent;
            activePlayerStats.moveSpeed += appliedMovementSpeedBonus;
        }

        Debug.Log($"{skillName} activated! Attack bonus: +{appliedAttackBonus}, move speed bonus: +{appliedMovementSpeedBonus:F2}");

        // Subscribe to enemy death event
        Enemy.OnAnyDeath += OnEnemyKilled;

        isActive = true;

        // Start health drain coroutine
        TryStartSkillCoroutine(caster, HealthDrainEffect(caster));
    }

    private IEnumerator HealthDrainEffect(GameObject caster)
    {
        float elapsed = 0f;
        float drainInterval = 0.5f;

        while (elapsed < duration && isActive)
        {
            // Drain health
            float drainAmount = healthDrainPerSecond * drainInterval;
            activePlayerStats.health.TakeDamage(
                drainAmount,
                isCrit: false,
                source: DamageSource.Skill,
                applyStagger: false
            );

            // Debug: Show current attack value
            // Debug.Log($"{skillName}: Current baseAttack = {activePlayerStats.baseAttack}");

            yield return new WaitForSeconds(drainInterval);
            elapsed += drainInterval;
        }

        OnSkillEnd(caster);
    }

    private void OnEnemyKilled(Enemy enemy)
    {
        if (!isActive || activePlayerStats == null || activePlayerStats.health == null) return;

        // Heal player when enemy is killed using healAmount from SkillBase
        HealPlayer(healAmount);

        // Debug.Log($"{skillName}: Healed {healAmount} HP from killing {enemy.name}");

        // Play impact sound for feedback
        if (impactSound != null && activeCaster != null)
        {
            AudioManager.Instance?.PlayClipAtPoint(impactSound, activeCaster.transform.position);
        }
    }

    private void HealPlayer(float amount)
    {
        if (activePlayerStats == null || activePlayerStats.health == null) return;
        activePlayerStats.health.Heal(amount);
    }

    public override void OnSkillEnd(GameObject caster)
    {
        if (!isActive) return;

        base.OnSkillEnd(caster);

        // Restore original attack
        if (activePlayerStats != null)
        {
            activePlayerStats.baseAttack -= appliedAttackBonus;
        }
        if (activePlayerStats != null && isUpgraded)
        {
            activePlayerStats.moveSpeed -= appliedMovementSpeedBonus;
        }

        // Unsubscribe from enemy death event
        Enemy.OnAnyDeath -= OnEnemyKilled;

        isActive = false;
        activeCaster = null;
        activePlayerStats = null;
        appliedAttackBonus = 0f;
        appliedMovementSpeedBonus = 0f;

        // Debug.Log($"{skillName} ended");
    }

    public override bool CanUseSkill(GameObject caster)
    {
        // Can't use if already active
        if (isActive) return false;

        PlayerStats playerStats = caster.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            return playerStats.CurrentMana >= manaCost;
        }
        return base.CanUseSkill(caster);
    }
}
