using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Sensor for monitoring player XP and gold gain rate
/// </summary>
public class ScoreSensor : Sensor
{
    private PlayerStats playerStats;
    private int startingXP;
    private int startingGold;
    private float startTime;
    private int enemiesKilled = 0;

    void Start()
    {
        attributeId = 1; // Economy attribute ID
        attributeLabel = "Economy";
        playerStats = PlayerStats.Instance != null ? PlayerStats.Instance : FindObjectOfType<PlayerStats>();
        startingXP = playerStats != null ? playerStats.currentXP : 0;
        startingGold = playerStats != null ? playerStats.Gold : 0;
        startTime = Time.time;
    }

    public override SensorReading Read()
    {
        if (playerStats == null)
        {
            playerStats = PlayerStats.Instance != null ? PlayerStats.Instance : FindObjectOfType<PlayerStats>();
            if (playerStats == null)
            {
                return new SensorReading(attributeId, 0f);
            }
        }

        // Combine XP and Gold gains into a single per-minute rate
        int xpGained = Mathf.Max(0, playerStats.currentXP - startingXP);
        int goldGained = Mathf.Max(0, playerStats.Gold - startingGold);
        float timePlayed = Mathf.Max(0.1f, Time.time - startTime);

        float economyPerMinute = ((float)xpGained + goldGained) / timePlayed * 60f;
        return new SensorReading(attributeId, economyPerMinute);
    }

    public void IncrementKills()
    {
        enemiesKilled++;
    }
}
