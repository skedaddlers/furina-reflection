using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CheckPlayerInRange", story: "[Enemy] see player", category: "Action/Conditional", id: "e9f7d98a21d5b461bd45e48d0b3275ca")]
public partial class CheckPlayerInRangeAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyAI> Enemy;
    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Enemy == null || Enemy.Value == null)
            return Status.Failure;

        if (Enemy.Value.SeePlayer())
            return Status.Success;
        
        Enemy.Value.StopChasing();
        return Status.Failure;
    }

    protected override void OnEnd()
    {
    }
}

