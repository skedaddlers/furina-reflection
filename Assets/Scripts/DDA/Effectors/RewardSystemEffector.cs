using UnityEngine;
using DDAMAPEKitFramework;
using System.Collections.Generic;

/// <summary>
/// Effector for adjusting item and reward-related variables
/// </summary>
public class RewardSystemEffector : Effector
{
    private const string ItemDropRateVariable = "itemDropRate";
    private const string RewardMultiplierVariable = "rewardMultiplier";

    private float minRewardMultiplier = 0.3f;
    private float maxRewardMultiplier = 3f;

    private float smoothingFactor = 0.5f; // For gradual changes
    private float trendBoostFactor = 0.1f; // To amplify consistent trends

    private readonly Dictionary<string, float> currentMultipliers = new Dictionary<string, float>();

    private string lastSymptom;
    private int sameSymptomCount;
    private float timeSinceLastSymptom;

    private void OnEnable()
    {
        ResetRuntimeState();
        SyncFromGlobalState();
    }

    public override void Apply(string variable, float value)
    {
        var diff = GlobalDifficultyState.Instance;
        if (diff == null)
        {
            Debug.LogWarning("[RewardSystemEffector] GlobalDifficultyState is missing. Reward adaptation was skipped.");
            return;
        }

        string normalizedVariable = NormalizeVariable(variable);
        if (string.IsNullOrEmpty(normalizedVariable))
        {
            Debug.LogWarning($"[RewardSystemEffector] Unknown variable: {variable}");
            return;
        }

        float newValue = GetNewMultiplier(normalizedVariable, value);

        switch (normalizedVariable)
        {
            case ItemDropRateVariable:
                diff.SetItemDropRate(newValue);
                break;
            case RewardMultiplierVariable:
                diff.SetRewardPayoutMultiplier(newValue);
                break;
        }

        Debug.Log(
            $"[RewardSystemEffector] Applied {normalizedVariable}: input={value:F2}, " +
            $"newMultiplier={newValue:F2}, itemDropRate={diff.itemDropRateMultiplier:F2}, " +
            $"rewardPayout={diff.rewardPayoutMultiplier:F2}");
    }

    private void ResetRuntimeState()
    {
        currentMultipliers.Clear();
        lastSymptom = null;
        sameSymptomCount = 0;
        timeSinceLastSymptom = 0f;
    }

    private void SyncFromGlobalState()
    {
        var diff = GlobalDifficultyState.Instance;
        currentMultipliers[ItemDropRateVariable] = diff != null ? diff.itemDropRateMultiplier : 1f;
        currentMultipliers[RewardMultiplierVariable] = diff != null ? diff.rewardPayoutMultiplier : 1f;
    }

    private static string NormalizeVariable(string variable)
    {
        switch (variable)
        {
            case "itemDropRate":
            case "itemDropRateMultiplier":
                return ItemDropRateVariable;

            case "rewardMultiplier":
            case "rewardPayoutMultiplier":
            case "goldRewardMultiplier":
            case "xpRewardMultiplier":
            case "scoreRewardMultiplier":
            case "eventRewardMultiplier":
                return RewardMultiplierVariable;

            default:
                return null;
        }
    }

    private float GetNewMultiplier(string variable, float value)
    {
        if (!currentMultipliers.ContainsKey(variable))
        {
            currentMultipliers[variable] = 1f;
        }

        if (Time.time - timeSinceLastSymptom > 1f)
        {
            DDAMAPEKit dda = DDAMAPEKit.TryGetExistingInstance();
            string currentSymptom = dda != null ? dda.GetCurrentSymptom()?.description ?? "None" : "None";
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
        newMultiplier = Mathf.Clamp(newMultiplier, minRewardMultiplier, maxRewardMultiplier);

        float smoothed = Mathf.Lerp(current, newMultiplier, smoothingFactor);

        currentMultipliers[variable] = smoothed;
        return smoothed;
    }
}
