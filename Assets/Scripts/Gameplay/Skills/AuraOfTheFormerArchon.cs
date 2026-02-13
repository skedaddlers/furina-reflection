using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "AuraOfTheFormerArchon", menuName = "Furina/Skills/Aura Of The Former Archon")]
public class AuraOfTheFormerArchon : SkillBase
{
    [Header("Aura Settings")]
    public float radius = 5f;
    public float tickInterval = 0.5f;
    public string enemyTag = "Enemy";
    public string playerTag = "Player";

    [Header("Slow Effect")]
    [Range(0f, 1f)]
    public float slowPercent = 0.5f; // 0.5 = 50% slower

    private Dictionary<EnemyAI, float> originalSpeeds = new Dictionary<EnemyAI, float>();
    private bool isActive = false;

    private void OnEnable()
    {
        isActive = false;
        originalSpeeds.Clear();
    }

    public override void OnSkillActivate(GameObject caster)
    {
        base.OnSkillActivate(caster);

        if (isActive) return;

        isActive = true;

        Debug.Log($"{skillName} activated by {caster.name}");

        // Play cast sound if available
        if (castSound != null)
        {
            AudioSource.PlayClipAtPoint(castSound, caster.transform.position);
        }

        // Spawn effect prefab if available
        if (effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(effectPrefab, caster.transform.position, Quaternion.identity, caster.transform);
            Object.Destroy(effect, duration);
        }

        // Start the aura coroutine on the caster
        MonoBehaviour casterMono = caster.GetComponent<MonoBehaviour>();
        if (casterMono != null)
        {
            casterMono.StartCoroutine(AuraEffect(caster));
        }
    }

    private IEnumerator AuraEffect(GameObject caster)
    {
        float elapsed = 0f;

        while (elapsed < duration && isActive)
        {
            DamageEnemiesInRadius(caster);
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }

        OnSkillEnd(caster);
    }

    private void DamageEnemiesInRadius(GameObject caster)
    {
        // Get player stats for damage calculation
        PlayerStats playerStats = caster.GetComponent<PlayerStats>();
        EnemyAI enemyCaster = caster.GetComponent<EnemyAI>();
        EnemyStats enemyStats = caster.GetComponent<EnemyStats>();
        float baseDamage = damageAmount;
        float critChance = 0f;
        float critMultiplier = 1f;
        int casterLevel = 0;
        float damageMultiplier = 1f;

        // Add attack stat and apply crit if available
        if (playerStats != null)
        {
            baseDamage += playerStats.baseAttack;
            critChance = playerStats.critRate;
            critMultiplier = playerStats.critMultiplier;
            casterLevel = playerStats.level;
            damageMultiplier = playerStats.GetCurrentDamageBuffMultiplier();
        }
        else if (enemyCaster != null)
        {
            baseDamage += enemyCaster.damage;
            if (enemyStats != null)
            {
                critChance = enemyStats.critRate;
                critMultiplier = enemyStats.critMultiplier;
                casterLevel = enemyStats.level;
            }
        }

        // Find all colliders in radius
        Collider[] hitColliders = Physics.OverlapSphere(caster.transform.position, radius);

        // Track enemies currently in radius
        HashSet<EnemyAI> enemiesInRadius = new HashSet<EnemyAI>();

        foreach (Collider hit in hitColliders)
        {
            // Skip the caster
            if (hit.gameObject == caster) continue;

            // Determine target tag based on caster tag
            string targetTag = caster.CompareTag(playerTag) ? enemyTag : playerTag;
            if (!hit.CompareTag(targetTag)) continue;

            // Apply damage
            Health health = hit.GetComponent<Health>();
            if (health != null)
            {
                float defense = 0f;
                int levelDiff = 0;
                if (targetTag == enemyTag)
                {
                    var targetStats = hit.GetComponent<EnemyStats>();
                    defense = targetStats != null ? targetStats.defense : 0f;
                    levelDiff = casterLevel - (targetStats != null ? targetStats.level : 0);
                }
                else
                {
                    var targetStats = hit.GetComponent<PlayerStats>();
                    defense = targetStats != null ? targetStats.baseDefense : 0f;
                    levelDiff = casterLevel - (targetStats != null ? targetStats.level : 0);
                }

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
                Debug.Log($"{hit.name} took {finalDamage} damage from {skillName}");

                // Play impact sound
                if (impactSound != null)
                {
                    AudioSource.PlayClipAtPoint(impactSound, hit.transform.position);
                }
            }

            // if upgraded, apply slow effect
            if(isUpgraded)
            {
                EnemyAI enemy = hit.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    enemiesInRadius.Add(enemy);
                    if (!originalSpeeds.ContainsKey(enemy))
                    {
                        originalSpeeds[enemy] = enemy.movementSpeed;
                        enemy.ApplySpeedModifier(1f - slowPercent);
                    }
                }
            } 
        }

        // Restore speed for enemies that left the radius

        if(isUpgraded)
        {
            RestoreSpeeds(enemiesInRadius);
        } 
    }

    private void RestoreSpeeds(HashSet<EnemyAI> enemiesInRadius)
    {
        List<EnemyAI> enemiesToRestore = new List<EnemyAI>();

        foreach (var kvp in originalSpeeds)
        {
            if (!enemiesInRadius.Contains(kvp.Key))
            {
                enemiesToRestore.Add(kvp.Key);
            }
        }

        foreach (var enemy in enemiesToRestore)
        {
            if (enemy != null)
            {
                enemy.ApplySpeedModifier(1f);
                Debug.Log($"{enemy.name} speed restored after leaving aura");
            }
            originalSpeeds.Remove(enemy);
        }
    }

    

    public override void OnSkillEnd(GameObject caster)
    {
        if (!isActive) return;

        base.OnSkillEnd(caster);

        // Restore all slowed enemies to original speed
        foreach (var kvp in originalSpeeds)
        {
            if (kvp.Key != null)
            {
                kvp.Key.ApplySpeedModifier(1f);
                Debug.Log($"{kvp.Key.name} speed restored on skill end");
            }
        }
        originalSpeeds.Clear();

        isActive = false;
        Debug.Log($"{skillName} aura ended");
    }

    public override bool CanUseSkill(GameObject caster)
    {
        if (isActive) return false;

        PlayerStats playerStats = caster.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            return playerStats.CurrentMana >= manaCost;
        }
        // Enemies or non-player casters: allow
        return true;
    }
}
