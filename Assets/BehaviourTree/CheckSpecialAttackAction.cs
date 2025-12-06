using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CheckSpecialAttack", story: "Check if [Enemy] [IsPerformingSpecialAttack]", category: "Action/Conditional", id: "13aebea7bfb9d8bc7a6eace213671495")]
public partial class CheckSpecialAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyAI> Enemy;
    [SerializeReference] public BlackboardVariable<EnemyAI> IsPerformingSpecialAttack;
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
