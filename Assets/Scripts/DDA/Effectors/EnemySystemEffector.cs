using UnityEngine;
using System.Collections.Generic;
using DDAMAPEKitFramework;

/// <summary>
/// Effector for adjusting enemy-related variables
/// </summary>
public class EnemySystemEffector : Effector
{
    private SpawnTrigger[] spawnTriggers;
    private EnemyAI[] activeEnemies;

    private float minMultiplier = 0.5f;
    private float maxMultiplier = 2.5f;
    private float smoothingFactor = 0.6f;
    private float trendBoostFactor = 0.1f;

    private Dictionary<string, float> currentMultipliers = new Dictionary<string, float>();

    private string lastSymptom;
    private int sameSymptomCount;
    private float timeSinceLastSymptom;

    void Start()
    {
        // Find all spawn triggers in the scene
        spawnTriggers = FindObjectsOfType<SpawnTrigger>();
    }

    public override void Apply(string variable, float value)
    {
        var diff = GlobalDifficultyState.Instance;
        switch (variable)
        {
            case "enemyDamageMultiplier":
                float enemyDamageValue = GetNewMultiplier(variable, value);
                diff?.SetEnemyMultiplier("damage", enemyDamageValue);
                // UpdateActiveEnemies();
                break;
            case "enemyHealthMultiplier":
                float enemyHealthValue = GetNewMultiplier(variable, value);
                diff?.SetEnemyMultiplier("health", enemyHealthValue);
                // UpdateActiveEnemies();
                break;
            case "enemySpeedMultiplier":
                float enemySpeedValue = GetNewMultiplier(variable, value);
                diff?.SetEnemyMultiplier("speed", enemySpeedValue);
                // UpdateActiveEnemies();
                break;
            case "enemyAttackSpeedMultiplier":
                float enemyAttackSpeedValue = GetNewMultiplier(variable, value);
                diff?.SetEnemyMultiplier("attackSpeed", enemyAttackSpeedValue);
                // UpdateActiveEnemies();
                break;
            case "enemyAggroMultiplier":
                float enemyAggroValue = GetNewMultiplier(variable, value);
                diff?.SetEnemyMultiplier("aggro", enemyAggroValue);
                // UpdateActiveEnemies();
                break;
            case "enemyCountMultiplier":
                float enemyCountValue = GetNewMultiplier(variable, value);
                diff?.SetEnemyCountMultiplier(enemyCountValue);
                break;
             default:
                Debug.LogWarning($"[EnemySystemEffector] Unknown variable: {variable}");
                break;
        }
    }

    private float GetNewMultiplier(string variable, float value)
    {
        if (!currentMultipliers.ContainsKey(variable))
        {
            currentMultipliers[variable] = 1f; // Start with no change
        }

        // since Apply function will be called multiple times per MAPE loop, we should check
        if (Time.time - timeSinceLastSymptom > 1f) //
        {
            string currentSymptom = DDAMAPEKit.Instance.GetCurrentSymptom()?.description ?? "None";
            if (lastSymptom == currentSymptom)
            {
                sameSymptomCount++;
            }
            else
            {
                sameSymptomCount = 0;
            }
            lastSymptom = currentSymptom;
            timeSinceLastSymptom = Time.time;
        }


        float current = currentMultipliers[variable];
        float newMultiplier = current * value;
        float boostFactor = sameSymptomCount * trendBoostFactor;
        float boost = 1f;
        if (value > 1f)
        {
            boost += boostFactor; // Amplify increases
        }
        else if (value < 1f)
        {
            boost -= boostFactor; // Amplify decreases
        }

        newMultiplier = newMultiplier * boost;
        newMultiplier = Mathf.Clamp(newMultiplier, minMultiplier, maxMultiplier);

        float smoothed = Mathf.Lerp(current, newMultiplier, smoothingFactor);

        currentMultipliers[variable] = smoothed;
        Debug.Log($"[EnemySystemEffector] Variable: {variable}, Current: {current:F2}, New: {newMultiplier:F2}, Smoothed: {smoothed:F2}, SameSymptomCount: {sameSymptomCount}");
        Debug.Log($"[EnemySystemEffector] Calculation: {current:F2} * {value:F2} * {boost:F2} = {newMultiplier:F2} (Clamped: {newMultiplier:F2}) -> Smoothed: {smoothed:F2}");
        Debug.Log($"[EnemySystemEffector] LastSymptom: {lastSymptom}, TimeSinceLastSymptom: {Time.time - timeSinceLastSymptom:F2}s");
        return smoothed;
    }

    private void UpdateActiveEnemies()
    {
        activeEnemies = FindObjectsOfType<EnemyAI>();
        foreach (var enemy in activeEnemies)
        {
            enemy?.ApplyDifficultyMultipliers();
        }
    }
}
