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
    private float smoothingFactor = 0.5f;
    private float trendBoostFactor = 0.1f;

    private Dictionary<string, float> currentMultipliers;

    private string lastSymptom;
    private int sameSymptomCount;
    private float timeSinceLastSymptom;

    void Start()
    {
        // Find all spawn triggers in the scene
        spawnTriggers = FindObjectsOfType<SpawnTrigger>();
        currentMultipliers = new Dictionary<string, float>();
    }

    public override void Apply(string variable, float value)
    {
        var diff = GlobalDifficultyState.Instance;
        float newValue = GetNewMultiplier(variable, value);
        switch (variable)
        {
            case "enemyDamageMultiplier":
                diff?.SetEnemyMultiplier("damage", newValue);
                UpdateActiveEnemies();
                break;
            case "enemyHealthMultiplier":
                diff?.SetEnemyMultiplier("health", newValue);
                UpdateActiveEnemies();
                break;
            case "enemySpeedMultiplier":
                diff?.SetEnemyMultiplier("speed", newValue);
                UpdateActiveEnemies();
                break;
            case "enemyAttackSpeedMultiplier":
                diff?.SetEnemyMultiplier("attackSpeed", newValue);
                UpdateActiveEnemies();
                break;
            case "enemyAggroMultiplier":
                diff?.SetEnemyMultiplier("aggro", newValue);
                UpdateActiveEnemies();
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
            lastSymptom = DDAMAPEKit.Instance.GetCurrentSymptom()?.description ?? "None";
            if (lastSymptom == variable)
            {
                sameSymptomCount++;
            }
            else
            {
                sameSymptomCount = 0;
            }
            timeSinceLastSymptom = Time.time;
        }


        float current = currentMultipliers[variable];
        float newMultiplier = current * value;
        float boost = 1f + sameSymptomCount * trendBoostFactor;

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
