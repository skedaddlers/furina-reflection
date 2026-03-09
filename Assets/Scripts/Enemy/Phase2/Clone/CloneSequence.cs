using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Boss/CloneSequence")]
public class CloneSequence : ScriptableObject
{
    public CloneProfileTag profileTag;

    public List<CloneAction> actions = new List<CloneAction>();

    public float baseWeight = 1f;

    [HideInInspector] public float cachedWeight;
}

public enum CloneProfileTag
{
    Melee,
    Ranged,
    SkillCaster,
    Acrobat,
    Defensive
}