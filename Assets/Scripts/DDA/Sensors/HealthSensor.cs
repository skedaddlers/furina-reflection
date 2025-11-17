using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Sensor for monitoring player health
/// </summary>
public class HealthSensor : Sensor
{
    private Health playerHealth;
    private float maxHealth;

    void Start()
    {
        attributeId = 0; // Health attribute ID
        attributeLabel = "Health";
        
        // Find player health component
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            maxHealth = playerHealth.maxHealth;
        }
    }

    public override SensorReading Read()
    {
        if (playerHealth == null) return null;

        // Calculate health percentage
        float healthPercentage = playerHealth.GetCurrentHealth() / maxHealth;
        return new SensorReading(attributeId, healthPercentage);
    }
}