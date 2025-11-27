using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SalonSolitaire", menuName = "Furina/Skills/Salon Solitaire")]
public class SalonSolitaire : SkillBase
{
    // this skill summons furina's 3 salon members for a short duration
    // salon members attack nearby enemies
    public override void OnSkillActivate(GameObject caster)
    {
        base.OnSkillActivate(caster);

        Debug.Log($"{skillName} activated by {caster.name}");
    }
}