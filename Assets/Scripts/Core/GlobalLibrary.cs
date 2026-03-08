using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Library/Game Components Library")]
public class GlobalLibrary : ScriptableObject
{
    public List<WeaponBase> allWeapons;
    public List<SkillBase> allSkills;

    [Header("Enemies")]
    public List<GameObject> commonEnemies;
    public List<GameObject> eliteEnemies;
    public List<GameObject> bossEnemies;

    [Header("Boss Sequences")]
    public List<BossSequence> bossSequences;

    public List<Color> rarityColors;
}
