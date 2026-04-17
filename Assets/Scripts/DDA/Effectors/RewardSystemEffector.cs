using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Effector for adjusting item and reward-related variables
/// </summary>
public class RewardSystemEffector : Effector
{
    private float minRewardMultiplier = 0.3f;
    private float maxRewardMultiplier = 3f;

    private float smoothingFactor = 0.5f; // For gradual changes
    private float trendBoostFactor = 0.1f; // To amplify consistent trends

    private float currentItemDropRate = 1f;

    private string lastSymptom;
    private int sameSymptomCount;
    private float timeSinceLastSymptom;

    public override void Apply(string variable, float value)
    {
        var diff = GlobalDifficultyState.Instance;
        switch (variable)
        {
            case "itemDropRate":
                float newValue = GetNewMultiplier(variable, value);
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

        float smoothed = Mathf.Lerp(currentItemDropRate, newMultiplier, smoothingFactor);

        currentItemDropRate = smoothed;
        return currentItemDropRate;
    }
}
