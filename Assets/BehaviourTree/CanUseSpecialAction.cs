using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CanUseSpecial", story: "[Enemy] can use special attack", category: "Action/Conditional", id: "13091ad616fa1248021e9be06bff2682")]
public partial class CanUseSpecialAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyAI> Enemy;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Enemy.Value == null)
            return Status.Failure;  
        if(Enemy.Value.CanPerformSpecialAttack() == false)
            return Status.Failure;
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

