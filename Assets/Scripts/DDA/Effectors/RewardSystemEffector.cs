using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Effector for adjusting item and reward-related variables
/// </summary>
public class RewardSystemEffector : Effector
{
    // Base values for reward system
    private float baseItemDropRate = 0.2f;
    private int baseHealthPackValue = 25;
    private int baseScoreMultiplier = 1;

    public override void Apply(string variable, float value)
    {
        switch (variable)
        {
            case "itemDropRate":
                AdjustItemDropRate(value);
                break;
            case "healthPackValue":
                AdjustHealthPackValue((int)value);
                break;
            case "scoreMultiplier":
                AdjustScoreMultiplier(value);
                break;
        }
    }

    private void AdjustItemDropRate(float adjustment)
    {
        float newRate = baseItemDropRate + (adjustment * 0.01f);
        newRate = Mathf.Clamp(newRate, 0.05f, 0.8f);
        
        // Apply to item spawning system
        // This would need to be integrated with your item spawning logic
        Debug.Log($"[RewardSystemEffector] Item drop rate adjusted to: {newRate}");
    }

    private void AdjustHealthPackValue(int adjustment)
    {
        int newValue = baseHealthPackValue + adjustment;
        newValue = Mathf.Max(10, newValue);
        
        // Apply to health pack items
        Debug.Log($"[RewardSystemEffector] Health pack value adjusted to: {newValue}");
    }

    private void AdjustScoreMultiplier(float adjustment)
    {
        float newMultiplier = baseScoreMultiplier + (adjustment * 0.1f);
        newMultiplier = Mathf.Max(0.5f, newMultiplier);
        
        // Apply to scoring system
        Debug.Log($"[RewardSystemEffector] Score multiplier adjusted to: {newMultiplier}");
    }
}
