using System;
using System.Collections.Generic;
using System.IO;
using DDAMAPEKitFramework;
using UnityEngine;

public enum RunEndReason
{
    Unknown,
    Victory,
    Defeat,
    Restart,
    Quit,
    Destroyed
}

[DisallowMultipleComponent]
public class RunMetricsLogger : MonoBehaviour
{
    [Header("Local Logging")]
    [SerializeField] private bool enableLocalRunLogging = true;
    [SerializeField] private bool prettyPrintJson = true;
    [SerializeField] private string logFolderName = "RunLogs";

    [Header("Run Survivability Scoring")]
    [SerializeField] private float survivabilityDamageBudgetMultiplier = 1.5f;
    [SerializeField, Range(0f, 1f)] private float survivabilityDamageWeight = 0.5f;
    [SerializeField, Range(0f, 1f)] private float survivabilityLowestHealthWeight = 0.3f;
    [SerializeField, Range(0f, 1f)] private float survivabilityEndHealthWeight = 0.2f;

    private readonly HashSet<int> visitedProgressionRoomIds = new HashSet<int>();
    private readonly HashSet<int> startedCombatRoomIds = new HashSet<int>();
    private readonly HashSet<int> clearedCombatRoomIds = new HashSet<int>();

    private RunMetricsSnapshot currentRun;
    private Health playerHealth;
    private float survivabilityDamageBudget;
    private float totalCompletedCombatDuration;
    private float activeCombatStartTime = -1f;
    private int activeCombatRoomId = -1;
    private RoomType activeCombatRoomType = RoomType.Start;
    private int bossRoomId = -1;
    private bool isRunActive;

    void OnEnable()
    {
        CombatEventManager.OnMeleeAttack += HandleDamageDealt;
        CombatEventManager.OnRangedAttack += HandleDamageDealt;
        CombatEventManager.OnSkillAttack += HandleDamageDealt;
        CombatEventManager.OnDodgeAttempt += HandleDodgeAttempt;
        CombatEventManager.OnSuccessfulDodge += HandleSuccessfulDodge;
        CombatEventManager.OnSuccessfulDodgeDamageAvoided += HandleSuccessfulDodgeDamageAvoided;
        CombatEventManager.OnDamageTaken += HandleDamageTaken;

        Room.OnRoomCombatStarted += HandleRoomCombatStarted;
        Room.OnRoomCleared += HandleRoomCleared;
        RoomManager.OnRoomEntered += HandleRoomEntered;
        BossManager.OnBossPhaseProgressed += HandleBossPhaseProgressed;
    }

    void OnDisable()
    {
        CombatEventManager.OnMeleeAttack -= HandleDamageDealt;
        CombatEventManager.OnRangedAttack -= HandleDamageDealt;
        CombatEventManager.OnSkillAttack -= HandleDamageDealt;
        CombatEventManager.OnDodgeAttempt -= HandleDodgeAttempt;
        CombatEventManager.OnSuccessfulDodge -= HandleSuccessfulDodge;
        CombatEventManager.OnSuccessfulDodgeDamageAvoided -= HandleSuccessfulDodgeDamageAvoided;
        CombatEventManager.OnDamageTaken -= HandleDamageTaken;

        Room.OnRoomCombatStarted -= HandleRoomCombatStarted;
        Room.OnRoomCleared -= HandleRoomCleared;
        RoomManager.OnRoomEntered -= HandleRoomEntered;
        BossManager.OnBossPhaseProgressed -= HandleBossPhaseProgressed;

        UnhookPlayerHealth();
    }

    void OnApplicationQuit()
    {
        FinalizeAndPersist(RunEndReason.Quit);
    }

    void OnDestroy()
    {
        FinalizeAndPersist(RunEndReason.Destroyed);
    }

    public void BeginRun()
    {
        ResetRunState();

        if (!enableLocalRunLogging)
            return;

        isRunActive = true;
        currentRun = new RunMetricsSnapshot
        {
            runId = Guid.NewGuid().ToString("N"),
            startedAtUtc = DateTime.UtcNow.ToString("O"),
            appVersion = Application.version,
            platform = Application.platform.ToString(),
            deviceModel = SystemInfo.deviceModel,
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        };

        InitializeLayoutSnapshot();
        EnsurePlayerHealth();
        UpdateHealthRatiosFromCurrentState();
    }

