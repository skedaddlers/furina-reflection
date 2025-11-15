using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "chase", story: "[Enemy] chase player", category: "Action", id: "d19865bcafe678ab9081a3a7cf04a8a6")]
public partial class ChaseAction : Action
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

        // Kalau sudah tidak lihat player, stop dan gagal
        if (!enemy.SeePlayer())
        {
            enemy.StopChasing();
            return Status.Failure;
        }

        // Kalau sudah cukup dekat, berhenti jalan dan sukses
        if (enemy.InAttackRange())
        {
            enemy.StopChasing();
            return Status.Success;
        }

        // Kalau masih jauh, kejar terus
        enemy.ChasePlayer();
        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}

