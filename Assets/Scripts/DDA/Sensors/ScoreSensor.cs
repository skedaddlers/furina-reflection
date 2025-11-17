using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Sensor for monitoring player score/performance
/// </summary>
public class ScoreSensor : Sensor
{
    private int currentScore = 0;
    private float startTime;
    private int enemiesKilled = 0;

    void Start()
    {
        attributeId = 1; // Score attribute ID
        attributeLabel = "Score";
        startTime = Time.time;
    }

    public override SensorReading Read()
    {
        // Calculate score rate (score per minute)
        float timePlayed = Time.time - startTime;
        float scoreRate = timePlayed > 0 ? (currentScore / timePlayed) * 60f : 0f;
        
        return new SensorReading(attributeId, scoreRate);
    }

    public void AddScore(int points)
    {
        currentScore += points;
    }

    public void IncrementKills()
    {
        enemiesKilled++;
        AddScore(100); // 100 points per kill
    }
}