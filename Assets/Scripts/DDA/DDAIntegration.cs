using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Example integration of DDA-MAPEKit into your game
/// Attach this to your GameManager or a dedicated DDA GameObject
/// </summary>
public class DDAIntegration : MonoBehaviour
{
    [Header("DDA Settings")]
    [SerializeField] private bool enableDDA = true;
    [SerializeField] private bool debugMode = true;

    private DDAConfigurationManager configManager;
    private ScoreSensor scoreSensor;
    private AccuracySensor accuracySensor;

    void Awake()
    {
        if (!enableDDA) return;

        // Setup DDA framework
        GameObject ddaObject = new GameObject("DDA System");
        DontDestroyOnLoad(ddaObject);

        // Add DDA-MAPEKit main component
        var ddaFramework = ddaObject.AddComponent<DDAMAPEKit>();

        // Add configuration manager
        configManager = ddaObject.AddComponent<DDAConfigurationManager>();
    }

    void Start()
    {
        if (!enableDDA) return;

        // Get sensor references for game events
        scoreSensor = GetComponent<ScoreSensor>();
        accuracySensor = GetComponent<AccuracySensor>();

        // Subscribe to game events
        SubscribeToGameEvents();

        if (debugMode)
        {
            StartCoroutine(DebugDDAStatus());
        }
    }

    private void SubscribeToGameEvents()
    {
        // Subscribe to enemy death events to update score
        Health.OnAnyDeath += OnEnemyDeath;
        
        // Subscribe to player shooting events for accuracy tracking
        if (PlayerCombat.Instance != null)
        {
            // You'll need to add these events to PlayerCombat
            // PlayerCombat.Instance.onShoot += OnPlayerShoot;
            // PlayerCombat.Instance.onHit += OnPlayerHit;
        }
    }

    private void OnEnemyDeath(Health enemyHealth)
    {
        if (scoreSensor != null)
        {
            scoreSensor.IncrementKills();
        }
    }

    private void OnPlayerShoot()
    {
        if (accuracySensor != null)
        {
            accuracySensor.RegisterShot();
        }
    }

    private void OnPlayerHit()
    {
        if (accuracySensor != null)
        {
            accuracySensor.RegisterHit();
        }
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
        // Cleanup event subscriptions
        Health.OnAnyDeath -= OnEnemyDeath;
    }
}
