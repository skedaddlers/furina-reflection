using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Boss/Sequence")]
public class BossSequence : ScriptableObject
{
    public BossIntent intent;
    public string profileName; // Optional tag to indicate which player profile this sequence is best suited for

    public List<BossAction> actions;

    public float baseWeight = 1f;
    public float cooldown = 2f;

    [HideInInspector] public float lastUsedTime;
    [HideInInspector] public int usageCount;
}

[System.Serializable]
public class BossAction
{
    public ActionType type;

    public MovementAction movement;
    public BossSkill skill;
}

[System.Serializable]
public class MovementAction
{
    public MovementType movementType;

    public float duration;
    public float speed;
    public float distance;
}

public enum ActionType
{
    Movement,
    Skill
}

public enum MovementType
{
    DashToPlayer,
    StrafePlayer,
    Retreat,
    Reposition
}

public enum BossIntent
{
    Pressure,
    Zone,
    Punish,
    Reposition,
    Bait,
    Reset,
    RageBurst
}