using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TearsOfTheSinner", menuName = "Furina/Skills/Tears Of The Sinner")]
public class TearsOfTheSinner : SkillBase
{
    // this skill summons a rain of tears that damages all enemies over time
    public override void OnSkillActivate(GameObject caster)
    {
        base.OnSkillActivate(caster);

        Debug.Log($"{skillName} activated by {caster.name}");
    }
}