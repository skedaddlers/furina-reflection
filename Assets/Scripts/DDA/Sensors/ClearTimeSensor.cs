using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Measures how long the player takes to clear combat in a room.
/// Reports a performance ratio: expectedClearTime / actualClearTime (higher = faster).
/// </summary>
public class ClearTimeSensor : Sensor
{
    [SerializeField] private float expectedClearTime = 30f; // seconds

    private float combatStartTime = -1f;
    private float lastClearTimeRatio = 1f;

    void Start()
    {
        attributeId = 4; // Clear time attribute ID
        attributeLabel = "ClearTime";
    }

    void OnEnable()
    {
        Room.OnRoomCombatStarted += HandleRoomCombatStarted;
        Room.OnRoomCleared += HandleRoomCleared;
    }

    void OnDisable()
    {
        Room.OnRoomCombatStarted -= HandleRoomCombatStarted;
        Room.OnRoomCleared -= HandleRoomCleared;
    }

    public void SetExpectedClearTime(float seconds)
    {
        expectedClearTime = Mathf.Max(1f, seconds);
    }

    public override SensorReading Read()
    {
        return new SensorReading(attributeId, lastClearTimeRatio);
    }

    private void HandleRoomCombatStarted(Room room)
    {
        combatStartTime = Time.time;
    }

    private void HandleRoomCleared(Room room)
    {
        if (combatStartTime < 0f)
        {
            lastClearTimeRatio = 1f;
            return;
        }

        float elapsed = Mathf.Max(0.1f, Time.time - combatStartTime);
        lastClearTimeRatio = Mathf.Clamp((expectedClearTime / elapsed), 0f, 3f);
        combatStartTime = -1f;
    }
}
