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
    private bool rewardsGranted;
    private bool deathEventNotified;

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

        GrantDeathRewards();
        NotifyDeathObservers();
        Destroy(gameObject);
    }

    public void GrantDeathRewards()
    {
        if (rewardsGranted)
            return;

        rewardsGranted = true;

        GlobalDifficultyState diff = GlobalDifficultyState.Instance;
        int scaledXpReward = diff != null ? diff.ScaleRewardAmount(xpReward) : Mathf.Max(0, xpReward);
        int scaledGoldReward = diff != null ? diff.ScaleRewardAmount(goldReward) : Mathf.Max(0, goldReward);
        int scaledScoreValue = diff != null ? diff.ScaleRewardAmount(scoreValue) : Mathf.Max(0, scoreValue);

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddXP(scaledXpReward);
            PlayerStats.Instance.AddGold(scaledGoldReward);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scaledScoreValue);
        }
    }

    public void NotifyDeathObservers()
    {
        if (deathEventNotified)
            return;

        deathEventNotified = true;
        OnAnyDeath?.Invoke(this);
    }
}
