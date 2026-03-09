using UnityEngine;

public class PlayerActionTracker : MonoBehaviour
{
    public static PlayerActionTracker Instance;

    public CloneActionType lastPlayerAction;

    void Awake()
    {
        Instance = this;
    }

    public void RegisterMelee()
    {
        lastPlayerAction = CloneActionType.MeleeAttack;
    }

    public void RegisterRanged()
    {
        lastPlayerAction = CloneActionType.RangedAttack;
    }

    public void RegisterSkill()
    {
        lastPlayerAction = CloneActionType.SkillCast;
    }

    public void RegisterDodge()
    {
        lastPlayerAction = CloneActionType.Dodge;
    }
}