using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "TearsOfTheSinner", menuName = "Furina/Skills/Tears Of The Sinner")]
public class TearsOfTheSinner : SkillBase
{
    [Header("Rain Settings")]
    public float tickInterval = 1f;
    public float additionalDurationUpgrade= 2f;
    public string enemyTag = "Enemy";

    private bool isActive = false;
    private float finalDuration;

    private void OnEnable()
    {
        isActive = false;
    }

    public override void OnSkillActivate(GameObject caster)
    {
        base.OnSkillActivate(caster);

        if (isActive) return;

        isActive = true;

        Debug.Log($"{skillName} activated by {caster.name}");
        if(isUpgraded)
        {
            finalDuration = duration + additionalDurationUpgrade;
        }
        else
        {
            finalDuration = duration;
        }

        // Play cast sound
        if (castSound != null)
        {
            AudioSource.PlayClipAtPoint(castSound, caster.transform.position);
        }

        // Spawn effect prefab
        if (effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(effectPrefab, caster.transform.position, Quaternion.identity);
            Object.Destroy(effect, finalDuration);
        }

        // Start the rain damage coroutine
        MonoBehaviour casterMono = caster.GetComponent<MonoBehaviour>();
        if (casterMono != null)
        {
            casterMono.StartCoroutine(RainOfTearsEffect(caster));
        }
    }

    private IEnumerator RainOfTearsEffect(GameObject caster)
    {
        float elapsed = 0f;

        while (elapsed < finalDuration && isActive)
        {
            DamageAllEnemies(caster);
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }

        OnSkillEnd(caster);
    }

    private void DamageAllEnemies(GameObject caster)
    {
        // Get player stats for damage calculation
        PlayerStats playerStats = caster.GetComponent<PlayerStats>();
        float baseDamage = damageAmount;
        float critChance = 0f;
        float critMultiplier = 1f;
        int casterLevel = 0;
        float damageMultiplier = 1f;

        // Add player attack stat and apply crit if available
        if (playerStats != null)
        {
            baseDamage += playerStats.baseAttack;
            critChance = playerStats.critRate;
            critMultiplier = playerStats.critMultiplier;
            casterLevel = playerStats.level;
            damageMultiplier = playerStats.GetCurrentDamageBuffMultiplier();
        }

        // Find ALL enemies in the scene by tag
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);

        Debug.Log($"{skillName}: Damaging {enemies.Length} enemies for {baseDamage} base damage each");

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;

            // Apply damage
            Health health = enemy.GetComponent<Health>();
            if (health != null)
            {
                var enemyStats = enemy.GetComponent<EnemyStats>();
                float defense = enemyStats != null ? enemyStats.defense : 0f;
                int levelDiff = casterLevel - (enemyStats != null ? enemyStats.level : 0);
                bool didCrit;
                float finalDamage = Helpers.CalculateFinalDamage(
                    baseDamage,
                    defense,
                    critChance,
                    critMultiplier,
                    levelDiff,
                    damageMultiplier,
                    out didCrit
                );
                health.TakeDamage(finalDamage, didCrit);

                // Play impact sound
                if (impactSound != null)
                {
                    AudioSource.PlayClipAtPoint(impactSound, enemy.transform.position);
                }
            }
        }
    }

    public override void OnSkillEnd(GameObject caster)
    {
        if (!isActive) return;

        base.OnSkillEnd(caster);
        isActive = false;

        Debug.Log($"{skillName} ended");
    }

    public override bool CanUseSkill(GameObject caster)
    {
        if (isActive) return false;

        PlayerStats playerStats = caster.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            return playerStats.CurrentMana >= manaCost;
        }
        return base.CanUseSkill(caster);
    }
}
