using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Example integration of DDA-MAPEKit into your game
/// Attach this to your GameManager or a dedicated DDA GameObject
/// </summary>
public class DDAIntegration : MonoBehaviour
{
    public static DDAIntegration Instance { get; private set; }

    [Header("DDA Settings")]
    [Tooltip("Keeps the monitor/analyzer pipeline running so player performance is still tracked per room clear.")]
    [SerializeField] private bool enablePerformanceTracking = true;
    [Tooltip("Applies adaptive changes through the planner/executor. Disable this for monitor/analyzer-only mode.")]
    [SerializeField] private bool enableDDA = true;
    [Tooltip("Runs additional DDA/profile refresh loops while a boss-room combat is active so the boss can react mid-fight.")]
    [SerializeField] private bool enableBossFightLoops = false;
    [SerializeField] private float bossFightLoopIntervalSeconds = 5f;
    [SerializeField] private bool analyzeNoMatterWhat = false; // If true, will trigger analysis on every observation regardless of thresholds
    [SerializeField] private bool debugMode = true;

    public CombatMetricCollector combatMetricCollector;
    private Coroutine bossFightLoopCoroutine;
    private Room activeBossRoom;

    public static bool IsTrackingEnabled
    {
        get
        {
            if (Instance == null)
                Instance = FindObjectOfType<DDAIntegration>();

            return Instance == null || Instance.enablePerformanceTracking;
        }
    }

    public static bool AnalyzeNoMatterWhat
    {
        get
        {
            return Instance != null && Instance.analyzeNoMatterWhat;
        }
    }

    public static bool IsDDAEnabled
    {
        get
        {
            return IsTrackingEnabled;
        }
    }

    public static bool IsAdaptationEnabled
    {
        get
        {
            if (Instance == null)
                Instance = FindObjectOfType<DDAIntegration>();

            return Instance == null || (Instance.enablePerformanceTracking && Instance.enableDDA);
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
            return;

        Instance = this;

        // if (!enableDDA) return;

        // // Setup DDA framework
        // GameObject ddaObject = new GameObject("DDA System");
        // DontDestroyOnLoad(ddaObject);

        // // Add DDA-MAPEKit main component
        // var ddaFramework = ddaObject.AddComponent<DDAMAPEKit>();

        // // Add configuration manager
        // configManager = ddaObject.AddComponent<DDAConfigurationManager>();
    }

    void Start()
    {
        if (!IsTrackingEnabled) return;

        if(combatMetricCollector == null)
        {
            combatMetricCollector = gameObject.AddComponent<CombatMetricCollector>();
        }

        BossManager.OnBossFightActivityChanged += HandleBossFightActivityChanged;
        Room.OnRoomCleared += HandleRoomCleared;

        if (debugMode)
        {
            StartCoroutine(DebugDDAStatus());
        }
    }

    private void HandleBossFightActivityChanged(Room room, bool isActive)
    {
        if (!enableBossFightLoops || room == null || room.roomType != RoomType.Boss)
            return;

        if (!isActive)
        {
            if (room == activeBossRoom)
            {
                StopBossFightLoop();
            }
            return;
        }

        activeBossRoom = room;
        StopBossFightLoop();
        bossFightLoopCoroutine = StartCoroutine(BossFightLoop());
    }

    private void HandleRoomCleared(Room room)
    {
        if (room == activeBossRoom)
        {
            activeBossRoom = null;
            StopBossFightLoop();
        }

        combatMetricCollector?.FinalizeWaveMetrics();
        DDAMAPEKit.Instance?.TriggerMAPEKLoop();
    }

    private System.Collections.IEnumerator BossFightLoop()
    {
        float loopInterval = Mathf.Max(0.25f, bossFightLoopIntervalSeconds);

        while (activeBossRoom != null && activeBossRoom.isInCombat)
        {
            yield return new WaitForSeconds(loopInterval);

            if (activeBossRoom == null || !activeBossRoom.isInCombat)
                break;

            if (combatMetricCollector != null && combatMetricCollector.PublishCurrentCombatMetrics())
            {
                DDAMAPEKit.Instance?.TriggerMAPEKLoop();
            }
        }

        bossFightLoopCoroutine = null;
    }

    private System.Collections.IEnumerator DebugDDAStatus()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            var playerModel = DDAMAPEKit.Instance.GetPlayerModel();
            if (playerModel != null)
            {
                var currentProfile = playerModel.GetCurrentProfile();
                Debug.Log($"[DDA Debug] Current Profile: {currentProfile?.name ?? "None"}");

                foreach (var attr in playerModel.GetAllAttributes())
                {
                    Debug.Log($"[DDA Debug] {attr.label}: {attr.value:F2} (Ref: {attr.reference.GetReference():F2})");
                }
            }
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        StopBossFightLoop();
        BossManager.OnBossFightActivityChanged -= HandleBossFightActivityChanged;
        Room.OnRoomCleared -= HandleRoomCleared;
    }

    private void StopBossFightLoop()
    {
        if (bossFightLoopCoroutine == null)
            return;

        StopCoroutine(bossFightLoopCoroutine);
        bossFightLoopCoroutine = null;
    }
}
