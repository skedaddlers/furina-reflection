using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DDAMAPEKitFramework
{
    /// <summary>
    /// Player Model containing attributes and profile information
    /// </summary>
    public class PlayerModel
    {
        private List<PlayerAttribute> attributes = new List<PlayerAttribute>();
        private List<PlayerProfile> profiles = new List<PlayerProfile>();
        private PlayerProfile currentProfile;
        private Dictionary<PlayerProfile, float> profileScores = new Dictionary<PlayerProfile, float>();
        private Dictionary<PlayerMetricType, PlayerMetric> metrics = new Dictionary<PlayerMetricType, PlayerMetric>();
        private Dictionary<PlayerMetricType, float> profilingMetrics = new Dictionary<PlayerMetricType, float>();
        // profile distribution mapping (profile -> normalized percentage)
        private Dictionary<PlayerProfile, float> profileDistribution = new Dictionary<PlayerProfile, float>();

        public float smoothingAlpha = 0.35f;

        // tie threshold to decide hybrid vs dominant (e.g., 0.10 = if top two within 10% => hybrid)
        public float tieThreshold = 0.1f;

        // last time we computed profile distribution (used to get window duration for DPS)
        private float lastProfileCalcTime = 0f;
        private float lastProfileUpdateTime;

        public PlayerModel()
        {
            attributes = new List<PlayerAttribute>();
            profiles = new List<PlayerProfile>();
            profileScores = new Dictionary<PlayerProfile, float>();
            metrics = new Dictionary<PlayerMetricType, PlayerMetric>();
            profileDistribution = new Dictionary<PlayerProfile, float>();
            currentProfile = null;
            lastProfileUpdateTime = 0f;
            lastProfileCalcTime = Time.time;
        }

        public void InitProfiles(List<PlayerProfile> initialProfiles)
        {
            foreach (var profile in initialProfiles)
            {
                AddProfile(profile);
            }
            currentProfile = profiles.FirstOrDefault();
        }

        public void AddAttribute(PlayerAttribute attribute)
        {
            if (!attributes.Any(a => a.id == attribute.id))
            {
                attributes.Add(attribute);
            }
        }

        public void UpdateAttribute(int id, float value)
        {
            var attribute = attributes.FirstOrDefault(a => a.id == id);
            if (attribute != null)
            {
                attribute.value = value;
            }
        }

        public PlayerAttribute GetAttribute(int id)
        {
            return attributes.FirstOrDefault(a => a.id == id);
        }

        public List<PlayerAttribute> GetAllAttributes()
        {
            return new List<PlayerAttribute>(attributes);
        }

        public List<PlayerProfile> GetProfiles()
        {
            return new List<PlayerProfile>(profiles);
        }

        public void AddProfile(PlayerProfile profile)
        {
            if (!profiles.Any(p => p.id == profile.id))
            {
                profiles.Add(profile);
                profileScores[profile] = 0f;
            }
        }

        public void SetProfilingMetric(PlayerMetricType metric, float value)
        {
            profilingMetrics[metric] = value;
        }

        public float GetProfilingMetric(PlayerMetricType metric)
        {
            if (profilingMetrics.ContainsKey(metric))
                return profilingMetrics[metric];

            return 0f;
        }

        public void RegisterMetricRange(PlayerMetricType metric, float minExpected, float maxExpected)
        {
            if (!metrics.ContainsKey(metric))
            {
                metrics[metric] = new PlayerMetric(metric, minExpected, maxExpected);
            }
            else
            {
                metrics[metric].minExpected = minExpected;
                metrics[metric].maxExpected = maxExpected;
            }
        }

        public void IncrementMetric(PlayerMetricType metric, float amount = 1f)
        {
            if (!metrics.ContainsKey(metric))
            {
                // default range if not registered
                RegisterMetricRange(metric, 0f, 10f);
            }
            metrics[metric].Accumulate(amount);
        }

        public float GetMetricEMA(PlayerMetricType metric)
        {
            if (metrics.ContainsKey(metric)) return metrics[metric].emaValue;
            return 0f;
        }

        public void CalculateProfileDistribution()
        {
            Dictionary<PlayerProfile, float> scores = new();

            float totalScore = 0;

            foreach (var profile in profiles)
            {
                float score = 0;

                foreach (var weight in profile.weights)
                {
                    float metricValue = GetProfilingMetric(weight.metric);
                    score += metricValue * weight.weight;
                    // Debug.Log($"[PlayerModel] Profile: {profile.name}, Metric: {weight.metric}, Value: {metricValue:F2}, Weight: {weight.weight:F2}, Partial Score: {metricValue * weight.weight:F2}");
                }

                score = Mathf.Max(0, score);

                scores[profile] = score;
                totalScore += score;
            }

            profileDistribution.Clear();

            if (totalScore == 0) return;

            foreach (var kvp in scores)
            {
                profileDistribution[kvp.Key] = kvp.Value / totalScore;
                Debug.Log($"[PlayerModel] Profile: {kvp.Key.name}, Raw Score: {kvp.Value:F2}, Distribution: {profileDistribution[kvp.Key]:P2}");
            }
        }

        public PlayerProfile GetDominantProfile()
        {
            if (profileDistribution == null || profileDistribution.Count == 0)
            {
                return profiles.FirstOrDefault();
            }

            return profileDistribution.OrderByDescending(k => k.Value).First().Key;
        }

        public Dictionary<PlayerProfile, float> GetProfileDistribution()
        {
            return new Dictionary<PlayerProfile, float>(profileDistribution);
        }

        public Dictionary<PlayerProfile, float> GetProfileScores()
        {
            return new Dictionary<PlayerProfile, float>(profileScores);
        }

        public List<KeyValuePair<PlayerProfile, float>> GetSortedProfiles()
        {
            return profileDistribution.OrderByDescending(k => k.Value).ToList();
        }

        public bool TryGetHybridProfiles(out List<KeyValuePair<PlayerProfile, float>> hybridOut)
        {
            hybridOut = new List<KeyValuePair<PlayerProfile, float>>();
            var sorted = GetSortedProfiles();
            if (sorted.Count < 2) return false;

            var top = sorted[0];
            var second = sorted[1];

            if (top.Value - second.Value <= tieThreshold)
            {
                // include any profiles within the top.Value - tieThreshold window
                float cutoff = top.Value - tieThreshold;
                hybridOut = sorted.Where(x => x.Value >= cutoff).ToList();
                return true;
            }
            return false;
        }

        
        public void UpdatePlayerProfile(float explorationRate)
        {
            if (profiles.Count == 0)
            {
                currentProfile = null;
                return;
            }

            // Multi-Armed Bandit approach for profile selection
            if (UnityEngine.Random.value < explorationRate)
            {
                // Exploration: choose random profile
                currentProfile = profiles[UnityEngine.Random.Range(0, profiles.Count)];
            }
            else
            {
                // Exploitation: choose best profile
                currentProfile = profileScores.OrderByDescending(kvp => kvp.Value).First().Key;
            }

            Debug.Log($"[PlayerModel] Current Profile: {currentProfile.name}, Score: {profileScores[currentProfile]}");
        }


        public void UpdateProfileScore(float performance)
        {
            if (currentProfile == null) return;

            // Calculate reward based on performance (as per paper formula)
            float reward;
            if (performance >= 0.8f && performance <= 1.2f)
            {
                reward = 5f; // Maximum reward
            }
            else
            {
                float maxR = 5f;
                float gR = 10f;
                reward = maxR * Mathf.Pow(gR, -1f * Mathf.Abs(performance - 1f));
            }
            Debug.Log($"[PlayerModel] Calculated Reward: {reward:F2}");

            UpdateProfileScoresPerDistribution(reward);
        }

        private void UpdateProfileScoresPerDistribution(float reward)
        {
            foreach (var kvp in profileDistribution)
            {
                var profile = kvp.Key;
                var distribution = kvp.Value;

                // Update score with EMA
                float currentScore = profileScores.ContainsKey(profile) ? profileScores[profile] : 0f;
                float newScore = currentScore + smoothingAlpha * (reward * distribution - currentScore);
                profileScores[profile] = newScore;

                Debug.Log($"[PlayerModel] Updated Score for {profile.name}: {newScore:F2} (Reward: {reward:F2}, Distribution: {distribution:F2})");
            }
        }

        public PlayerProfile GetCurrentProfile()
        {
            return currentProfile;
        }

        public int GetCurrentProfileId()
        {
            return currentProfile?.id ?? 0;
        }
    }

    /// <summary>
    /// Represents a player attribute that will be monitored
    /// </summary>
    [System.Serializable]
    public class PlayerAttribute
    {
        public int id;
        public string label;
        public float value;
        public Threshold threshold;
        public Reference reference;
        public float weight = 1.0f;

        public PlayerAttribute(int id, string label, float minThreshold, float maxThreshold)
        {
            this.id = id;
            this.label = label;
            this.value = 0f;
            this.threshold = new Threshold(minThreshold, maxThreshold);
            this.reference = new Reference();
        }
    }

    /// <summary>
    /// Threshold for player attributes
    /// </summary>
    [System.Serializable]
    public class Threshold
    {
        public float min;
        public float max;

        public Threshold(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public bool IsInRange(float value)
        {
            return value >= min && value <= max;
        }
    }

    /// <summary>
    /// Reference values for performance calculation
    /// </summary>
    [System.Serializable]
    public class Reference
    {
        public float staticReference = 1.0f;
        public Dictionary<float, float> timeBasedReferences = new Dictionary<float, float>();

        public float GetReference(float time = -1)
        {
            if (time < 0 || timeBasedReferences.Count == 0)
            {
                return staticReference;
            }

            // Find closest time-based reference
            var closest = timeBasedReferences.OrderBy(kvp => Mathf.Abs(kvp.Key - time)).First();
            return closest.Value;
        }

        public void SetStaticReference(float value)
        {
            staticReference = value;
        }

        public void AddTimeBasedReference(float time, float value)
        {
            timeBasedReferences[time] = value;
        }
    }

    public enum PlayerMetricType
    {
        MeleeUsage,     // count of melee hits
        RangedUsage,    // count of ranged hits
        SkillUsage,     // count of skill uses
        DodgeRate,      // count of dodges (will be normalized against window)
        DamageTaken,    // damage taken in window
        HealingUsed,    // healing used in window
        AverageDistance,// average distance to target (0..maxDistance)
        ManaUsed,       // mana consumed in window
        DamageDealt,    // total damage dealt in window (used to compute DPS-like metric)
        DefensiveUpgradePreference,
        OffensiveUpgradePreference,
        ManaUpgradePreference,
        SpeedUpgradePreference,
        SkillCastRate,          // count/rate of successful skill activations in window
        DamageAbsorbedByShield  // damage prevented by the player's shield in window
    }

    [System.Serializable]
    public class ProfileAttributeWeight
    {
        public PlayerMetricType metric;
        public float weight;
    }

    /// <summary>
    /// Player profile (e.g., Killer, Achiever, Explorer)
    /// </summary>
    [System.Serializable]
    public class PlayerProfile
    {
        public int id;
        public string name;
        public List<ProfileAttributeWeight> weights = new List<ProfileAttributeWeight>();

        public PlayerProfile(int id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}
