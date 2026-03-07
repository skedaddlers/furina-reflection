using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DDAMAPEKitFramework
{
    /// <summary>
    /// Analyzer component of MAPE-K loop
    /// Calculates performance and identifies symptoms
    /// </summary>
    public class Analyzer : Observable, IObserver
    {
        private PlayerModel playerModel;
        private SymptomRepository symptomRepository;
        private SystemStateLog systemStateLog;
        
        private bool performanceOverTime = false;
        private bool flexibleSymptoms = false;
        private int movingAverageSample = 5;
        private Queue<float> performanceHistory = new Queue<float>();

        public Analyzer(PlayerModel playerModel, SymptomRepository repository, SystemStateLog log)
        {
            this.playerModel = playerModel;
            this.symptomRepository = repository;
            this.systemStateLog = log;
        }

        public void Update(object data)
        {
            if (data == null) return;

            // Calculate overall performance
            float performance = CalculatePerformance();
            
            // Update player profile score
            playerModel.UpdateProfileScore(performance);
            playerModel.CalculateProfileDistribution();

            // Store in history for moving average
            performanceHistory.Enqueue(performance);
            if (performanceHistory.Count > movingAverageSample)
            {
                performanceHistory.Dequeue();
            }

            // Check for symptoms
            Symptom identifiedSymptom = null;
            float adjustedPerformance = performance;

            // Apply flexible symptoms if enabled
            if (flexibleSymptoms && performanceHistory.Count >= movingAverageSample)
            {
                float movingAverage = performanceHistory.Average();
                adjustedPerformance = performance * movingAverage;
            }

            identifiedSymptom = symptomRepository.FindSymptom(adjustedPerformance);

            if (identifiedSymptom != null)
            {
                Debug.Log($"[Analyzer] Symptom identified: {identifiedSymptom.description} (Performance: {performance:F2})");
                
                // Create adaptation request
                var adaptationRequest = new AdaptationRequest
                {
                    symptom = identifiedSymptom,
                    performance = performance,
                    movingAverage = flexibleSymptoms ? performanceHistory.Average() : 1f,
                    profileId = playerModel.GetCurrentProfileId()
                };

                NotifyObservers(adaptationRequest);
            }
        }

        private float CalculatePerformance()
        {
            var attributes = playerModel.GetAllAttributes();
            if (attributes.Count == 0) return 1f;

            float totalWeight = 0f;
            float weightedSum = 0f;

            foreach (var attribute in attributes)
            {
                float reference = performanceOverTime ? 
                    attribute.reference.GetReference(Time.time) : 
                    attribute.reference.GetReference();

                if (reference > 0)
                {
                    float attributePerformance = attribute.value / reference;
                    weightedSum += attributePerformance * attribute.weight;
                    totalWeight += attribute.weight;
                }
            }

            return totalWeight > 0 ? weightedSum / totalWeight : 1f;
        }

        public void SetPerformanceOverTime(bool value)
        {
            performanceOverTime = value;
        }

        public void SetFlexibleSymptoms(bool value)
        {
            flexibleSymptoms = value;
        }

        public void SetMovingAverageSample(int sample)
        {
            movingAverageSample = sample;
        }
    }

    /// <summary>
    /// Adaptation request data structure
    /// </summary>
    public class AdaptationRequest
    {
        public Symptom symptom;
        public float performance;
        public float movingAverage;
        public int profileId;
    }

    /// <summary>
    /// Repository for managing symptoms
    /// </summary>
    public class SymptomRepository
    {
        private List<Symptom> symptoms = new List<Symptom>();

        public void AddSymptom(Symptom symptom)
        {
            symptoms.Add(symptom);
            // Sort by lower threshold for efficient searching
            symptoms = symptoms.OrderBy(s => s.threshold.min).ToList();
        }

        public Symptom FindSymptom(float performance)
        {
            foreach (var symptom in symptoms)
            {
                if (symptom.threshold.IsInRange(performance))
                {
                    return symptom;
                }
            }
            return null;
        }

        public List<Symptom> GetAllSymptoms()
        {
            return new List<Symptom>(symptoms);
        }
    }

    /// <summary>
    /// Symptom definition
    /// </summary>
    [System.Serializable]
    public class Symptom
    {
        public string description;
        public Threshold threshold;

        public Symptom(string description, float min, float max)
        {
            this.description = description;
            this.threshold = new Threshold(min, max);
        }
    }
}