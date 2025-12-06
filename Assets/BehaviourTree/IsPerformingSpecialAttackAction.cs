using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "IsPerformingSpecialAttack", story: "[Enemy] is PerformingSpecial", category: "Action", id: "c9c983c73669c34ecc65ee18f3f80c06")]
public partial class IsPerformingSpecialAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyAI> Enemy;
    protected override Status OnStart()
    {
        var enemy = Enemy?.Value;
        if (enemy == null)
            return Status.Failure;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var enemy = Enemy?.Value;
        if (enemy == null)
            return Status.Failure;
        // If the enemy is performing a special attack, return Failure to indicate the condition is met
        return enemy.isPerformingSpecialAttack ? Status.Failure : Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

