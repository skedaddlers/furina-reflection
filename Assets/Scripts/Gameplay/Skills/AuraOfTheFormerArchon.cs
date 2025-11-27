using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AuraOfTheFormerArchon", menuName = "Furina/Skills/Aura Of The Former Archon")]
public class AuraOfTheFormerArchon : SkillBase
{

    // this skill gives the player a damaging aura for a short duration
    public override void OnSkillActivate(GameObject caster)
    {
        base.OnSkillActivate(caster);

        Debug.Log($"{skillName} activated by {caster.name}");
    }
}