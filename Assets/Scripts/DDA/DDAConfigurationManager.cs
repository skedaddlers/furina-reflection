using UnityEngine;
using DDAMAPEKitFramework;

/// <summary>
/// Configuration manager for DDA-MAPEKit
/// Sets up default rules, symptoms, and player attributes
/// </summary>
public class DDAConfigurationManager : MonoBehaviour
{
    [Header("DDA Configuration")]
    [SerializeField] private bool autoInitialize = true;
    [SerializeField] private bool useCorePerformanceOnly = true;

    [Header("Player Attribute Thresholds")]
    [SerializeField] private Vector2 healthThreshold = new Vector2(0.3f, 1.0f);
    [SerializeField] private Vector2 scoreThreshold = new Vector2(50f, 500f);
    [SerializeField] private Vector2 accuracyThreshold = new Vector2(0.2f, 0.8f);
    [SerializeField] private Vector2 survivabilityThreshold = new Vector2(0.5f, 1.0f);
    [SerializeField] private Vector2 clearTimeThreshold = new Vector2(0.7f, 1.3f); // ratio around 1 is expected

    [Header("Reference Values")]
    [SerializeField] private float healthReference = 0.7f;
    [SerializeField] private float scoreReference = 200f;
    [SerializeField] private float accuracyReference = 0.5f;
    [SerializeField] private float survivabilityReference = 0.85f;
    [SerializeField] private float expectedClearTimeSeconds = 45f;
    [SerializeField] private float clearTimeReference = 1f;

    [Header("Player Attribute Weights")]
    [SerializeField] private float healthWeight = 1f;
    [SerializeField] private float scoreWeight = 1f;
    [SerializeField] private float accuracyWeight = 0.5f;
    [SerializeField] private float survivabilityWeight = 1.2f;
    [SerializeField] private float clearTimeWeight = 1f;

    [Header("Dynamic Clear Time Tuning")]
    [SerializeField] private float clearTimeFallbackSeconds = 45f;
    [SerializeField] private float clearTimeBaseRoomOverheadSeconds = 8f;
    [SerializeField] private float clearTimePerEnemySeconds = 7f;
    [SerializeField] private float aoeFactor = 0.75f; // How much multiple enemies should increase expected clear time (0.75 = 25% less than linear)
    [SerializeField] private float clearTimePerWaveSetupSeconds = 3f;
    [SerializeField] private float clearTimeEliteMultiplier = 1.35f;
    [SerializeField] private float clearTimeBossExpectedSeconds = 110f;
    [SerializeField, Range(0f, 1f)] private float clearTimeEnemyHealthInfluence = 0.75f;

    [Header("Survivability Tuning")]
    [SerializeField] private float survivabilityDamageBudgetMultiplier = 1.5f;
    [SerializeField, Range(0f, 1f)] private float survivabilityDamageWeight = 0.5f;
    [SerializeField, Range(0f, 1f)] private float survivabilityLowestHealthWeight = 0.3f;
    [SerializeField, Range(0f, 1f)] private float survivabilityEndHealthWeight = 0.2f;


    [Header("Rules (Designer editable)")]
    [SerializeField] private RuleConfig[] rules;

    private DDAMAPEKit ddaFramework;

    void Start()
    {
        if (autoInitialize)
        {
            InitializeDDA();
        }
    }

    public void InitializeDDA()
    {
        ddaFramework = DDAMAPEKit.Instance;

        // Configure player attributes
        ConfigurePlayerAttributes();

        // Configure symptoms
        ConfigureSymptoms();

        // Configure rules for each profile
        ConfigureRules();

        // Register sensors
        RegisterSensors();

        // Register effectors
        RegisterEffectors();

        Debug.Log("[DDAConfiguration] DDA System configured successfully");
    }

    private void ConfigurePlayerAttributes()
    {
        if (useCorePerformanceOnly)
        {
            ConfigureCorePerformanceAttributes();
            return;
        }

        ConfigureFullPerformanceAttributes();
    }

