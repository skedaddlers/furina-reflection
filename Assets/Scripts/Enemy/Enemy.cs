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
    [Tooltip("If true, rewards/event/destroy on death are handled externally.")]
    public bool SuppressDefaultDeathHandling = false;
    public Renderer enemyRenderer;
    public Color rendererColor;
    public Transform healthBar;
    public int xpReward = 15;
    public int goldReward = 10;
    public int scoreValue = 100;
    public static event Action<Enemy> OnAnyDeath;

    void Awake()
    {
        GetComponent<Health>().onDeath += HandleDeath;
        if (enemyRenderer != null)
        {
            rendererColor = enemyRenderer.material.color;
        }
    }

    private void HandleDeath()
    {
        if (SuppressDefaultDeathHandling)
            return;

        // Reward player with XP and Gold upon enemy death
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddXP(xpReward);
            PlayerStats.Instance.AddGold(goldReward);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
        }
        OnAnyDeath?.Invoke(this);
        Destroy(gameObject);
    }
}
