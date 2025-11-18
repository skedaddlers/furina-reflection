using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SpecialAttack", story: "[EliteEnemy] performs special_attack", category: "Action", id: "931704903703397e6901f03f7e3dab42")]
public partial class SpecialAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyAI> EliteEnemy;
    protected override Status OnStart()
    {
        var eliteEnemy = EliteEnemy?.Value;
        if (eliteEnemy == null)
            return Status.Failure;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var eliteEnemy = EliteEnemy?.Value;
        if (eliteEnemy == null)
            return Status.Failure;

        eliteEnemy.SpecialAttack();
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

