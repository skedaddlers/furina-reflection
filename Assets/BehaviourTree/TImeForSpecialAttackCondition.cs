using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "TImeForSpecialAttack", story: "[Lawachurl] is ready to perform special attack", category: "Conditions", id: "5daa0198969bbce84a9e5e82004e95a2")]
public partial class TImeForSpecialAttackCondition : Condition
{
    [SerializeReference] public BlackboardVariable<EnemyAI> Lawachurl;

    public override bool IsTrue()
    {
        var lawachurl = Lawachurl?.Value;
        if (lawachurl == null)
            return false;
        if (lawachurl.CanPerformSpecialAttack() == false)
            return false;
        return true;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
