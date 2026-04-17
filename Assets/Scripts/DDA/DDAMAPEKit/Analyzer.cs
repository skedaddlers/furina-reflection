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
        private List<AnalysisSnapshot> analysisHistory = new List<AnalysisSnapshot>();
        private AnalysisTriggerSource currentTriggerSource = AnalysisTriggerSource.Unknown;

        private Symptom currentSymptom;
        public Symptom CurrentSymptom => currentSymptom;

        public Analyzer(PlayerModel playerModel, SymptomRepository repository, SystemStateLog log)
        {
            this.playerModel = playerModel;
            this.symptomRepository = repository;
            this.systemStateLog = log;
        }

        public void Update(object data)
        {
            if (data == null) return;

            ObservationBatch observation = data as ObservationBatch;

            // Calculate overall performance
            float performance = CalculatePerformance(observation?.excludedAttributeIds);
            analysisHistory.Add(CaptureAnalysisSnapshot(performance));
            
            // Update player profile score
            playerModel.CalculateProfileDistribution();
            playerModel.UpdateProfileScore(performance);

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
                currentSymptom = identifiedSymptom;
                Debug.Log($"[Analyzer] Symptom identified: {identifiedSymptom.description} (Performance: {adjustedPerformance:F2})");
                
                // Create adaptation request
                var adaptationRequest = new AdaptationRequest
                {
                    symptom = identifiedSymptom,
                    performance = adjustedPerformance,
                    movingAverage = flexibleSymptoms ? performanceHistory.Average() : 1f,
                    profileId = playerModel.GetCurrentProfileId()
                };

                NotifyObservers(adaptationRequest);
            }
        }

        public List<AnalysisSnapshot> GetAnalysisHistory()
        {
            return new List<AnalysisSnapshot>(analysisHistory);
        }

        public void SetCurrentTriggerSource(AnalysisTriggerSource source)
        {
            currentTriggerSource = source;
        }

        public void ClearCurrentTriggerSource()
        {
            currentTriggerSource = AnalysisTriggerSource.Unknown;
        }

        public void ResetAnalysisHistory()
        {
            analysisHistory.Clear();
            performanceHistory.Clear();
            currentSymptom = null;
        }

        private float CalculatePerformance(ISet<int> excludedAttributeIds = null)
        {
            var attributes = playerModel.GetAllAttributes();
            if (attributes.Count == 0) return 1f;

            float totalWeight = 0f;
            float weightedSum = 0f;

            foreach (var attribute in attributes)
            {
                if (excludedAttributeIds != null && excludedAttributeIds.Contains(attribute.id))
                    continue;

                float reference = performanceOverTime ? 
                    attribute.reference.GetReference(Time.time) : 
                    attribute.reference.GetReference();

                if (reference > 0)
                {
                    float attributePerformance = attribute.value / reference;
                    weightedSum += attributePerformance * attribute.weight;
                    totalWeight += attribute.weight;
                    Debug.Log($"[Analyzer] Attribute: {attribute.label}, Value: {attribute.value:F2}, Reference: {reference:F2}, Weight: {attribute.weight:F2}, Performance: {(reference > 0 ? attributePerformance : 0f):F2}");
                }
            }
            Debug.Log($"[Analyzer] Total Weighted Performance: {weightedSum:F2}, Total Weight: {totalWeight:F2}");
            return totalWeight > 0 ? weightedSum / totalWeight : 1f;
        }

        private AnalysisSnapshot CaptureAnalysisSnapshot(float performance)
        {
            var snapshot = new AnalysisSnapshot
            {
                timestamp = Time.time,
                performance = performance,
                triggerSource = currentTriggerSource
            };

            foreach (var attribute in playerModel.GetAllAttributes())
            {
                snapshot.attributes.Add(new AnalysisAttributeSnapshot
                {
                    attributeId = attribute.id,
                    label = attribute.label,
                    value = attribute.value
                });
            }

            return snapshot;
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

    public class AnalysisSnapshot
    {
        public float timestamp;
        public float performance;
        public AnalysisTriggerSource triggerSource;
        public List<AnalysisAttributeSnapshot> attributes = new List<AnalysisAttributeSnapshot>();
    }

    public enum AnalysisTriggerSource
    {
        Unknown,
        Automatic,
        RoomClear,
        BossTick
    }

    public class AnalysisAttributeSnapshot
    {
        public int attributeId;
        public string label;
        public float value;
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
