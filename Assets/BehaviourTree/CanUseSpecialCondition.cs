using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CanUseSpecial", story: "[Enemy] can perform special", category: "Conditions", id: "a9de3b27b8580c007451c6342647aad0")]
public partial class CanUseSpecialCondition : Condition
{
    [SerializeReference] public BlackboardVariable<EnemyAI> Enemy;

    public override bool IsTrue()
    {
        var agent = Enemy?.Value;
        if (agent == null)
            return false;

        return agent.CanPerformSpecialAttack();
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
