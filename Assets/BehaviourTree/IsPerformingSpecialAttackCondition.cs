using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsPerformingSpecialAttack", story: "[Agent] is currently performing special attack", category: "Conditions", id: "b93333e0f1a854528c4129b191d4990d")]
public partial class IsPerformingSpecialAttackCondition : Condition
{
    [SerializeReference] public BlackboardVariable<EnemyAI> Agent;

    public override bool IsTrue()
    {
        var agent = Agent?.Value;
        if (agent == null)
            return false;
        return agent.isPerformingSpecialAttack;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
