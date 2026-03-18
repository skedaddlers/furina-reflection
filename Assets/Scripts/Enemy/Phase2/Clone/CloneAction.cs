using UnityEngine;

[System.Serializable]
public class CloneAction
{
    public CloneActionType type;

    public float duration = 0.6f;
    public float speed = 6f;
}

public enum CloneActionType
{
    DashToPlayer,
    StrafePlayer,
    Retreat,
    MeleeAttack,
    RangedAttack,
    SkillCast,
    Dodge,
    Heal
}
