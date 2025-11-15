using System;
using Unity.Behavior;
using UnityEngine;
using Composite = Unity.Behavior.Composite;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Attack", story: "Attack Player", category: "Flow/Repeat", id: "14a44b9c4b9d0c311ddb8ad9918efd71")]
public partial class AttackSequence : Composite
{
    [SerializeReference] public Node OutputPort;

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

