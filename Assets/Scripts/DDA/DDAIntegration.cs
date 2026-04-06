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
    [SerializeField] private bool enableDDA = true;
    [SerializeField] private bool debugMode = true;

    public CombatMetricCollector combatMetricCollector;

    public static bool IsDDAEnabled
    {
        get
        {
            if (Instance == null)
                Instance = FindObjectOfType<DDAIntegration>();

            return Instance == null || Instance.enableDDA;
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
        if (!enableDDA) return;

        if(combatMetricCollector == null)
        {
            combatMetricCollector = gameObject.AddComponent<CombatMetricCollector>();
        }

        Room.OnRoomCleared += HandleRoomCleared;

        if (debugMode)
        {
            StartCoroutine(DebugDDAStatus());
        }
    }

    private void HandleRoomCleared(Room room)
    {
        combatMetricCollector?.FinalizeWaveMetrics();
        DDAMAPEKit.Instance?.TriggerMAPEKLoop();
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

        Room.OnRoomCleared -= HandleRoomCleared;
    }
}