    public void FinalizeAndPersist(RunEndReason reason)
    {
        if (!isRunActive || currentRun == null)
            return;

        EnsurePlayerHealth();

        if (reason == RunEndReason.Victory)
        {
            CompleteActiveCombatEncounter(countAsCleared: true);
        }

        FinalizeSnapshot(reason);

        try
        {
            string directoryPath = Path.Combine(Application.persistentDataPath, logFolderName);
            Directory.CreateDirectory(directoryPath);

            string fileName = $"run_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{currentRun.runId}.json";
            string fullPath = Path.Combine(directoryPath, fileName);
            currentRun.logFilePath = fullPath;

            string json = JsonUtility.ToJson(currentRun, prettyPrintJson);
            File.WriteAllText(fullPath, json);
            Debug.Log($"[RunMetricsLogger] Saved run metrics to: {fullPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RunMetricsLogger] Failed to save run metrics: {ex}");
        }
        finally
        {
            isRunActive = false;
        }
    }

    private void HandleDamageDealt(float damage)
    {
        if (!isRunActive || currentRun == null)
            return;

        currentRun.totalDamageDealt += Mathf.Max(0f, damage);
    }

    private void HandleDamageTaken(float damage)
    {
        if (!isRunActive || currentRun == null)
            return;

        currentRun.totalDamageTaken += Mathf.Max(0f, damage);
    }

    private void HandleDodgeAttempt()
    {
        if (!isRunActive || currentRun == null)
            return;

        currentRun.dodgeAttempts++;
    }

    private void HandleSuccessfulDodge()
    {
        if (!isRunActive || currentRun == null)
            return;

        currentRun.successfulDodges++;
    }

    private void HandleSuccessfulDodgeDamageAvoided(float damage)
    {
        if (!isRunActive || currentRun == null)
            return;

        currentRun.totalDamageAvoidedByDodging += Mathf.Max(0f, damage);
    }

    private void HandleRoomEntered(Room room)
    {
        if (!isRunActive || currentRun == null || room == null)
            return;

        InitializeLayoutSnapshot();

        if (room.roomType != RoomType.Start && room.roomType != RoomType.Shop)
        {
            visitedProgressionRoomIds.Add(room.roomIndex);
        }

        if (room.roomType == RoomType.Boss)
        {
            currentRun.reachedBoss = 1;
        }
    }

    private void HandleRoomCombatStarted(Room room)
    {
        if (!isRunActive || currentRun == null || room == null || !IsCombatRoom(room.roomType))
            return;

        InitializeLayoutSnapshot();
        EnsurePlayerHealth();

        activeCombatStartTime = Time.time;
        activeCombatRoomId = room.roomIndex;
        activeCombatRoomType = room.roomType;

        startedCombatRoomIds.Add(room.roomIndex);
        currentRun.startedCombatRooms = startedCombatRoomIds.Count;

        if (playerHealth != null)
        {
            survivabilityDamageBudget += Mathf.Max(1f, playerHealth.maxHealth) * survivabilityDamageBudgetMultiplier;
            UpdateHealthRatios(playerHealth.GetCurrentHealth(), playerHealth.maxHealth);
        }

        if (room.roomType == RoomType.Boss)
        {
            currentRun.reachedBoss = 1;
            currentRun.reachedBossPhase = Mathf.Max(currentRun.reachedBossPhase, 1);
        }
    }

    private void HandleRoomCleared(Room room)
    {
        if (!isRunActive || currentRun == null || room == null)
            return;

        CompleteActiveCombatEncounter(countAsCleared: true, expectedRoomId: room.roomIndex);
    }

    private void HandleBossPhaseProgressed(Room room, int phaseIndex)
    {
        if (!isRunActive || currentRun == null || room == null)
            return;

        currentRun.reachedBoss = 1;
        currentRun.reachedBossPhase = Mathf.Max(currentRun.reachedBossPhase, Mathf.Clamp(phaseIndex, 0, 3));
    }

    private void FinalizeSnapshot(RunEndReason reason)
    {
        currentRun.endedAtUtc = DateTime.UtcNow.ToString("O");
        currentRun.endReason = reason.ToString();
        currentRun.persistentDataPath = Application.persistentDataPath;
        currentRun.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        currentRun.victory = reason == RunEndReason.Victory ? 1 : 0;
        if (bossRoomId > 0 && visitedProgressionRoomIds.Contains(bossRoomId))
        {
            currentRun.reachedBoss = 1;
        }

        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            currentRun.score = gameManager.CurrentRunScore;
            currentRun.timePlayedSeconds = Mathf.Max(0f, gameManager.CurrentRunDuration);
        }

        currentRun.timePlayedFormatted = FormatDuration(currentRun.timePlayedSeconds);
        currentRun.progressionRoomsVisited = visitedProgressionRoomIds.Count;
        currentRun.clearedCombatRooms = clearedCombatRoomIds.Count;
        currentRun.averageClearTimeSeconds = currentRun.clearedCombatRooms > 0
            ? totalCompletedCombatDuration / currentRun.clearedCombatRooms
            : 0f;

