using UnityEngine;

[DisallowMultipleComponent]
public class EnemyStats : MonoBehaviour
{
    [Header("Core")]
    public int level = 1;
    public float defense = 0f;

    [Header("Crit")]
    [Range(0f, 1f)] public float critRate = 0f;
    public float critMultiplier = 1.5f;
}
