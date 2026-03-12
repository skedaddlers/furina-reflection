using UnityEngine;

public class PlayerActionTracker : MonoBehaviour
{
    public static PlayerActionTracker Instance;

    public CloneActionType lastPlayerAction;
    public float LastMeleeTime { get; private set; } = -999f;
    public float LastRangedTime { get; private set; } = -999f;
    public float LastSkillTime { get; private set; } = -999f;
    public float LastDodgeTime { get; private set; } = -999f;

    void Awake()
    {
        Instance = this;
    }

    public void RegisterMelee()
    {
        lastPlayerAction = CloneActionType.MeleeAttack;
        LastMeleeTime = Time.time;
    }

    public void RegisterRanged()
    {
        lastPlayerAction = CloneActionType.RangedAttack;
        LastRangedTime = Time.time;
    }

    public void RegisterSkill()
    {
        lastPlayerAction = CloneActionType.SkillCast;
        LastSkillTime = Time.time;
    }

    public void RegisterDodge()
    {
        lastPlayerAction = CloneActionType.Dodge;
        LastDodgeTime = Time.time;
    }
}
