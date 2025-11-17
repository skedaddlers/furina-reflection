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
    
    [Header("Player Attribute Thresholds")]
    [SerializeField] private Vector2 healthThreshold = new Vector2(0.3f, 1.0f);
    [SerializeField] private Vector2 scoreThreshold = new Vector2(50f, 500f);
    [SerializeField] private Vector2 accuracyThreshold = new Vector2(0.2f, 0.8f);

    [Header("Reference Values")]
    [SerializeField] private float healthReference = 0.7f;
    [SerializeField] private float scoreReference = 200f;
    [SerializeField] private float accuracyReference = 0.5f;

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
        // Health attribute
        var healthAttr = new PlayerAttribute(0, "Health", healthThreshold.x, healthThreshold.y);
        healthAttr.reference.SetStaticReference(healthReference);
        healthAttr.weight = 2.0f; // Health is more important
        ddaFramework.AddPlayerAttribute(healthAttr);

        // Score attribute
        var scoreAttr = new PlayerAttribute(1, "Score", scoreThreshold.x, scoreThreshold.y);
        scoreAttr.reference.SetStaticReference(scoreReference);
        scoreAttr.weight = 1.0f;
        ddaFramework.AddPlayerAttribute(scoreAttr);

        // Accuracy attribute
        var accuracyAttr = new PlayerAttribute(2, "Accuracy", accuracyThreshold.x, accuracyThreshold.y);
        accuracyAttr.reference.SetStaticReference(accuracyReference);
        accuracyAttr.weight = 0.5f; // Less important than health and score
        ddaFramework.AddPlayerAttribute(accuracyAttr);
    }

    private void ConfigureSymptoms()
    {
        // Already configured in DDAMAPEKit default initialization
        // But we can add more specific ones if needed
    }

    private void ConfigureRules()
    {
        var policyEngine = ddaFramework.GetPolicyEngine();

        // Rules for Killer profile (Profile 0)
        ConfigureKillerRules(policyEngine);

        // Rules for Achiever profile (Profile 1)
        ConfigureAchieverRules(policyEngine);

        // Rules for Explorer profile (Profile 2)
        ConfigureExplorerRules(policyEngine);
    }

    private void ConfigureKillerRules(PolicyEngine engine)
    {
        // Killer: Enjoys combat and challenge

        // Very high performance - increase difficulty significantly
        var veryHighAction = new GameAction();
        veryHighAction.AddVariableChange("enemySpawnRate", -0.8f); // Faster spawns
        veryHighAction.AddVariableChange("maxEnemies", 3);
        veryHighAction.AddVariableChange("enemyDamage", 5);
        veryHighAction.AddVariableChange("enemySpeed", 0.8f);
        engine.AddRule(new Rule("killer_very_high", "very.high", 0, veryHighAction));

        // High performance - increase difficulty
        var highAction = new GameAction();
        highAction.AddVariableChange("enemySpawnRate", -0.5f);
        highAction.AddVariableChange("maxEnemies", 2);
        highAction.AddVariableChange("enemyDamage", 3);
        engine.AddRule(new Rule("killer_high", "high", 0, highAction));

        // Low performance - decrease difficulty
        var lowAction = new GameAction();
        lowAction.AddVariableChange("enemySpawnRate", 0.5f);
        lowAction.AddVariableChange("maxEnemies", -2);
        lowAction.AddVariableChange("enemyDamage", -3);
        lowAction.AddVariableChange("itemDropRate", 2);
        engine.AddRule(new Rule("killer_low", "low", 0, lowAction));

        // Very low performance - help the player significantly
        var veryLowAction = new GameAction();
        veryLowAction.AddVariableChange("enemySpawnRate", 1.0f);
        veryLowAction.AddVariableChange("maxEnemies", -3);
        veryLowAction.AddVariableChange("enemyDamage", -5);
        veryLowAction.AddVariableChange("playerDamageBonus", 2);
        veryLowAction.AddVariableChange("itemDropRate", 5);
        engine.AddRule(new Rule("killer_very_low", "very.low", 0, veryLowAction));
    }

    private void ConfigureAchieverRules(PolicyEngine engine)
    {
        // Achiever: Focuses on score and progression

        // Very high performance - increase challenge and rewards
        var veryHighAction = new GameAction();
        veryHighAction.AddVariableChange("enemySpawnRate", -0.6f);
        veryHighAction.AddVariableChange("maxEnemies", 2);
        veryHighAction.AddVariableChange("scoreMultiplier", 2);
        engine.AddRule(new Rule("achiever_very_high", "very.high", 1, veryHighAction));

        // High performance
        var highAction = new GameAction();
        highAction.AddVariableChange("enemySpawnRate", -0.3f);
        highAction.AddVariableChange("maxEnemies", 1);
        highAction.AddVariableChange("scoreMultiplier", 1);
        engine.AddRule(new Rule("achiever_high", "high", 1, highAction));

        // Low performance - help with scoring
        var lowAction = new GameAction();
        lowAction.AddVariableChange("enemySpawnRate", 0.3f);
        lowAction.AddVariableChange("maxEnemies", -1);
        lowAction.AddVariableChange("scoreMultiplier", -1);
        lowAction.AddVariableChange("itemDropRate", 3);
        engine.AddRule(new Rule("achiever_low", "low", 1, lowAction));

        // Very low performance
        var veryLowAction = new GameAction();
        veryLowAction.AddVariableChange("enemySpawnRate", 0.8f);
        veryLowAction.AddVariableChange("maxEnemies", -2);
        veryLowAction.AddVariableChange("scoreMultiplier", -2);
        veryLowAction.AddVariableChange("itemDropRate", 5);
        veryLowAction.AddVariableChange("healthPackValue", 10);
        engine.AddRule(new Rule("achiever_very_low", "very.low", 1, veryLowAction));
    }

    private void ConfigureExplorerRules(PolicyEngine engine)
    {
        // Explorer: Enjoys discovering and experimenting

        // Very high performance - add variety
        var veryHighAction = new GameAction();
        veryHighAction.AddVariableChange("enemySpawnRate", -0.4f);
        veryHighAction.AddVariableChange("maxEnemies", 2);
        veryHighAction.AddVariableChange("enemyHealth", 2);
        engine.AddRule(new Rule("explorer_very_high", "very.high", 2, veryHighAction));

        // High performance
        var highAction = new GameAction();
        highAction.AddVariableChange("enemySpawnRate", -0.2f);
        highAction.AddVariableChange("maxEnemies", 1);
        highAction.AddVariableChange("playerSpeedBonus", -1);
        engine.AddRule(new Rule("explorer_high", "high", 2, highAction));

        // Low performance - help exploration
        var lowAction = new GameAction();
        lowAction.AddVariableChange("enemySpawnRate", 0.5f);
        lowAction.AddVariableChange("maxEnemies", -1);
        lowAction.AddVariableChange("playerSpeedBonus", 2);
        lowAction.AddVariableChange("playerManaRegen", 2);
        engine.AddRule(new Rule("explorer_low", "low", 2, lowAction));

        // Very low performance
        var veryLowAction = new GameAction();
        veryLowAction.AddVariableChange("enemySpawnRate", 0.5f);
        veryLowAction.AddVariableChange("maxEnemies", -2);
        veryLowAction.AddVariableChange("playerSpeedBonus", 3);
        veryLowAction.AddVariableChange("playerDefenseBonus", 3);
        veryLowAction.AddVariableChange("itemDropRate", 4);
        engine.AddRule(new Rule("explorer_very_low", "very.low", 2, veryLowAction));
    }

    private void RegisterSensors()
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
    }

    private void RegisterEffectors()
    {
        // Add enemy system effector
        var enemyEffector = gameObject.AddComponent<EnemySystemEffector>();
        ddaFramework.RegisterEffector(enemyEffector);

        // Add reward system effector
        var rewardEffector = gameObject.AddComponent<RewardSystemEffector>();
        ddaFramework.RegisterEffector(rewardEffector);

        // Add player system effector
        var playerEffector = gameObject.AddComponent<PlayerSystemEffector>();
        ddaFramework.RegisterEffector(playerEffector);
    }
}