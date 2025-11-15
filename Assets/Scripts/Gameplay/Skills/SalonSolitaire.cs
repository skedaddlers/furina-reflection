using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SalonSolitaire", menuName = "Furina/Skills/Salon Solitaire")]
public class SalonSolitaire : SkillBase
{

    public override void OnSkillActivate(GameObject caster)
    {
        base.OnSkillActivate(caster);

        Debug.Log($"{skillName} activated by {caster.name}");
    }
}