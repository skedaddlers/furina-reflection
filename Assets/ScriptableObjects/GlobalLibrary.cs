using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Library/Game Components Library")]
public class GlobalLibrary : ScriptableObject
{
    public List<WeaponBase> allWeapons;
    public List<SkillBase> allSkills;
}
