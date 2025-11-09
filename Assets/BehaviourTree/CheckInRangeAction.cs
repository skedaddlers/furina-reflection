using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CheckInRange", story: "Check if [Enemy] see Player", category: "Action", id: "13aebea7bfb9d8bc7a6eace213671495")]
public partial class CheckInRangeAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyAI> Enemy;
    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

