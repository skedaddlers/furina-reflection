using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    public GameObject transformationEffectPrefab;
    public bool withDialogue = true;
    public List<Dialogue> phase1StartDialogues;
    public int showPhase1BossHPBarAtIndex;
    public List<Dialogue> phase1EndDialogues;
    public float transformationEffectDelay = 2.8f;
    public List<Dialogue> phase2StartDialogues;
    public List<Dialogue> phase2EndDialogues;
    public BossPhase CurrentBossPhase { get; private set; } = BossPhase.Phase1;

    private FocalorsPhase1AI focalorsPhase1Instance;
    private FocalorsPhase2AI focalorsPhase2Instance;
    private Health phase1Health;
    private Health phase2Health;

    private GameObject currentBoss;
    private bool isTransitioning = false;

    public void SpawnFocalorsPhase1()
    {
        if (currentBoss != null || isTransitioning) return;

        currentBoss = Instantiate(focalorsPhase1Prefab, focalorsPhase1SpawnPoint.position, Quaternion.identity);
        focalorsPhase1Instance = currentBoss.GetComponent<FocalorsPhase1AI>();
        currentBoss.transform.SetParent(transform);
        if (focalorsPhase1Instance != null)
            focalorsPhase1Instance.SetCanAct(false);

        Enemy phase1Enemy = currentBoss.GetComponent<Enemy>();
        if (phase1Enemy != null)
            phase1Enemy.SuppressDefaultDeathHandling = true;

        phase1Health = currentBoss.GetComponent<Health>();
        if (phase1Health != null)
        {
            phase1Health.onDeath -= HandlePhase1Defeated;
            phase1Health.onDeath += HandlePhase1Defeated;
        }

        if (!withDialogue)
        {
            if (UIManager.Instance != null && UIManager.Instance.bossHPBarUI != null)
                UIManager.Instance.bossHPBarUI.InitForBossFight(focalorsPhase1Instance.GetComponent<BossHealthBar>());

            if (focalorsPhase1Instance != null)
                focalorsPhase1Instance.SetCanAct(true);
            return;
        }

        if (UIManager.Instance != null && UIManager.Instance.bossHPBarUI != null)
            UIManager.Instance.bossHPBarUI.SetActive(false);

        // delay for dialogue
        UIManager.Instance.dialogueUI.StartDialogueSequence(phase1StartDialogues);
        float durationUntillBossHPBar = UIManager.Instance.dialogueUI.GetTotalDialogueSequenceDuration(phase1StartDialogues) * (showPhase1BossHPBarAtIndex / (float)phase1StartDialogues.Count);
        Invoke(nameof(ShowBossHPBar), durationUntillBossHPBar);
        Invoke(nameof(EnableFocalorsPhase1), UIManager.Instance.dialogueUI.GetTotalDialogueSequenceDuration(phase1StartDialogues));
    }

    void ShowBossHPBar()
    {
        if (UIManager.Instance != null && UIManager.Instance.bossHPBarUI != null)
            UIManager.Instance.bossHPBarUI.InitForBossFight(focalorsPhase1Instance.GetComponent<BossHealthBar>());
    }

    void EnableFocalorsPhase1()
    {
        if (focalorsPhase1Instance != null)
        {
            focalorsPhase1Instance.SetCanAct(true);
        }
    }

    private void HandlePhase1Defeated()
    {
        if (CurrentBossPhase != BossPhase.Phase1 || isTransitioning)
            return;
        if (phase1Health == null || phase1Health.CurrentHealth > 0f)
            return;
        
        focalorsPhase1Instance.SetCanAct(false);
        UIManager.Instance.dialogueUI.StartDialogueSequence(phase1EndDialogues);
        float durationUntillPhase2 = UIManager.Instance.dialogueUI.GetTotalDialogueSequenceDuration(phase1EndDialogues);
        Invoke(nameof(StartPhase2), durationUntillPhase2);
    }

    private void StartPhase2()
    {
        StartCoroutine(TransitionToPhase2());
    }

    private IEnumerator TransitionToPhase2()
    {
        CancelInvoke(nameof(EnableFocalorsPhase1));
        isTransitioning = true;
        CurrentBossPhase = BossPhase.Phase2;

        Vector3 deathPos =
            focalorsPhase1Instance != null
            ? focalorsPhase1Instance.transform.position
            : focalorsPhase1SpawnPoint.position;

        if (transformationEffectPrefab != null)
            Instantiate(transformationEffectPrefab, deathPos, Quaternion.identity);

        if (focalorsPhase1Instance != null)
            focalorsPhase1Instance.SetCanAct(false);

        if (UIManager.Instance != null && UIManager.Instance.bossHPBarUI != null)
            UIManager.Instance.bossHPBarUI.SetActive(false);

        yield return new WaitForSeconds(transformationEffectDelay);

        if (phase1Health != null)
        {
            phase1Health.onDeath -= HandlePhase1Defeated;
            phase1Health = null;
        }

        if (focalorsPhase1Instance != null)
        {
            Destroy(focalorsPhase1Instance.gameObject);
            focalorsPhase1Instance = null;
        }

        currentBoss = null;
        SpawnFocalorsPhase2(deathPos);
        isTransitioning = false;
    }

    public void SpawnFocalorsPhase2(Vector3 pos)
    {
        if (currentBoss != null) return;

        GameObject bossPhase2GO = Instantiate(focalorsPhase2Prefab, pos, Quaternion.identity);
        bossPhase2GO.transform.SetParent(transform);
        focalorsPhase2Instance = bossPhase2GO.GetComponent<FocalorsPhase2AI>();
        currentBoss = bossPhase2GO;

        phase2Health = bossPhase2GO.GetComponent<Health>();
        if (phase2Health != null)
        {
            phase2Health.onDeath -= OnBossDefeated;
            phase2Health.onDeath += OnBossDefeated;
        }

        if (UIManager.Instance != null && UIManager.Instance.bossHPBarUI != null)
            UIManager.Instance.bossHPBarUI.InitForBossFight(bossPhase2GO.GetComponent<BossHealthBar>());
    }

    public void OnBossDefeated()
    {
        if (isTransitioning)
            return;

        if (CurrentBossPhase == BossPhase.Phase1)
        {
            HandlePhase1Defeated();
        }
        else
        {
            if (phase2Health != null)
                phase2Health.onDeath -= OnBossDefeated;

            if (UIManager.Instance != null && UIManager.Instance.bossHPBarUI != null)
                UIManager.Instance.bossHPBarUI.SetActive(false);

            GameManager.Instance.OnBossRoomCleared();
        }
    }

    private void OnDestroy()
    {
        if (phase1Health != null)
            phase1Health.onDeath -= HandlePhase1Defeated;

        if (phase2Health != null)
            phase2Health.onDeath -= OnBossDefeated;
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
