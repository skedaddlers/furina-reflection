using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Effector for adjusting enemy-related variables
/// </summary>
public class EnemySystemEffector : Effector
{
    private SpawnTrigger[] spawnTriggers;
    private EnemyAI[] activeEnemies;
    
    // Base values for enemy system
    private float baseEnemySpawnRate = 3f;
    private int baseMaxEnemies = 5;
    private float baseEnemyDamage = 10f;
    private float baseEnemySpeed = 3f;

    void Start()
    {
        // Find all spawn triggers in the scene
        spawnTriggers = FindObjectsOfType<SpawnTrigger>();
    }

    public override void Apply(string variable, float value)
    {
        switch (variable)
        {
            case "enemySpawnRate":
                AdjustSpawnRate(value);
                break;
            case "maxEnemies":
                AdjustMaxEnemies((int)value);
                break;
            case "enemyDamage":
                AdjustEnemyDamage(value);
                break;
            case "enemySpeed":
                AdjustEnemySpeed(value);
                break;
            case "enemyHealth":
                AdjustEnemyHealth(value);
                break;
        }
    }

    private void AdjustSpawnRate(float adjustment)
    {
        float newRate = baseEnemySpawnRate + adjustment;
        newRate = Mathf.Max(0.5f, newRate); // Minimum spawn rate

        foreach (var spawner in spawnTriggers)
        {
            if (spawner != null)
            {
                // Adjust spawn timer or cooldown
                spawner.SetSpawnCooldown(newRate);
            }
        }
        Debug.Log($"[EnemySystemEffector] Spawn rate adjusted to: {newRate}");
    }

    private void AdjustMaxEnemies(int adjustment)
    {
        int newMax = baseMaxEnemies + adjustment;
        newMax = Mathf.Max(1, newMax); // At least 1 enemy

        foreach (var spawner in spawnTriggers)
        {
            if (spawner != null)
            {
                spawner.SetMaxEnemies(newMax);
            }
        }
        Debug.Log($"[EnemySystemEffector] Max enemies adjusted to: {newMax}");
    }

    private void AdjustEnemyDamage(float adjustment)
    {
        float newDamage = baseEnemyDamage + adjustment;
        newDamage = Mathf.Max(1f, newDamage);

        // Update all active enemies
        activeEnemies = FindObjectsOfType<EnemyAI>();
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.damage = (int)newDamage;
            }
        }
        Debug.Log($"[EnemySystemEffector] Enemy damage adjusted to: {newDamage}");
    }

    private void AdjustEnemySpeed(float adjustment)
    {
        float newSpeed = baseEnemySpeed + adjustment;
        newSpeed = Mathf.Max(0.5f, newSpeed);

        activeEnemies = FindObjectsOfType<EnemyAI>();
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null)
                {
                    agent.speed = newSpeed;
                }
            }
        }
        Debug.Log($"[EnemySystemEffector] Enemy speed adjusted to: {newSpeed}");
    }

    private void AdjustEnemyHealth(float adjustment)
    {
        float healthMultiplier = 1f + (adjustment * 0.1f);
        healthMultiplier = Mathf.Max(0.5f, healthMultiplier);

        activeEnemies = FindObjectsOfType<EnemyAI>();
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                var health = enemy.GetComponent<Health>();
                if (health != null)
                {
                    health.maxHealth *= healthMultiplier;
                }
            }
        }
        Debug.Log($"[EnemySystemEffector] Enemy health multiplier: {healthMultiplier}");
    }
}

