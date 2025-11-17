using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Sensor for monitoring player accuracy
/// </summary>
public class AccuracySensor : Sensor
{
    private int shotsFired = 0;
    private int shotsHit = 0;

    void Start()
    {
        attributeId = 2; // Accuracy attribute ID
        attributeLabel = "Accuracy";
    }

    public override SensorReading Read()
    {
        float accuracy = shotsFired > 0 ? (float)shotsHit / shotsFired : 1f;
        return new SensorReading(attributeId, accuracy);
    }

    public void RegisterShot()
    {
        shotsFired++;
    }

    public void RegisterHit()
    {
        shotsHit++;
    }
}






