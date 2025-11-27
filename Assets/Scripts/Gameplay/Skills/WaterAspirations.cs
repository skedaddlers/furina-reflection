using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WaterAspirations", menuName = "Furina/Skills/Water Aspirations")]
public class WaterAspirations : SkillBase
{
    // this skill gives the player a water shield for a short duration
    public override void OnSkillActivate(GameObject caster)
    {
        base.OnSkillActivate(caster);

        Debug.Log($"{skillName} activated by {caster.name}");
    }
}