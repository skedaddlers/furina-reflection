using UnityEngine;

public enum BossPhase
{
    Phase1,
    Phase2
}
public class BossManager : MonoBehaviour
{
    [Header("Boss Settings")]
    public Transform focalorsPhase1SpawnPoint;
    public Transform focalorsPhase2SpawnPoint;
    public GameObject focalorsPhase1Prefab;
    public GameObject focalorsPhase2Prefab;
    public BossPhase CurrentBossPhase { get; private set; } = BossPhase.Phase1;

    private GameObject currentBoss;

    public void SpawnFocalorsPhase1()
    {
        if (currentBoss != null) return;

        currentBoss = Instantiate(focalorsPhase1Prefab, focalorsPhase1SpawnPoint.position, Quaternion.identity);
    }

    public void SpawnFocalorsPhase2()
    {
        if (currentBoss != null) return;

        GameObject bossPhase2GO = Instantiate(focalorsPhase2Prefab, focalorsPhase2SpawnPoint.position, Quaternion.identity);
        bossPhase2GO.transform.SetParent(transform);
        currentBoss = bossPhase2GO;
    }

    public void OnBossDefeated()
    {
        if (CurrentBossPhase == BossPhase.Phase1)
        {
            CurrentBossPhase = BossPhase.Phase2;
            SpawnFocalorsPhase2();
        }
        else
        {
            GameManager.Instance.OnBossRoomCleared();
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (focalorsPhase1SpawnPoint != null)
            Gizmos.DrawSphere(focalorsPhase1SpawnPoint.position, 0.5f);
        if (focalorsPhase2SpawnPoint != null)
            Gizmos.DrawSphere(focalorsPhase2SpawnPoint.position, 0.5f);
    }
#endif
}