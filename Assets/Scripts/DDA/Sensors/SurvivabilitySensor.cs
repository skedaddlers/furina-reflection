using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Tracks how much damage the player takes per room and reports a survivability score.
/// The score blends damage pressure, healing recovery, dodge avoidance, lowest health reached,
/// and end-of-room health.
/// </summary>
public class SurvivabilitySensor : Sensor
{
    protected override bool SupportsBossTickAnalysis => true;

    [SerializeField] private Health playerHealth;
    [SerializeField] private float damageBudgetMultiplier = 1.5f;
    [SerializeField, Range(0f, 1f)] private float damageWeight = 0.6f;
    [SerializeField, Range(0f, 1f)] private float successfulDodgedAttacksWeight = 0.1f;
    [SerializeField, Range(0f, 1f)] private float healingWeight = 0.1f;
    [SerializeField, Range(0f, 1f)] private float lowestHealthWeight = 0.15f;
    [SerializeField, Range(0f, 1f)] private float endHealthWeight = 0.15f;

    private float lastSurvivability = 1f;
    private float lastHealthValue = -1f;
    private float damageThisRoom = 0f;
    private float successfulDodgedDamageThisRoom = 0f;
    private float healingThisRoom = 0f;
    private float lowestHealthRatioThisRoom = 1f;
    private float endHealthRatioThisRoom = 1f;
    private bool isTrackingRoom = false;
    private bool roomSnapshotFinalized = false;
    private Room currentRoom;

    void Start()
    {
        attributeId = 3; // Survivability attribute ID
        attributeLabel = "Survivability";
        EnsurePlayerHealth();
        HookHealthEvents();
    }

    void OnEnable()
    {
        CombatEventManager.OnSuccessfulDodgeDamageAvoided += HandleSuccessfulDodgeDamageAvoided;
        CombatEventManager.OnHeal += HandleHeal;
        Room.OnRoomCombatStarted += HandleRoomCombatStarted;
        Room.OnRoomCleared += HandleRoomCleared;
    }

    void OnDisable()
    {
        CombatEventManager.OnSuccessfulDodgeDamageAvoided -= HandleSuccessfulDodgeDamageAvoided;
        CombatEventManager.OnHeal -= HandleHeal;
        Room.OnRoomCombatStarted -= HandleRoomCombatStarted;
        Room.OnRoomCleared -= HandleRoomCleared;
        UnhookHealthEvents();
    }

    public override SensorReading Read()
    {
        lastSurvivability = EvaluateCurrentSurvivability();
        Debug.Log($"[SurvivabilitySensor] Read called. Last Survivability: {lastSurvivability:F2}");
        return new SensorReading(attributeId, lastSurvivability);
    }

    public void ConfigureScoring(
        float damageBudgetMultiplierValue,
        float damageWeightValue,
        float successfulDodgedAttacksWeightValue,
        float healingWeightValue,
        float lowestHealthWeightValue,
        float endHealthWeightValue
    )
    {
        damageBudgetMultiplier = Mathf.Max(0.25f, damageBudgetMultiplierValue);
        damageWeight = Mathf.Clamp01(damageWeightValue);
        successfulDodgedAttacksWeight = Mathf.Clamp01(successfulDodgedAttacksWeightValue);
        healingWeight = Mathf.Clamp01(healingWeightValue);
        lowestHealthWeight = Mathf.Clamp01(lowestHealthWeightValue);
        endHealthWeight = Mathf.Clamp01(endHealthWeightValue);
    }

    private void HandleRoomCombatStarted(Room room)
    {
        EnsurePlayerHealth();
        if (playerHealth == null) return;

        currentRoom = room;
        isTrackingRoom = true;
        roomSnapshotFinalized = false;
        damageThisRoom = 0f;
        successfulDodgedDamageThisRoom = 0f;
        healingThisRoom = 0f;
        lastHealthValue = playerHealth.GetCurrentHealth();
        float currentHealthRatio = Mathf.Clamp01(lastHealthValue / Mathf.Max(1f, playerHealth.maxHealth));
        lowestHealthRatioThisRoom = currentHealthRatio;
        endHealthRatioThisRoom = currentHealthRatio;
    }

    private void HandleRoomCleared(Room room)
    {
        if (room == null || room != currentRoom)
            return;

        EnsurePlayerHealth();
        if (playerHealth == null) return;

        lastSurvivability = EvaluateCurrentSurvivability();
        lastHealthValue = playerHealth.GetCurrentHealth();
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
    private void HandleSuccessfulDodgeDamageAvoided(float damageAvoided)
    {
        if (!isTrackingRoom)
            return;

        successfulDodgedDamageThisRoom += Mathf.Max(0f, damageAvoided);
    }

    private void HandleHeal(float healAmount)
    {
        if (!isTrackingRoom)
            return;

        healingThisRoom += Mathf.Max(0f, healAmount);
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

    private float EvaluateCurrentSurvivability()
    {
        EnsurePlayerHealth();
        if (playerHealth == null || currentRoom == null)
            return lastSurvivability;

        if (roomSnapshotFinalized)
            return lastSurvivability;

        float maxHealth = Mathf.Max(1f, playerHealth.maxHealth);
        float survivabilityBudget = maxHealth * damageBudgetMultiplier;
        float damageScore = 1f - Mathf.Clamp01(damageThisRoom / survivabilityBudget);
        float totalDamage = damageThisRoom + successfulDodgedDamageThisRoom;
        float successfulDodgedAttacksScore = totalDamage > 0f ? Mathf.Clamp01(successfulDodgedDamageThisRoom / totalDamage) : 0f;
        float healingScore = Mathf.Clamp01(healingThisRoom / maxHealth);
        float lowestHealthScore = Mathf.Clamp01(lowestHealthRatioThisRoom);
        endHealthRatioThisRoom = Mathf.Clamp01(playerHealth.GetCurrentHealth() / maxHealth);
        float endHealthScore = endHealthRatioThisRoom;

        float totalWeight = Mathf.Max(
            0.0001f,
            damageWeight +
            successfulDodgedAttacksWeight +
            healingWeight +
            lowestHealthWeight +
            endHealthWeight
        );
        float survivability = Mathf.Clamp01(
            (
                damageScore * damageWeight +
                successfulDodgedAttacksScore * successfulDodgedAttacksWeight +
                healingScore * healingWeight +
                lowestHealthScore * lowestHealthWeight +
                endHealthScore * endHealthWeight
            ) / totalWeight
        );

        Debug.Log(
            $"[SurvivabilitySensor] Room {currentRoom.roomIndex} damage score: {damageScore:F2}, " +
            $"successful dodged attacks score: {successfulDodgedAttacksScore:F2}, healing score: {healingScore:F2}, " +
            $"lowest health score: {lowestHealthScore:F2}, " +
            $"end health score: {endHealthScore:F2}, final survivability: {survivability:F2}. "
        );

        if (!currentRoom.isInCombat)
        {
            isTrackingRoom = false;
            roomSnapshotFinalized = true;
            lastHealthValue = playerHealth.GetCurrentHealth();
        }

        return survivability;
    }
}
