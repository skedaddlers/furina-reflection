using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LetThePeopleRejoice", menuName = "Furina/Skills/Let The People Rejoice")]
public class LetThePeopleRejoice : SkillBase
{
    // tbis skill increases the damage of the player
    // but drains their health over time
    // gains hp when enemies are defeated
    public override void OnSkillActivate(GameObject caster)
    {
        base.OnSkillActivate(caster);

        Debug.Log($"{skillName} activated by {caster.name}");
    }
}