    private void ConfigureCorePerformanceAttributes()
    {
        var survivabilityAttr = new PlayerAttribute(3, "Survivability", survivabilityThreshold.x, survivabilityThreshold.y);
        survivabilityAttr.reference.SetStaticReference(survivabilityReference);
        survivabilityAttr.weight = survivabilityWeight;
        ddaFramework.AddPlayerAttribute(survivabilityAttr);

        var clearTimeAttr = new PlayerAttribute(4, "ClearTime", clearTimeThreshold.x, clearTimeThreshold.y);
        clearTimeAttr.reference.SetStaticReference(clearTimeReference);
        clearTimeAttr.weight = clearTimeWeight;
        ddaFramework.AddPlayerAttribute(clearTimeAttr);
    }

    private void ConfigureFullPerformanceAttributes()
    {
        // Health attribute
        var healthAttr = new PlayerAttribute(0, "Health", healthThreshold.x, healthThreshold.y);
        healthAttr.reference.SetStaticReference(healthReference);
        healthAttr.weight = healthWeight;
        ddaFramework.AddPlayerAttribute(healthAttr);

        // Economy attribute (XP + Gold gain rate)
        var scoreAttr = new PlayerAttribute(1, "Economy", scoreThreshold.x, scoreThreshold.y);
        scoreAttr.reference.SetStaticReference(scoreReference);
        scoreAttr.weight = scoreWeight;
        ddaFramework.AddPlayerAttribute(scoreAttr);

        // Accuracy attribute
        var accuracyAttr = new PlayerAttribute(2, "Accuracy", accuracyThreshold.x, accuracyThreshold.y);
        accuracyAttr.reference.SetStaticReference(accuracyReference);
        accuracyAttr.weight = accuracyWeight;
        ddaFramework.AddPlayerAttribute(accuracyAttr);

        var survivabilityAttr = new PlayerAttribute(3, "Survivability", survivabilityThreshold.x, survivabilityThreshold.y);
        survivabilityAttr.reference.SetStaticReference(survivabilityReference);
        survivabilityAttr.weight = survivabilityWeight;
        ddaFramework.AddPlayerAttribute(survivabilityAttr);

        var clearTimeAttr = new PlayerAttribute(4, "ClearTime", clearTimeThreshold.x, clearTimeThreshold.y);
        clearTimeAttr.reference.SetStaticReference(clearTimeReference);
        clearTimeAttr.weight = clearTimeWeight;
        ddaFramework.AddPlayerAttribute(clearTimeAttr);
    }

    private void ConfigureSymptoms()
    {
        // Already configured in DDAMAPEKit default initialization
        // But we can add more specific ones if needed
    }

    // private void ConfigureRules()
    // {
    //     var policyEngine = ddaFramework.GetPolicyEngine();

    //     // Rules for Killer profile (Profile 0)
    //     ConfigureKillerRules(policyEngine);

    //     // Rules for Achiever profile (Profile 1)
    //     ConfigureAchieverRules(policyEngine);

    //     // Rules for Explorer profile (Profile 2)
    //     ConfigureExplorerRules(policyEngine);
    // }

    // private void ConfigureKillerRules(PolicyEngine engine)
    // {
    //     // Killer: Enjoys combat and challenge

    //     // Very high performance - increase difficulty significantly
    //     var veryHighAction = new GameAction();
    //     veryHighAction.AddVariableChange("enemySpawnRate", -0.8f); // Faster spawns
    //     veryHighAction.AddVariableChange("maxEnemies", 3);
    //     veryHighAction.AddVariableChange("enemyDamage", 5);
    //     veryHighAction.AddVariableChange("enemySpeed", 0.8f);
    //     engine.AddRule(new Rule("killer_very_high", "very.high", 0, veryHighAction));

    //     // High performance - increase difficulty
    //     var highAction = new GameAction();
    //     highAction.AddVariableChange("enemySpawnRate", -0.5f);
    //     highAction.AddVariableChange("maxEnemies", 2);
    //     highAction.AddVariableChange("enemyDamage", 3);
    //     engine.AddRule(new Rule("killer_high", "high", 0, highAction));

    //     // Low performance - decrease difficulty
    //     var lowAction = new GameAction();
    //     lowAction.AddVariableChange("enemySpawnRate", 0.5f);
    //     lowAction.AddVariableChange("maxEnemies", -2);
    //     lowAction.AddVariableChange("enemyDamage", -3);
    //     lowAction.AddVariableChange("itemDropRate", 2);
    //     engine.AddRule(new Rule("killer_low", "low", 0, lowAction));

