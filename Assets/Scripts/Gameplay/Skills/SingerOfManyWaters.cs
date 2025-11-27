using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SingerOfManyWaters", menuName = "Furina/Skills/Singer Of Many Waters")]
public class SingerOfManyWaters : SkillBase
{
    // this skill summons a singer that heals the player over time
    public override void OnSkillActivate(GameObject caster)
    {
        base.OnSkillActivate(caster);

        Debug.Log($"{skillName} activated by {caster.name}");
    }
}