        currentRun.progressRatePercent = CalculateProgressRatePercent();
        currentRun.totalDamageThatWouldHaveBeenTaken =
            currentRun.totalDamageTaken + currentRun.totalDamageAvoidedByDodging;
        currentRun.damageTakenOverPotentialDamage = currentRun.totalDamageThatWouldHaveBeenTaken > 0f
            ? currentRun.totalDamageTaken / currentRun.totalDamageThatWouldHaveBeenTaken
            : 0f;

        UpdateHealthRatiosFromCurrentState();
        currentRun.survivability = CalculateRunSurvivability();

        string performanceSource;
        currentRun.performance = CalculatePerformance(out performanceSource);
        currentRun.performanceSource = performanceSource;
    }

    private void CompleteActiveCombatEncounter(bool countAsCleared, int expectedRoomId = -1)
    {
        if (activeCombatStartTime < 0f || activeCombatRoomId < 0)
            return;

        if (!IsCombatRoom(activeCombatRoomType))
            return;

        if (expectedRoomId >= 0 && activeCombatRoomId != expectedRoomId)
            return;

        if (countAsCleared && clearedCombatRoomIds.Add(activeCombatRoomId))
        {
            totalCompletedCombatDuration += Mathf.Max(0f, Time.time - activeCombatStartTime);
        }

        currentRun.clearedCombatRooms = clearedCombatRoomIds.Count;
        activeCombatStartTime = -1f;
        activeCombatRoomId = -1;
        activeCombatRoomType = RoomType.Start;
    }

    private void InitializeLayoutSnapshot()
    {
        if (currentRun == null)
            return;

        RoomManager roomManager = GameManager.Instance != null ? GameManager.Instance.roomManager : null;
        DungeonLayout layout = roomManager != null ? roomManager.Layout : null;
        if (layout == null || layout.roomDataMap == null || layout.roomDataMap.Count == 0)
            return;

        currentRun.totalProgressionRooms = 0;
        currentRun.totalCombatRooms = 0;
        bossRoomId = -1;

        foreach (RoomData room in layout.roomDataMap.Values)
        {
            if (room.roomType != RoomType.Start && room.roomType != RoomType.Shop)
            {
                currentRun.totalProgressionRooms++;
            }

            if (IsCombatRoom(room.roomType))
            {
                currentRun.totalCombatRooms++;
            }

            if (room.roomType == RoomType.Boss)
            {
                bossRoomId = room.id;
            }
        }

        currentRun.dungeonSeed = GameManager.Instance != null && GameManager.Instance.roomGenerator != null
            ? GameManager.Instance.roomGenerator.dungeonConfig.seed
            : currentRun.dungeonSeed;
    }

    private float CalculateProgressRatePercent()
    {
        if (currentRun == null || currentRun.totalProgressionRooms <= 0)
            return 0f;

        return (visitedProgressionRoomIds.Count / (float)currentRun.totalProgressionRooms) * 100f;
    }

    private float CalculateRunSurvivability()
    {
        float totalWeight = survivabilityDamageWeight +
            survivabilityLowestHealthWeight +
            survivabilityEndHealthWeight;
        if (totalWeight <= 0f)
            return 0f;

        float budget = Mathf.Max(1f, survivabilityDamageBudget);
        float damageScore = 1f - Mathf.Clamp01(currentRun.totalDamageTaken / budget);
        float lowestHealthScore = Mathf.Clamp01(currentRun.lowestHealthRatio);
        float endHealthScore = Mathf.Clamp01(currentRun.endHealthRatio);

        return Mathf.Clamp01(
            (
                damageScore * survivabilityDamageWeight +
                lowestHealthScore * survivabilityLowestHealthWeight +
                endHealthScore * survivabilityEndHealthWeight
            ) / totalWeight
        );
    }

    private float CalculatePerformance(out string source)
    {
        DDAMAPEKit dda = DDAMAPEKit.TryGetExistingInstance();
        PlayerModel playerModel = dda != null && dda.IsInitialized ? dda.GetPlayerModel() : null;
        if (playerModel != null)
        {
            float weightedSum = 0f;
            float totalWeight = 0f;

            foreach (PlayerAttribute attribute in playerModel.GetAllAttributes())
            {
                float reference = attribute.reference.GetReference();
                if (reference <= 0f)
                    continue;

                weightedSum += (attribute.value / reference) * attribute.weight;
                totalWeight += attribute.weight;
            }

            if (totalWeight > 0f)
            {
                source = "dda_attributes";
                return weightedSum / totalWeight;
            }
        }

        source = "local_fallback";
        return CalculateFallbackPerformance();
    }

    private float CalculateFallbackPerformance()
    {
        float progressScore = Mathf.Clamp01(currentRun.progressRatePercent / 100f);
        float survivabilityScore = Mathf.Clamp01(currentRun.survivability);
        float damageEfficiency = currentRun.totalDamageDealt + currentRun.totalDamageTaken > 0f
            ? currentRun.totalDamageDealt / (currentRun.totalDamageDealt + currentRun.totalDamageTaken)
            : 0f;
        float victoryScore = currentRun.victory;

        return Mathf.Clamp01(
            progressScore * 0.35f +
            survivabilityScore * 0.35f +
            damageEfficiency * 0.2f +
            victoryScore * 0.1f
        );
    }

    private void EnsurePlayerHealth()
    {
        if (playerHealth != null)
            return;

        Health candidate = null;

        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            candidate = GameManager.Instance.player.GetComponent<Health>();
        }

        if (candidate == null && PlayerStats.Instance != null)
        {
            candidate = PlayerStats.Instance.health != null
                ? PlayerStats.Instance.health
                : PlayerStats.Instance.GetComponent<Health>();
        }

        if (candidate == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                candidate = playerObject.GetComponent<Health>();
            }
        }

        if (candidate == null)
        {
            return;
        }

        playerHealth = candidate;
        playerHealth.onHealthChanged -= HandlePlayerHealthChanged;
        playerHealth.onHealthChanged += HandlePlayerHealthChanged;
    }

    private void UnhookPlayerHealth()
    {
        if (playerHealth == null)
            return;

        playerHealth.onHealthChanged -= HandlePlayerHealthChanged;
        playerHealth = null;
    }

    private void HandlePlayerHealthChanged(float current, float max)
    {
        if (!isRunActive || currentRun == null)
            return;

        UpdateHealthRatios(current, max);
    }

    private void UpdateHealthRatiosFromCurrentState()
    {
        if (currentRun == null)
            return;

        EnsurePlayerHealth();
        if (playerHealth == null)
            return;

        UpdateHealthRatios(playerHealth.GetCurrentHealth(), playerHealth.maxHealth);
    }

    private void UpdateHealthRatios(float current, float max)
    {
        if (currentRun == null || max <= 0f)
            return;

        float ratio = Mathf.Clamp01(current / max);
        currentRun.lowestHealthRatio = Mathf.Min(currentRun.lowestHealthRatio, ratio);
        currentRun.endHealthRatio = ratio;
    }

    private void ResetRunState()
    {
        isRunActive = false;
        currentRun = null;
        survivabilityDamageBudget = 0f;
        totalCompletedCombatDuration = 0f;
        activeCombatStartTime = -1f;
        activeCombatRoomId = -1;
        activeCombatRoomType = RoomType.Start;
        bossRoomId = -1;

        visitedProgressionRoomIds.Clear();
        startedCombatRoomIds.Clear();
        clearedCombatRoomIds.Clear();

        UnhookPlayerHealth();
    }

    private static bool IsCombatRoom(RoomType roomType)
    {
        return roomType == RoomType.Normal ||
            roomType == RoomType.Elite ||
            roomType == RoomType.Boss;
    }

    private static string FormatDuration(float seconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
        return timeSpan.TotalHours >= 1d
            ? timeSpan.ToString(@"hh\:mm\:ss")
            : timeSpan.ToString(@"mm\:ss");
    }
}

[Serializable]
public class RunMetricsSnapshot
{
    public string runId;
    public string startedAtUtc;
    public string endedAtUtc;
    public string endReason;
    public string appVersion;
    public string platform;
    public string deviceModel;
    public string sceneName;
    public string persistentDataPath;
    public string logFilePath;
    public int dungeonSeed;

    public int score;
    public float timePlayedSeconds;
    public string timePlayedFormatted;

    public float totalDamageTaken;
    public float totalDamageDealt;
    public float totalDamageAvoidedByDodging;
    public float totalDamageThatWouldHaveBeenTaken;
    public float damageTakenOverPotentialDamage;

    public float averageClearTimeSeconds;
    public float survivability;
    public float performance;
    public string performanceSource;

    public float progressRatePercent;
    public int progressionRoomsVisited;
    public int totalProgressionRooms;
    public int startedCombatRooms;
    public int clearedCombatRooms;
    public int totalCombatRooms;

    public int dodgeAttempts;
    public int successfulDodges;

    public float lowestHealthRatio = 1f;
    public float endHealthRatio = 1f;

    public int reachedBoss;
    public int reachedBossPhase;
    public int victory;
}