    //     // Very low performance - help the player significantly
    //     var veryLowAction = new GameAction();
    //     veryLowAction.AddVariableChange("enemySpawnRate", 1.0f);
    //     veryLowAction.AddVariableChange("maxEnemies", -3);
    //     veryLowAction.AddVariableChange("enemyDamage", -5);
    //     veryLowAction.AddVariableChange("playerDamageBonus", 2);
    //     veryLowAction.AddVariableChange("itemDropRate", 5);
    //     engine.AddRule(new Rule("killer_very_low", "very.low", 0, veryLowAction));
    // }

    // private void ConfigureAchieverRules(PolicyEngine engine)
    // {
    //     // Achiever: Focuses on score and progression

    //     // Very high performance - increase challenge and rewards
    //     var veryHighAction = new GameAction();
    //     veryHighAction.AddVariableChange("enemySpawnRate", -0.6f);
    //     veryHighAction.AddVariableChange("maxEnemies", 2);
    //     veryHighAction.AddVariableChange("scoreMultiplier", 2);
    //     engine.AddRule(new Rule("achiever_very_high", "very.high", 1, veryHighAction));

    //     // High performance
    //     var highAction = new GameAction();
    //     highAction.AddVariableChange("enemySpawnRate", -0.3f);
    //     highAction.AddVariableChange("maxEnemies", 1);
    //     highAction.AddVariableChange("scoreMultiplier", 1);
    //     engine.AddRule(new Rule("achiever_high", "high", 1, highAction));

    //     // Low performance - help with scoring
    //     var lowAction = new GameAction();
    //     lowAction.AddVariableChange("enemySpawnRate", 0.3f);
    //     lowAction.AddVariableChange("maxEnemies", -1);
    //     lowAction.AddVariableChange("scoreMultiplier", -1);
    //     lowAction.AddVariableChange("itemDropRate", 3);
    //     engine.AddRule(new Rule("achiever_low", "low", 1, lowAction));

    //     // Very low performance
    //     var veryLowAction = new GameAction();
    //     veryLowAction.AddVariableChange("enemySpawnRate", 0.8f);
    //     veryLowAction.AddVariableChange("maxEnemies", -2);
    //     veryLowAction.AddVariableChange("scoreMultiplier", -2);
    //     veryLowAction.AddVariableChange("itemDropRate", 5);
    //     veryLowAction.AddVariableChange("healthPackValue", 10);
    //     engine.AddRule(new Rule("achiever_very_low", "very.low", 1, veryLowAction));
    // }

    // private void ConfigureExplorerRules(PolicyEngine engine)
    // {
    //     // Explorer: Enjoys discovering and experimenting

    //     // Very high performance - add variety
    //     var veryHighAction = new GameAction();
    //     veryHighAction.AddVariableChange("enemySpawnRate", -0.4f);
    //     veryHighAction.AddVariableChange("maxEnemies", 2);
    //     veryHighAction.AddVariableChange("enemyHealth", 2);
    //     engine.AddRule(new Rule("explorer_very_high", "very.high", 2, veryHighAction));

    //     // High performance
    //     var highAction = new GameAction();
    //     highAction.AddVariableChange("enemySpawnRate", -0.2f);
    //     highAction.AddVariableChange("maxEnemies", 1);
    //     highAction.AddVariableChange("playerSpeedBonus", -1);
    //     engine.AddRule(new Rule("explorer_high", "high", 2, highAction));

    //     // Low performance - help exploration
    //     var lowAction = new GameAction();
    //     lowAction.AddVariableChange("enemySpawnRate", 0.5f);
    //     lowAction.AddVariableChange("maxEnemies", -1);
    //     lowAction.AddVariableChange("playerSpeedBonus", 2);
    //     lowAction.AddVariableChange("playerManaRegen", 2);
    //     engine.AddRule(new Rule("explorer_low", "low", 2, lowAction));

