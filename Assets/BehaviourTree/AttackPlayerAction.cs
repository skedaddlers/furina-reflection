using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AttackPlayer", story: "[Enemy] Attacks Player", category: "Action", id: "e80f79e52cd2a66dc87175e3f366b48f")]
public partial class AttackPlayerAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyAI> Enemy;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var enemy = Enemy?.Value;
        if (enemy == null)
            return Status.Failure;

        // Kalau musuh sudah kabur / ga kelihatan, atau keluar jarak serang
        if (!enemy.SeePlayer() || !enemy.InAttackRange())
        {
            enemy.StopChasing();
            return Status.Failure;
        }

        // Serang (cooldown di-handle di EnemyAI)
        enemy.AttackPlayer();
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

