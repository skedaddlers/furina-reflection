using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LetThePeopleRejoice", menuName = "Furina/Skills/Let The People Rejoice")]
public class LetThePeopleRejoice : SkillBase
{

    public override void OnSkillActivate(GameObject caster)
    {
        base.OnSkillActivate(caster);

        Debug.Log($"{skillName} activated by {caster.name}");
    }
}