using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Tracks how much damage the player takes per room and reports a survivability score.
/// The score blends damage pressure, lowest health reached, and end-of-room health.
/// </summary>
public class SurvivabilitySensor : Sensor
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private float damageBudgetMultiplier = 1.5f;
    [SerializeField, Range(0f, 1f)] private float damageWeight = 0.5f;
    [SerializeField, Range(0f, 1f)] private float lowestHealthWeight = 0.3f;
    [SerializeField, Range(0f, 1f)] private float endHealthWeight = 0.2f;

    private float lastSurvivability = 1f;
    private float lastHealthValue = -1f;
    private float damageThisRoom = 0f;
    private float lowestHealthRatioThisRoom = 1f;
    private float endHealthRatioThisRoom = 1f;
    private bool isTrackingRoom = false;

    void Start()
    {
        attributeId = 3; // Survivability attribute ID
        attributeLabel = "Survivability";
        EnsurePlayerHealth();
        HookHealthEvents();
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
        UnhookHealthEvents();
    }

    public override SensorReading Read()
    {
        return new SensorReading(attributeId, lastSurvivability);
    }

    public void ConfigureScoring(
        float damageBudgetMultiplierValue,
        float damageWeightValue,
        float lowestHealthWeightValue,
        float endHealthWeightValue
    )
    {
        damageBudgetMultiplier = Mathf.Max(0.25f, damageBudgetMultiplierValue);
        damageWeight = Mathf.Clamp01(damageWeightValue);
        lowestHealthWeight = Mathf.Clamp01(lowestHealthWeightValue);
        endHealthWeight = Mathf.Clamp01(endHealthWeightValue);
    }

    private void HandleRoomCombatStarted(Room room)
    {
        EnsurePlayerHealth();
        if (playerHealth == null) return;

        isTrackingRoom = true;
        damageThisRoom = 0f;
        lastHealthValue = playerHealth.GetCurrentHealth();
        float currentHealthRatio = Mathf.Clamp01(lastHealthValue / Mathf.Max(1f, playerHealth.maxHealth));
        lowestHealthRatioThisRoom = currentHealthRatio;
        endHealthRatioThisRoom = currentHealthRatio;
    }

    private void HandleRoomCleared(Room room)
    {
        EnsurePlayerHealth();
        if (playerHealth == null) return;

        isTrackingRoom = false;
        float maxHealth = Mathf.Max(1f, playerHealth.maxHealth);
        float damageScore = 1f - Mathf.Clamp01(damageThisRoom / (maxHealth * damageBudgetMultiplier));
        float lowestHealthScore = Mathf.Clamp01(lowestHealthRatioThisRoom);
        endHealthRatioThisRoom = Mathf.Clamp01(playerHealth.GetCurrentHealth() / maxHealth);
        float endHealthScore = endHealthRatioThisRoom;

        float totalWeight = Mathf.Max(0.0001f, damageWeight + lowestHealthWeight + endHealthWeight);
        lastSurvivability = Mathf.Clamp01(
            (
                damageScore * damageWeight +
                lowestHealthScore * lowestHealthWeight +
                endHealthScore * endHealthWeight
            ) / totalWeight
        );

        Debug.Log(
            $"[SurvivabilitySensor] Room {room.roomIndex} damage {damageThisRoom:F1}, " +
            $"lowest {lowestHealthScore:F2}, end {endHealthScore:F2}, score {lastSurvivability:F2}"
        );

        damageThisRoom = 0f;
        lowestHealthRatioThisRoom = endHealthScore;
        endHealthRatioThisRoom = endHealthScore;
    }

    private void OnHealthChanged(float current, float max)
    {
        if (!isTrackingRoom)
        {
            lastHealthValue = current;
            return;
        }

        if (lastHealthValue < 0f)
        {
            lastHealthValue = current;
            return;
        }

        float delta = Mathf.Max(0f, lastHealthValue - current);
        damageThisRoom += delta;
        lastHealthValue = current;

        float currentHealthRatio = Mathf.Clamp01(current / Mathf.Max(1f, max));
        lowestHealthRatioThisRoom = Mathf.Min(lowestHealthRatioThisRoom, currentHealthRatio);
        endHealthRatioThisRoom = currentHealthRatio;
    }

    private void EnsurePlayerHealth()
    {
        if (playerHealth != null) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<Health>();
        }

        HookHealthEvents();
    }

    private void HookHealthEvents()
    {
        if (playerHealth != null)
        {
            UnhookHealthEvents();
            playerHealth.onHealthChanged += OnHealthChanged;
            lastHealthValue = playerHealth.GetCurrentHealth();
        }
    }

    private void UnhookHealthEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.onHealthChanged -= OnHealthChanged;
        }
    }
}
