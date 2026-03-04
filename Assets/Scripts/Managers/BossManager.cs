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
    public int showPhase2BossHPBarAtIndex;
    public List<Dialogue> phase2CloneDeathDialogues;
    public List<Dialogue> healthBasedPhase2Dialogues;
    public List<Dialogue> phase2EndDialogues;

    public BossPhase CurrentBossPhase { get; private set; } = BossPhase.Phase1;

    private FocalorsPhase1AI focalorsPhase1Instance;
    private FocalorsPhase2AI focalorsPhase2Instance;
    private Health phase1Health;
    private Health phase2Health;

    private GameObject currentBoss;
    private bool isTransitioning = false;

    #region Phase 1

    public void SpawnFocalorsPhase1()
    {
        if (currentBoss != null || isTransitioning)
            return;

        currentBoss = Instantiate(focalorsPhase1Prefab, focalorsPhase1SpawnPoint.position, Quaternion.identity);
        currentBoss.transform.SetParent(transform);

        focalorsPhase1Instance = currentBoss.GetComponent<FocalorsPhase1AI>();
        phase1Health = currentBoss.GetComponent<Health>();

        SetupPhase1Boss();

        if (!withDialogue)
        {
            InitializeBossUI(focalorsPhase1Instance);
            EnablePhase1Boss();
            return;
        }

        StartPhase1Dialogue();
    }

    private void SetupPhase1Boss()
    {
        if (focalorsPhase1Instance != null)
            focalorsPhase1Instance.SetCanAct(false);

        Enemy phase1Enemy = currentBoss.GetComponent<Enemy>();
        if (phase1Enemy != null)
            phase1Enemy.SuppressDefaultDeathHandling = true;

        if (phase1Health != null)
        {
            phase1Health.onDeath -= HandlePhase1Defeated;
            phase1Health.onDeath += HandlePhase1Defeated;
        }
    }

    private void StartPhase1Dialogue()
    {
        if (UIManager.Instance?.bossHPBarUI != null)
            UIManager.Instance.bossHPBarUI.SetActive(false);

        UIManager.Instance.dialogueUI.StartDialogueSequence(phase1StartDialogues);

        focalorsPhase1Instance.SetImmune(true);

        float totalDuration = UIManager.Instance.dialogueUI
            .GetTotalDialogueSequenceDuration(phase1StartDialogues);

        float hpBarDelay = totalDuration *
            (showPhase1BossHPBarAtIndex / (float)phase1StartDialogues.Count);

        Invoke(nameof(ShowBossHPBar), hpBarDelay);
        Invoke(nameof(EnableBoss), totalDuration);
    }

    private void EnablePhase1Boss()
    {
        if (focalorsPhase1Instance == null)
            return;

        focalorsPhase1Instance.SetCanAct(true);
        focalorsPhase1Instance.SetImmune(false);
    }

    private void HandlePhase1Defeated()
    {
        if (CurrentBossPhase != BossPhase.Phase1 || isTransitioning)
            return;

        if (phase1Health == null || phase1Health.CurrentHealth > 0f)
            return;

        if (!withDialogue)
        {
            StartPhase2();
            return;
        }

        focalorsPhase1Instance.SetCanAct(false);

        UIManager.Instance.dialogueUI.StartDialogueSequence(phase1EndDialogues);

        float duration = UIManager.Instance.dialogueUI
            .GetTotalDialogueSequenceDuration(phase1EndDialogues);

        Invoke(nameof(StartPhase2), duration);
    }

    #endregion

    #region Phase Transition

    private void StartPhase2()
    {
        StartCoroutine(TransitionToPhase2());
    }

    private IEnumerator TransitionToPhase2()
    {
        CancelInvoke(nameof(EnableBoss));

        isTransitioning = true;
        CurrentBossPhase = BossPhase.Phase2;

        Vector3 deathPos = focalorsPhase1Instance != null
            ? focalorsPhase1Instance.transform.position
            : focalorsPhase1SpawnPoint.position;

        if (transformationEffectPrefab != null)
            Instantiate(transformationEffectPrefab, deathPos, Quaternion.identity);

        if (focalorsPhase1Instance != null)
            focalorsPhase1Instance.SetCanAct(false);

        if (UIManager.Instance?.bossHPBarUI != null)
            UIManager.Instance.bossHPBarUI.SetActive(false);

        yield return new WaitForSeconds(transformationEffectDelay);

        CleanupPhase1();
        SpawnFocalorsPhase2(deathPos);

        isTransitioning = false;
    }

    private void CleanupPhase1()
    {
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
    }

    #endregion

    #region Phase 2

    public void SpawnFocalorsPhase2(Vector3 pos)
    {
        if (currentBoss != null)
            return;

        currentBoss = Instantiate(focalorsPhase2Prefab, pos, Quaternion.identity);
        currentBoss.transform.SetParent(transform);

        focalorsPhase2Instance = currentBoss.GetComponent<FocalorsPhase2AI>();
        focalorsPhase2Instance.onCloneDies -= StartCloneDeathDialogue;
        focalorsPhase2Instance.onCloneDies += StartCloneDeathDialogue;
        phase2Health = currentBoss.GetComponent<Health>();

        if (phase2Health != null)
        {
            phase2Health.onDeath -= OnBossDefeated;
            phase2Health.onDeath += OnBossDefeated;
        }

        if (!withDialogue)
        {
            InitializeBossUI(focalorsPhase2Instance);
            return;
        }

        StartPhase2Dialogue();
    }

    private void StartPhase2Dialogue()
    {
        UIManager.Instance?.dialogueUI?.StartDialogueSequence(phase2StartDialogues);

        float totalDuration = UIManager.Instance.dialogueUI
            .GetTotalDialogueSequenceDuration(phase2StartDialogues);

        focalorsPhase2Instance.SetImmune(true);
        focalorsPhase2Instance.SetCanAct(false);

        float hpBarDelay = totalDuration *
            (showPhase2BossHPBarAtIndex / (float)phase2StartDialogues.Count);

        Invoke(nameof(ShowBossHPBar), hpBarDelay);
        Invoke(nameof(EnableBoss), totalDuration);
    }

    
    private void StartCloneDeathDialogue()
    {
        if (focalorsPhase2Instance != null)
            focalorsPhase2Instance.onCloneDies -= StartCloneDeathDialogue;

        if (!withDialogue)
        {
            NotifyPhase2CloneDeath();
            return;
        }

        UIManager.Instance?.dialogueUI?.StartDialogueSequence(phase2CloneDeathDialogues);
        float duration = UIManager.Instance.dialogueUI
            .GetTotalDialogueSequenceDuration(phase2CloneDeathDialogues);
        Invoke(nameof(NotifyPhase2CloneDeath), duration);
    }

    private void NotifyPhase2CloneDeath()
    {
        if (focalorsPhase2Instance != null)
            focalorsPhase2Instance.NotifyCloneDeath();

        Debug.Log("Notified Focalors Phase 2 of clone death");
    }

    #endregion

    #region Shared

    void ShowBossHPBar()
    {
        if (UIManager.Instance?.bossHPBarUI == null)
            return;

        if (CurrentBossPhase == BossPhase.Phase1 && focalorsPhase1Instance != null)
            UIManager.Instance.bossHPBarUI.InitForBossFight(
                focalorsPhase1Instance.GetComponent<BossHealthBar>());

        else if (CurrentBossPhase == BossPhase.Phase2 && focalorsPhase2Instance != null)
            UIManager.Instance.bossHPBarUI.InitForBossFight(
                focalorsPhase2Instance.GetComponent<BossHealthBar>());
    }

    void EnableBoss()
    {
        if (CurrentBossPhase == BossPhase.Phase1)
            EnablePhase1Boss();
        else if (CurrentBossPhase == BossPhase.Phase2 && focalorsPhase2Instance != null)
        {
            focalorsPhase2Instance.SetCanAct(true);
            focalorsPhase2Instance.SetImmune(false);
        }
    }


    private void InitializeBossUI(Component bossComponent)
    {
        if (UIManager.Instance?.bossHPBarUI == null || bossComponent == null)
            return;

        UIManager.Instance.bossHPBarUI.InitForBossFight(
            bossComponent.GetComponent<BossHealthBar>());
    }

    public void OnBossDefeated()
    {
        if (isTransitioning)
            return;

        if (CurrentBossPhase == BossPhase.Phase1)
        {
            HandlePhase1Defeated();
            return;
        }

        if (phase2Health != null)
            phase2Health.onDeath -= OnBossDefeated;

        if (withDialogue)
        {
            UIManager.Instance?.dialogueUI?.StartDialogueSequence(phase2EndDialogues);
            float duration = UIManager.Instance.dialogueUI
                .GetTotalDialogueSequenceDuration(phase2EndDialogues);
            Invoke(nameof(CleanupAfterBossDefeat), duration);
        }
        else
        {
            CleanupAfterBossDefeat();
        }
    }

    private void CleanupAfterBossDefeat()
    {
        if (focalorsPhase2Instance != null)
        {
            Destroy(focalorsPhase2Instance.gameObject);
            focalorsPhase2Instance = null;
        }

        if (UIManager.Instance?.bossHPBarUI != null)
            UIManager.Instance.bossHPBarUI.SetActive(false);

        currentBoss = null;
        GameManager.Instance.OnBossRoomCleared();
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

    #endregion
}