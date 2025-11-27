using UnityEngine;
using System;

public enum EnemyType
{
    Basic,
    Elite,
    Boss
}

public class Enemy : MonoBehaviour
{
    public EnemyType enemyType = EnemyType.Basic;
    public int xpReward = 15;
    public int goldReward = 10;
    public static event Action<Enemy> OnAnyDeath;

    void Awake()
    {
        GetComponent<Health>().onDeath += HandleDeath;
    }

    private void HandleDeath()
    {

        // Reward player with XP and Gold upon enemy death
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddXP(xpReward);
            PlayerStats.Instance.AddGold(goldReward);
        }
        OnAnyDeath?.Invoke(this);
        Destroy(gameObject);
    }
}