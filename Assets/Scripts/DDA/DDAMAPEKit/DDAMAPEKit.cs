using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace DDAMAPEKitFramework
{
    /// <summary>
    /// Main facade for the DDA-MAPEKit framework
    /// Manages all MAPE-K components and orchestrates the adaptation loop
    /// </summary>
    public class DDAMAPEKit : MonoBehaviour
    {
        [Header("MAPE-K Configuration")]
        [SerializeField] private int readFrequency = 60; // frames between reads
        [SerializeField] private bool isPerformanceOverTime = false;
        [SerializeField] private bool flexibleSymptoms = false;
        [SerializeField] private bool flexibleRules = false;
        [SerializeField] private float adjustmentConstant = 0.5f;
        [SerializeField] private int movingAverageSample = 5;
        [SerializeField] private bool runAutomatically = true;

        [Header("Player Profile Configuration")]
        [SerializeField] private List<PlayerProfile> profiles = new List<PlayerProfile>();
        [SerializeField] private float profileUpdateFrequency = 10f;
        [SerializeField] private float explorationRate = 0.2f;

        [Header("Symptoms (Designer editable)")]
        [SerializeField] private bool useDefaultSymptoms = true;
        [SerializeField] private SymptomConfig[] customSymptoms;
        
        private PlayerModel playerModel;
        private Monitor monitor;
        private Analyzer analyzer;
        private Planner planner;
        private Executor executor;
        private SymptomRepository symptomRepository;
        private PolicyEngine policyEngine;
        private SystemStateLog systemStateLog;
        
        private List<Sensor> sensors = new List<Sensor>();
        private List<Effector> effectors = new List<Effector>();
        
        private int frameCounter = 0;
        private bool isInitialized = false;

        private static DDAMAPEKit instance;
        public static DDAMAPEKit Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<DDAMAPEKit>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("DDA-MAPEKit");
                        instance = go.AddComponent<DDAMAPEKit>();
                    }
                }
                return instance;
            }
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
            isInitialized = false;
            sensors.Clear();
            effectors.Clear();
        }

        public static void DestroyInstanceForRestart()
        {
            if (instance == null) return;
            var go = instance.gameObject;
            instance = null;
            Object.Destroy(go);
        }

        void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (isInitialized) return;

            // Initialize components
            playerModel = new PlayerModel();
            playerModel.InitProfiles(profiles);
            // ApplyDefaultProfileMetricWeights();
            symptomRepository = new SymptomRepository();
            policyEngine = new PolicyEngine();
            systemStateLog = new SystemStateLog();

            // Initialize MAPE-K components
            monitor = new Monitor(playerModel, systemStateLog);
            analyzer = new Analyzer(playerModel, symptomRepository, systemStateLog);
            analyzer.SetPerformanceOverTime(isPerformanceOverTime);
            analyzer.SetFlexibleSymptoms(flexibleSymptoms);
            analyzer.SetMovingAverageSample(movingAverageSample);

            planner = new Planner(policyEngine, playerModel);
            planner.SetFlexibleRules(flexibleRules);
            planner.SetAdjustmentConstant(adjustmentConstant);

            executor = new Executor();

            // Setup Observer pattern connections
            monitor.Subscribe(analyzer);
            analyzer.Subscribe(planner);
            planner.Subscribe(executor);

            // Configure default symptoms and rules
            ConfigureDefaultSymptomsAndRules();

            isInitialized = true;
            Debug.Log("[DDA-MAPEKit] Framework initialized successfully");
        }

        void Update()
        {
            if (!isInitialized || !runAutomatically) return;

            frameCounter++;
            if (frameCounter >= readFrequency)
            {
                frameCounter = 0;
                RunMAPEKLoop();
            }

            // Update player profile periodically
            if (Time.time % profileUpdateFrequency < Time.deltaTime)
            {
                playerModel.UpdatePlayerProfile(explorationRate);
            }
        }

        public void TriggerMAPEKLoop()
        {
            if (!isInitialized) return;
            RunMAPEKLoop();
            playerModel.UpdatePlayerProfile(explorationRate);
        }

        private void RunMAPEKLoop()
        {
            // Monitor phase
            monitor.Observe(sensors);

            // The rest of the loop is handled through the Observer pattern
            // Analyzer -> Planner -> Executor are triggered automatically
        }

        public void RegisterSensor(Sensor sensor)
        {
            if (sensor != null && !sensors.Contains(sensor))
            {
                sensors.Add(sensor);
                Debug.Log($"[DDA-MAPEKit] Registered sensor: {sensor.GetType().Name}");
            }
        }

        public void RegisterEffector(Effector effector)
        {
            if (effector != null && !effectors.Contains(effector))
            {
                effectors.Add(effector);
                executor.RegisterEffector(effector);
                Debug.Log($"[DDA-MAPEKit] Registered effector: {effector.GetType().Name}");
            }
        }

        public void AddPlayerAttribute(PlayerAttribute attribute)
        {
            playerModel.AddAttribute(attribute);
        }

        public void AddSymptom(Symptom symptom)
        {
            symptomRepository.AddSymptom(symptom);
        }

        public void AddRule(Rule rule)
        {
            policyEngine.AddRule(rule);
        }

        public void AddPlayerProfile(PlayerProfile profile)
        {
            profiles.Add(profile);
            playerModel.AddProfile(profile);
        }

        private void ConfigureDefaultSymptomsAndRules()
        {
            // Configure default symptoms based on the paper
            if(useDefaultSymptoms)
            {
                symptomRepository.AddSymptom(new Symptom("very.high", 1.8f, 3.0f));
                symptomRepository.AddSymptom(new Symptom("high", 1.5f, 1.8f));
                symptomRepository.AddSymptom(new Symptom("slightly.high", 1.1f, 1.5f));
                symptomRepository.AddSymptom(new Symptom("normal", 0.9f, 1.1f));
                symptomRepository.AddSymptom(new Symptom("slightly.low", 0.5f, 0.9f));
                symptomRepository.AddSymptom(new Symptom("low", 0.2f, 0.5f));
                symptomRepository.AddSymptom(new Symptom("very.low", 0.0f, 0.2f));
            }

            if (customSymptoms != null)
            {
                foreach (var cfg in customSymptoms)
                {
                    if (string.IsNullOrEmpty(cfg.description)) continue;
                    symptomRepository.AddSymptom(new Symptom(cfg.description, cfg.min, cfg.max));
                }
            }
        }

        public Symptom GetCurrentSymptom() => analyzer.CurrentSymptom;
        public PlayerModel GetPlayerModel() => playerModel;
        public SymptomRepository GetSymptomRepository() => symptomRepository;
        public PolicyEngine GetPolicyEngine() => policyEngine;

        // private void ApplyDefaultProfileMetricWeights()
        // {
        //     foreach (var profile in profiles)
        //     {
        //         if (profile == null || string.IsNullOrWhiteSpace(profile.name))
        //             continue;

        //         string profileName = profile.name.Trim().ToLowerInvariant();
        //         switch (profileName)
        //         {
        //             case "melee lover":
        //                 EnsureProfileMetricWeight(profile, PlayerMetricType.OffensiveUpgradePreference, 0.25f);
        //                 break;
        //             case "ranged lover":
        //                 EnsureProfileMetricWeight(profile, PlayerMetricType.AverageDistance, 0.25f);
        //                 EnsureProfileMetricWeight(profile, PlayerMetricType.OffensiveUpgradePreference, 0.2f);
        //                 break;
        //             case "skill spam":
        //                 EnsureProfileMetricWeight(profile, PlayerMetricType.ManaUpgradePreference, 0.35f);
        //                 break;
        //             case "dodger":
        //                 EnsureProfileMetricWeight(profile, PlayerMetricType.SpeedUpgradePreference, 0.35f);
        //                 break;
        //             case "defensive":
        //                 EnsureProfileMetricWeight(profile, PlayerMetricType.DefensiveUpgradePreference, 0.4f);
        //                 break;
        //         }
        //     }
        // }

        // private void EnsureProfileMetricWeight(PlayerProfile profile, PlayerMetricType metric, float weight)
        // {
        //     if (profile.weights == null)
        //     {
        //         profile.weights = new List<ProfileAttributeWeight>();
        //     }

        //     if (profile.weights.Any(existingWeight => existingWeight.metric == metric))
        //         return;

        //     profile.weights.Add(new ProfileAttributeWeight
        //     {
        //         metric = metric,
        //         weight = weight
        //     });
        // }
    }
}

[System.Serializable]
public struct SymptomConfig
{
    public string description;
    public float min;
    public float max;
}
