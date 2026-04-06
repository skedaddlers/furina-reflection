using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Effector for adjusting item and reward-related variables
/// </summary>
public class RewardSystemEffector : Effector
{
    // Base values for reward system
    private float baseItemDropRate = 0.2f;
    private float minRewardMultiplier = 0.3f;
    private float maxRewardMultiplier = 3f;

    private float smoothingFactor = 0.5f; // For gradual changes
    private float trendBoostFactor = 0.1f; // To amplify consistent trends

    private float currentItemDropRate;

    private string lastSymptom;
    private int sameSymptomCount;
    private float timeSinceLastSymptom;

    public override void Apply(string variable, float value)
    {
        var diff = GlobalDifficultyState.Instance;
        float newValue = GetNewMultiplier(variable, value);
        switch (variable)
        {
            case "itemDropRate":
                diff?.SetItemDropRate(newValue);
                break;
        }
    }

    private float GetNewMultiplier(string variable, float value)
    {
        if (Time.time - timeSinceLastSymptom > 1f)
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

        float newMultiplier =  currentItemDropRate * value;
        float boost = 1f + (sameSymptomCount * trendBoostFactor);

        newMultiplier = newMultiplier * boost;
        newMultiplier = Mathf.Clamp(newMultiplier, minRewardMultiplier, maxRewardMultiplier);

        float smoothed = Mathf.Lerp(currentItemDropRate, newMultiplier, smoothingFactor);

        currentItemDropRate = smoothed;
        return currentItemDropRate;
    }
}
