using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DDAMAPEKitFramework
{
    /// <summary>
    /// Planner component of MAPE-K loop
    /// Selects adaptation rules and creates change plans
    /// </summary>
    public class Planner : Observable, IObserver
    {
        private PolicyEngine policyEngine;
        private PlayerModel playerModel;
        private bool flexibleRules = false;
        private float adjustmentConstant = 0.5f;

        public Planner(PolicyEngine engine, PlayerModel model)
        {
            this.policyEngine = engine;
            this.playerModel = model;
        }

        public void Update(object data)
        {
            if (data == null) return;

            var adaptationRequest = data as AdaptationRequest;
            if (adaptationRequest == null) return;

            // Find applicable rules for the symptom and profile
            var applicableRules = policyEngine.FindRules(
                adaptationRequest.symptom.description,
                adaptationRequest.profileId
            );

            if (applicableRules.Count > 0)
            {
                var changePlan = new ChangePlan();

                foreach (var rule in applicableRules)
                {
                    // Clone the action to avoid modifying the original
                    var adjustedAction = new GameAction(rule.action);

                    // Apply flexible rules if enabled
                    if (flexibleRules && Mathf.Abs(adaptationRequest.movingAverage - 1f) > 0.01f)
                    {
                        float adjustmentCoefficient = (adaptationRequest.movingAverage - 1f) * adjustmentConstant;
                        adjustedAction.AdjustValues(adjustmentCoefficient);
                    }

                    changePlan.AddAction(adjustedAction);
                }

                Debug.Log($"[Planner] Created change plan with {changePlan.GetActionCount()} actions");
                NotifyObservers(changePlan);
            }
            else
            {
                Debug.Log($"[Planner] No rules found for symptom: {adaptationRequest.symptom.description}");
            }
        }

        public void SetFlexibleRules(bool value)
        {
            flexibleRules = value;
        }

        public void SetAdjustmentConstant(float value)
        {
            adjustmentConstant = value;
        }
    }

    /// <summary>
    /// Policy engine containing adaptation rules
    /// </summary>
    public class PolicyEngine
    {
        private List<Rule> rules = new List<Rule>();

        public void AddRule(Rule rule)
        {
            rules.Add(rule);
        }

        public List<Rule> FindRules(string symptomDescription, int profileId)
        {
            return rules.Where(r => 
                r.symptomDescription == symptomDescription && 
                r.profileId == profileId
            ).ToList();
        }

        public List<Rule> GetAllRules()
        {
            return new List<Rule>(rules);
        }
    }

    /// <summary>
    /// Rule definition for adaptation
    /// </summary>
    [System.Serializable]
    public class Rule
    {
        public string id;
        public string symptomDescription;
        public int profileId;
        public GameAction action;

        public Rule(string id, string symptom, int profile, GameAction action)
        {
            this.id = id;
            this.symptomDescription = symptom;
            this.profileId = profile;
            this.action = action;
        }
    }

    /// <summary>
    /// Game action containing variable modifications
    /// </summary>
    [System.Serializable]
    public class GameAction
    {
        public Dictionary<string, float> variableChanges = new Dictionary<string, float>();

        public GameAction()
        {
            variableChanges = new Dictionary<string, float>();
        }

        public GameAction(GameAction other)
        {
            variableChanges = new Dictionary<string, float>(other.variableChanges);
        }

        public void AddVariableChange(string variable, float value)
        {
            variableChanges[variable] = value;
        }

        public void AdjustValues(float coefficient)
        {
            var keys = variableChanges.Keys.ToList();
            foreach (var key in keys)
            {
                variableChanges[key] -= coefficient;
            }
        }
    }

    /// <summary>
    /// Change plan containing actions to execute
    /// </summary>
    public class ChangePlan
    {
        private List<GameAction> actions = new List<GameAction>();

        public void AddAction(GameAction action)
        {
            actions.Add(action);
        }

        public List<GameAction> GetActions()
        {
            return new List<GameAction>(actions);
        }

        public int GetActionCount()
        {
            return actions.Count;
        }
    }
}