using System.Collections.Generic;
using UnityEngine;

namespace DDAMAPEKitFramework
{
    /// <summary>
    /// Executor component of MAPE-K loop
    /// Applies changes through effectors
    /// </summary>
    public class Executor : IObserver
    {
        private List<Effector> effectors = new List<Effector>();
        private Dictionary<string, Queue<float>> variableHistory = new Dictionary<string, Queue<float>>();
        private const int HISTORY_SIZE = 10;

        public void Update(object data)
        {
            if (data == null) return;

            var changePlan = data as ChangePlan;
            if (changePlan == null) return;

            var actions = changePlan.GetActions();
            Debug.Log($"[Executor] Executing {actions.Count} actions");

            // Aggregate all variable changes
            var aggregatedChanges = new Dictionary<string, float>();

            foreach (var action in actions)
            {
                foreach (var kvp in action.variableChanges)
                {
                    if (aggregatedChanges.ContainsKey(kvp.Key))
                    {
                        // If multiple actions affect the same variable, average them
                        aggregatedChanges[kvp.Key] = (aggregatedChanges[kvp.Key] + kvp.Value) / 2f;
                    }
                    else
                    {
                        aggregatedChanges[kvp.Key] = kvp.Value;
                    }
                    Debug.Log($"[Executor] Planned change: {kvp.Key} -> {kvp.Value}");
                }
            }

            // Apply adjustments based on history
            foreach (var kvp in aggregatedChanges)
            {
                float adjustedValue = AdjustValueBasedOnHistory(kvp.Key, kvp.Value);
                
                // Apply changes through effectors
                foreach (var effector in effectors)
                {
                    effector.Apply(kvp.Key, adjustedValue);
                }

                // Update history
                UpdateHistory(kvp.Key, adjustedValue);
            }
        }

        private float AdjustValueBasedOnHistory(string variable, float value)
        {
            if (!variableHistory.ContainsKey(variable))
            {
                variableHistory[variable] = new Queue<float>();
            }

            var history = variableHistory[variable];
            if (history.Count > 0)
            {
                // Apply smoothing based on history
                float avgHistory = 0f;
                foreach (var h in history)
                {
                    avgHistory += h;
                }
                avgHistory /= history.Count;

                // Blend new value with history average
                return Mathf.Lerp(avgHistory, value, 0.7f);
            }

            return value;
        }

        private void UpdateHistory(string variable, float value)
        {
            if (!variableHistory.ContainsKey(variable))
            {
                variableHistory[variable] = new Queue<float>();
            }

            var history = variableHistory[variable];
            history.Enqueue(value);

            if (history.Count > HISTORY_SIZE)
            {
                history.Dequeue();
            }
        }

        public void RegisterEffector(Effector effector)
        {
            if (!effectors.Contains(effector))
            {
                effectors.Add(effector);
            }
        }
    }

    /// <summary>
    /// Base class for effectors that apply changes to the game
    /// </summary>
    public abstract class Effector : MonoBehaviour
    {
        public abstract void Apply(string variable, float value);
    }
}