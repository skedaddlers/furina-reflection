using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Measures how long the player takes to clear combat in a room.
/// Reports a performance ratio: expectedClearTime / actualClearTime (higher = faster).
/// The expected time is derived from the room's actual combat budget instead of a flat constant.
/// </summary>
public class ClearTimeSensor : Sensor
{
    [SerializeField] private float fallbackExpectedClearTime = 45f;
    [SerializeField] private float baseRoomOverheadSeconds = 8f;
    [SerializeField] private float secondsPerEnemy = 7f;
    [SerializeField] private float aoeFactor = 0.75f; // How much multiple enemies should increase expected clear time (0.75 = 25% less than linear)
    [SerializeField] private float perWaveSetupSeconds = 3f;
    [SerializeField] private float eliteRoomMultiplier = 1.35f;
    [SerializeField] private float bossExpectedClearTimeSeconds = 110f;
    [SerializeField, Range(0f, 1f)] private float enemyHealthInfluence = 0.75f;

    private float combatStartTime = -1f;
    private float lastClearTimeRatio = 1f;
    private float expectedClearTimeForActiveRoom = -1f;
    private Room currentRoom;

    void Start()
    {
        attributeId = 4; // Clear time attribute ID
        attributeLabel = "ClearTime";
    }

    void OnEnable()
    {
        Room.OnRoomCombatStarted += HandleRoomCombatStarted;
    }

    void OnDisable()
    {
        Room.OnRoomCombatStarted -= HandleRoomCombatStarted;
    }

    public void SetExpectedClearTime(float seconds)
    {
        fallbackExpectedClearTime = Mathf.Max(1f, seconds);
    }

    public void ConfigureDynamicBudget(
        float fallbackSeconds,
        float roomOverheadSeconds,
        float secondsPerEnemyValue,
        float aoeFactorValue,
        float perWaveSetupSecondsValue,
        float eliteMultiplier,
        float bossExpectedSeconds,
        float healthInfluence
    )
    {
        fallbackExpectedClearTime = Mathf.Max(1f, fallbackSeconds);
        baseRoomOverheadSeconds = Mathf.Max(0f, roomOverheadSeconds);
        secondsPerEnemy = Mathf.Max(0.25f, secondsPerEnemyValue);
        aoeFactor = Mathf.Clamp01(aoeFactorValue);
        perWaveSetupSeconds = Mathf.Max(0f, perWaveSetupSecondsValue);
        eliteRoomMultiplier = Mathf.Max(1f, eliteMultiplier);
        bossExpectedClearTimeSeconds = Mathf.Max(1f, bossExpectedSeconds);
        enemyHealthInfluence = Mathf.Clamp01(healthInfluence);
    }

    public override SensorReading Read()
    {
        HandleRoomCleared(currentRoom);
        Debug.Log($"[ClearTimeSensor] Read called. Last Clear Time Ratio: {lastClearTimeRatio:F2}, Expected Clear Time for Active Room: {expectedClearTimeForActiveRoom:F1}s");
        return new SensorReading(attributeId, lastClearTimeRatio);
    }

    private void HandleRoomCombatStarted(Room room)
    {
        combatStartTime = Time.time;
        expectedClearTimeForActiveRoom = CalculateExpectedClearTime(room);
        currentRoom = room;
    }

    private void HandleRoomCleared(Room room)
    {
        if (combatStartTime < 0f)
        {
            lastClearTimeRatio = 1f;
            return;
        }

        float elapsed = Mathf.Max(0.1f, Time.time - combatStartTime);
        float expected = expectedClearTimeForActiveRoom > 0f
            ? expectedClearTimeForActiveRoom
            : CalculateExpectedClearTime(room);

        lastClearTimeRatio = Mathf.Clamp(expected / elapsed, 0f, 3f);
        Debug.Log(
            $"[ClearTimeSensor] Room {room.roomIndex} expected {expected:F1}s, actual {elapsed:F1}s, ratio {lastClearTimeRatio:F2}"
        );

        combatStartTime = -1f;
        expectedClearTimeForActiveRoom = -1f;
    }

    private float CalculateExpectedClearTime(Room room)
    {
        if (room == null)
            return fallbackExpectedClearTime;

        float healthFactor = 1f;
        var diff = GlobalDifficultyState.Instance;
        if (diff != null)
        {
            var snapshot = diff.GetEnemyDifficultySnapshot();
            healthFactor = Mathf.Lerp(1f, snapshot.health, enemyHealthInfluence);
        }

        if (room.roomType == RoomType.Boss)
        {
            return Mathf.Max(5f, bossExpectedClearTimeSeconds * healthFactor);
        }

        int enemiesPerWave = Mathf.Max(1, room.enemyCount);
        int waveCount = Mathf.Max(1, room.waveCount);
        
        float expected = baseRoomOverheadSeconds;

        for(int i = 0; i < waveCount; i++)
        {
            float effectiveEnemies = Mathf.Pow(enemiesPerWave, aoeFactor);
            expected += effectiveEnemies * secondsPerEnemy;
        }

        if (waveCount > 1)
        {
            expected += (waveCount - 1) * (perWaveSetupSeconds + room.timeBetweenWaves);
        }

        if (room.roomType == RoomType.Elite)
        {
            expected *= eliteRoomMultiplier;
        }

        expected *= healthFactor;

        return Mathf.Max(5f, expected);
    }
}
