using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Tracks how much damage the player takes per room and reports a survivability score.
/// Score is 1 when no damage was taken and trends toward 0 as more health is lost.
/// </summary>
public class SurvivabilitySensor : Sensor
{
    [SerializeField] private Health playerHealth;

    private float lastSurvivability = 1f;
    private float lastHealthValue = -1f;
    private float damageThisRoom = 0f;

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

    private void HandleRoomCombatStarted(Room room)
    {
        EnsurePlayerHealth();
        if (playerHealth == null) return;

        damageThisRoom = 0f;
        lastHealthValue = playerHealth.GetCurrentHealth();
    }

    private void HandleRoomCleared(Room room)
    {
        EnsurePlayerHealth();
        if (playerHealth == null) return;

        float maxHealth = Mathf.Max(1f, playerHealth.maxHealth);
        float survivability = Mathf.Clamp01(1f - (damageThisRoom / maxHealth));
        lastSurvivability = survivability;
        damageThisRoom = 0f;
    }

    private void OnHealthChanged(float current, float max)
    {
        if (lastHealthValue < 0f)
        {
            lastHealthValue = current;
            return;
        }

        float delta = Mathf.Max(0f, lastHealthValue - current);
        damageThisRoom += delta;
        lastHealthValue = current;
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
