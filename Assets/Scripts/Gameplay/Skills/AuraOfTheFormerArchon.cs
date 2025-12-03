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

    [Header("Slow Effect")]
    [Range(0f, 1f)]
    public float slowPercent = 0.5f; // 0.5 = 50% slower

    private Dictionary<NavMeshAgent, float> originalSpeeds = new Dictionary<NavMeshAgent, float>();
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
            DamageAndSlowEnemiesInRadius(caster);
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }

        OnSkillEnd(caster);
    }

    private void DamageAndSlowEnemiesInRadius(GameObject caster)
    {
        // Get player stats for damage calculation
        PlayerStats playerStats = caster.GetComponent<PlayerStats>();
        float finalDamage = damageAmount;

        // Add player attack stat and apply crit if available
        if (playerStats != null)
        {
            finalDamage += playerStats.baseAttack;
            finalDamage = playerStats.RollDamage(finalDamage);
        }

        // Find all colliders in radius
        Collider[] hitColliders = Physics.OverlapSphere(caster.transform.position, radius);

        // Track enemies currently in radius
        HashSet<NavMeshAgent> enemiesInRadius = new HashSet<NavMeshAgent>();

        foreach (Collider hit in hitColliders)
        {
            // Skip the caster
            if (hit.gameObject == caster) continue;

            // Check if it's an enemy by tag
            if (!hit.CompareTag(enemyTag)) continue;

            // Apply damage
            Health health = hit.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(finalDamage);
                Debug.Log($"{hit.name} took {finalDamage} damage from {skillName}");

                // Play impact sound
                if (impactSound != null)
                {
                    AudioSource.PlayClipAtPoint(impactSound, hit.transform.position);
                }
            }

            // Apply slow effect
            NavMeshAgent agent = hit.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                enemiesInRadius.Add(agent);

                // Store original speed if not already stored
                if (!originalSpeeds.ContainsKey(agent))
                {
                    originalSpeeds[agent] = agent.speed;
                    agent.speed = originalSpeeds[agent] * (1f - slowPercent);
                    Debug.Log($"{hit.name} slowed by {slowPercent * 100}%");
                }
            }
        }

        // Restore speed for enemies that left the radius
        List<NavMeshAgent> toRemove = new List<NavMeshAgent>();
        foreach (var kvp in originalSpeeds)
        {
            if (kvp.Key != null && !enemiesInRadius.Contains(kvp.Key))
            {
                kvp.Key.speed = kvp.Value;
                toRemove.Add(kvp.Key);
                Debug.Log($"{kvp.Key.name} speed restored");
            }
        }

        foreach (var agent in toRemove)
        {
            originalSpeeds.Remove(agent);
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
                kvp.Key.speed = kvp.Value;
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
        return base.CanUseSkill(caster);
    }
}