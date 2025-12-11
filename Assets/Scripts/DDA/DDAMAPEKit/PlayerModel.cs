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
        private float lastProfileUpdateTime;

        public PlayerModel()
        {
            attributes = new List<PlayerAttribute>();
            profiles = new List<PlayerProfile>();
            profileScores = new Dictionary<PlayerProfile, float>();
            currentProfile = null;
            lastProfileUpdateTime = 0f;
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

        public void AddProfile(PlayerProfile profile)
        {
            if (!profiles.Any(p => p.id == profile.id))
            {
                profiles.Add(profile);
                profileScores[profile] = 0f;
            }
        }

        public void UpdatePlayerProfile(float explorationRate)
        {
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
                reward = Mathf.Floor(maxR * Mathf.Pow(gR, -1f * Mathf.Abs(performance - 1f)));
            }

            profileScores[currentProfile] += reward;
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

    /// <summary>
    /// Player profile (e.g., Killer, Achiever, Explorer)
    /// </summary>
    [System.Serializable]
    public class PlayerProfile
    {
        public int id;
        public string name;

        public PlayerProfile(int id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}