    //     // Very low performance
    //     var veryLowAction = new GameAction();
    //     veryLowAction.AddVariableChange("enemySpawnRate", 0.5f);
    //     veryLowAction.AddVariableChange("maxEnemies", -2);
    //     veryLowAction.AddVariableChange("playerSpeedBonus", 3);
    //     veryLowAction.AddVariableChange("playerDefenseBonus", 3);
    //     veryLowAction.AddVariableChange("itemDropRate", 4);
    //     engine.AddRule(new Rule("explorer_very_low", "very.low", 2, veryLowAction));
    // }

    private void ConfigureRules()
    {
        var policyEngine = ddaFramework.GetPolicyEngine();

        if (rules == null || rules.Length == 0)
        {
            Debug.LogWarning("[DDA Config] No rules configured in Inspector. " + "Using only code defaults (if any).");
            return;
        }

        foreach (var ruleConfig in rules)
        {
            if (ruleConfig == null) continue;

            var action = new GameAction();
            if (ruleConfig.variableChanges != null)
            {
                foreach (var vc in ruleConfig.variableChanges)
                {
                    if (!string.IsNullOrEmpty(vc.variableName))
                    {
                        action.AddVariableChange(vc.variableName, vc.delta);
                    }
                }
            }

            var rule = new Rule(
                ruleConfig.id,
                ruleConfig.symptomDescription,
                ruleConfig.profileId,
                action
            );

            policyEngine.AddRule(rule);
        }
    }

    private void RegisterSensors()
    {
        if (useCorePerformanceOnly)
        {
            RegisterCoreSensors();
            return;
        }

        RegisterFullSensorSet();
    }

    private void RegisterCoreSensors()
    {
        var survivabilitySensor = gameObject.AddComponent<SurvivabilitySensor>();
        survivabilitySensor.ConfigureScoring(
            survivabilityDamageBudgetMultiplier,
            survivabilityDamageWeight,
            survivabilityLowestHealthWeight,
            survivabilityEndHealthWeight
        );
        ddaFramework.RegisterSensor(survivabilitySensor);

        var clearTimeSensor = gameObject.AddComponent<ClearTimeSensor>();
        clearTimeSensor.ConfigureDynamicBudget(
            clearTimeFallbackSeconds,
            clearTimeBaseRoomOverheadSeconds,
            clearTimePerEnemySeconds,
            aoeFactor,
            clearTimePerWaveSetupSeconds,
            clearTimeEliteMultiplier,
            clearTimeBossExpectedSeconds,
            clearTimeEnemyHealthInfluence
        );
        ddaFramework.RegisterSensor(clearTimeSensor);
    }

    private void RegisterFullSensorSet()
    {
        // Add health sensor
        var healthSensor = gameObject.AddComponent<HealthSensor>();
        ddaFramework.RegisterSensor(healthSensor);

        // Add score sensor
        var scoreSensor = gameObject.AddComponent<ScoreSensor>();
        ddaFramework.RegisterSensor(scoreSensor);

        // Add accuracy sensor
        var accuracySensor = gameObject.AddComponent<AccuracySensor>();
        ddaFramework.RegisterSensor(accuracySensor);

        // Add survivability sensor
        var survivabilitySensor = gameObject.AddComponent<SurvivabilitySensor>();
        ddaFramework.RegisterSensor(survivabilitySensor);

        // Add clear time sensor
        var clearTimeSensor = gameObject.AddComponent<ClearTimeSensor>();
        clearTimeSensor.SetExpectedClearTime(expectedClearTimeSeconds);
        ddaFramework.RegisterSensor(clearTimeSensor);
    }

    private void RegisterEffectors()
    {
        // Add enemy system effector
        var enemyEffector = gameObject.AddComponent<EnemySystemEffector>();
        ddaFramework.RegisterEffector(enemyEffector);

        // Add reward system effector
        var rewardEffector = gameObject.AddComponent<RewardSystemEffector>();
        ddaFramework.RegisterEffector(rewardEffector);
    }
}

[System.Serializable]
public class VariableChangeConfig
{
    public string variableName;   // e.g. "enemySpawnRate"
    public float delta;           // e.g. -0.5f
}

[System.Serializable]
public class RuleConfig
{
    public string id;                 // "killer_high", "achiever_low", etc.
    public string symptomDescription; // "high", "low", "very.low"
    public int profileId;             // 0 = Killer, 1 = Achiever, 2 = Explorer (or your own mapping)
    public VariableChangeConfig[] variableChanges;
}
