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
    public static System.Action<Room, bool> OnBossFightActivityChanged;
    public static System.Action<Room, int> OnBossPhaseProgressed;

    [Header("Boss Settings")]
    public Transform focalorsPhase1SpawnPoint;
    public Transform focalorsPhase2SpawnPoint;
    public GameObject focalorsPhase1Prefab;
    public GameObject focalorsPhase2Prefab;
    public GameObject transformationEffectPrefab;

    [Header("Debug Settings")]
    public bool withDialogue = true;
    public bool skipPhase1 = false;

    public List<Dialogue> phase1StartDialogues;
    public int showPhase1BossHPBarAtIndex;
    public List<Dialogue> phase1EndDialogues;

    public float transformationEffectDelay = 2.8f;

    public List<Dialogue> phase2StartDialogues;
    public int showPhase2BossHPBarAtIndex;
    public List<Dialogue> phase2CloneDeathDialogues;
    public List<Dialogue> healthBasedPhase2Dialogues;
    public float healthDialogueTriggerPercentage = 0.5f;
    public List<Dialogue> phase2EndDialogues;

    public BossPhase CurrentBossPhase { get; private set; } = BossPhase.Phase1;

    [Header("SFX")]
    public AudioClip phase2spawnSound;

    private FocalorsPhase1AI focalorsPhase1Instance;
    private FocalorsPhase2AI focalorsPhase2Instance;
    private Health phase1Health;
    private Health phase2Health;
    private Room parentRoom;

    private GameObject currentBoss;
    private bool isTransitioning = false;

    #region Phase 1

    void Start()
    {
        withDialogue =GameManager.Instance.withDialogue;
        parentRoom = GetComponent<Room>();
    }
    public void SpawnFocalorsPhase1()
    {
        if (currentBoss != null || isTransitioning)
            return;

        if (skipPhase1)
        {
            CurrentBossPhase = BossPhase.Phase2;
            SpawnFocalorsPhase2(focalorsPhase1SpawnPoint.position);
            return;
        }

        currentBoss = Instantiate(focalorsPhase1Prefab, focalorsPhase1SpawnPoint.position, Quaternion.identity);
        currentBoss.transform.SetParent(transform);
        ApplyBossRoomLevel(currentBoss);
        NotifyBossPhaseProgressed(1);

        focalorsPhase1Instance = currentBoss.GetComponent<FocalorsPhase1AI>();
        phase1Health = currentBoss.GetComponent<Health>();

        SetupPhase1Boss();

        if (!withDialogue)
        {
            InitializeBossUI(focalorsPhase1Instance);
            EnablePhase1Boss();
            AudioManager.Instance?.PlayBossMusic();
            return;
        }

        AudioManager.Instance?.PlayBossMusic();

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

        focalorsPhase1Instance.SetImmune(true);

        if (UIManager.Instance?.dialogueUI == null)
        {
            ShowBossHPBar();
            EnableBoss();
            return;
        }

        bool hpBarShown = false;
        Coroutine hpBarCoroutine = StartBossHPBarDelay(
            phase1StartDialogues,
            showPhase1BossHPBarAtIndex,
            () => hpBarShown = true);

        UIManager.Instance.dialogueUI.StartDialogueSequence(phase1StartDialogues, () =>
        {
            StopBossHPBarDelay(hpBarCoroutine);

            if (!hpBarShown)
                ShowBossHPBar();

            EnableBoss();
        });
    }

    private void EnablePhase1Boss()
    {
        if (focalorsPhase1Instance == null)
            return;

        focalorsPhase1Instance.SetCanAct(true);
        focalorsPhase1Instance.SetImmune(false);
        NotifyBossFightActivity(true);
    }

    private void HandlePhase1Defeated()
    {
        if (CurrentBossPhase != BossPhase.Phase1 || isTransitioning)
            return;

        if (phase1Health == null || phase1Health.CurrentHealth > 0f)
            return;

        if (!withDialogue)
        {
            NotifyBossFightActivity(false);
            StartPhase2();
            return;
        }

        focalorsPhase1Instance.SetCanAct(false);
        NotifyBossFightActivity(false);

        if (UIManager.Instance?.dialogueUI == null)
        {
            StartPhase2();
            return;
        }

        UIManager.Instance.dialogueUI.StartDialogueSequence(phase1EndDialogues, StartPhase2);
    }

    #endregion

    #region Phase Transition

    private void StartPhase2()
    {
        StartCoroutine(TransitionToPhase2());
    }

    private IEnumerator TransitionToPhase2()
    {
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
        ApplyBossRoomLevel(currentBoss);
        NotifyBossPhaseProgressed(2);
        Enemy f2 = currentBoss.GetComponent<Enemy>();
        f2.SuppressDefaultDeathHandling = true;
        focalorsPhase2Instance = currentBoss.GetComponent<FocalorsPhase2AI>();
        focalorsPhase2Instance.onCloneDies -= StartCloneDeathDialogue;
        focalorsPhase2Instance.onCloneDies += StartCloneDeathDialogue;
        focalorsPhase2Instance.onPhase2Death -= TransformAfterDefeat;
        focalorsPhase2Instance.onPhase2Death += TransformAfterDefeat;
        phase2Health = currentBoss.GetComponent<Health>();

        if (phase2spawnSound != null)
        {
            AudioManager.Instance?.PlaySFXNoOverlap(phase2spawnSound);
        }

        if (phase2Health != null)
        {
            phase2Health.onDeath -= OnBossDefeated;
            phase2Health.onDeath += OnBossDefeated;
            phase2Health.onHealthChanged -= CheckPhase2HealthForDialogue;
            phase2Health.onHealthChanged += CheckPhase2HealthForDialogue;
        }

        if (!withDialogue)
        {
            InitializeBossUI(focalorsPhase2Instance);
            focalorsPhase2Instance.SetCanAct(true);
            focalorsPhase2Instance.SetImmune(false);
            NotifyBossFightActivity(true);
            AudioManager.Instance?.PlayBossMusicPhase2();
            return;
        }

        StartPhase2Dialogue();
    }

    private void CheckPhase2HealthForDialogue(float currentHealth, float maxHealth)
    {
        if (currentHealth / maxHealth <= healthDialogueTriggerPercentage)
        {
            phase2Health.onHealthChanged -= CheckPhase2HealthForDialogue;
            StartHealthBasedDialogue();
        }
    }

    private void StartHealthBasedDialogue()
    {
        if (UIManager.Instance?.dialogueUI == null || !withDialogue)
            return;

        UIManager.Instance.dialogueUI.StartDialogueSequence(healthBasedPhase2Dialogues);
    }

    private void StartPhase2Dialogue()
    {
        focalorsPhase2Instance.SetImmune(true);
        focalorsPhase2Instance.SetCanAct(false);

        if (UIManager.Instance?.dialogueUI == null)
        {
            ShowBossHPBar();
            EnableBoss();
            return;
        }

        bool hpBarShown = false;
        Coroutine hpBarCoroutine = StartBossHPBarDelay(
            phase2StartDialogues,
            showPhase2BossHPBarAtIndex,
            () => hpBarShown = true);

        UIManager.Instance.dialogueUI.StartDialogueSequence(phase2StartDialogues, () =>
        {
            StopBossHPBarDelay(hpBarCoroutine);

            if (!hpBarShown)
                ShowBossHPBar();

            EnableBoss();
        });
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

        if (UIManager.Instance?.dialogueUI == null)
        {
            NotifyPhase2CloneDeath();
            return;
        }

        UIManager.Instance.dialogueUI.StartDialogueSequence(phase2CloneDeathDialogues, NotifyPhase2CloneDeath);
    }

    private void NotifyPhase2CloneDeath()
    {
        if (focalorsPhase2Instance != null)
            focalorsPhase2Instance.NotifyCloneDeath();
        NotifyBossPhaseProgressed(3);
        // Debug.Log("Notified Focalors Phase 2 of clone death");
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
        {
            AudioManager.Instance?.PlayBossMusicPhase2();
            UIManager.Instance.bossHPBarUI.InitForBossFight(
                focalorsPhase2Instance.GetComponent<BossHealthBar>());
        }
    }

    private Coroutine StartBossHPBarDelay(List<Dialogue> dialogues, int showBossHPBarAtIndex, System.Action onShown)
    {
        float hpBarDelay = GetBossHPBarDelay(dialogues, showBossHPBarAtIndex);

        if (hpBarDelay <= 0f)
        {
            ShowBossHPBar();
            onShown?.Invoke();
            return null;
        }

        return StartCoroutine(ShowBossHPBarAfterDelay(hpBarDelay, onShown));
    }

    private float GetBossHPBarDelay(List<Dialogue> dialogues, int showBossHPBarAtIndex)
    {
        if (UIManager.Instance?.dialogueUI == null || dialogues == null || dialogues.Count == 0)
            return 0f;

        float totalDuration = UIManager.Instance.dialogueUI.GetTotalDialogueSequenceDuration(dialogues);
        float normalizedIndex = Mathf.Clamp01(showBossHPBarAtIndex / (float)dialogues.Count);
        return totalDuration * normalizedIndex;
    }

    private IEnumerator ShowBossHPBarAfterDelay(float delay, System.Action onShown)
    {
        yield return new WaitForSeconds(delay);
        ShowBossHPBar();
        onShown?.Invoke();
    }

    private void StopBossHPBarDelay(Coroutine hpBarCoroutine)
    {
        if (hpBarCoroutine != null)
            StopCoroutine(hpBarCoroutine);
    }

    void EnableBoss()
    {
        if (CurrentBossPhase == BossPhase.Phase1)
            EnablePhase1Boss();
        else if (CurrentBossPhase == BossPhase.Phase2 && focalorsPhase2Instance != null)
        {
            focalorsPhase2Instance.SetCanAct(true);
            focalorsPhase2Instance.SetImmune(false);
            NotifyBossFightActivity(true);
        }
    }


    private void InitializeBossUI(Component bossComponent)
    {
        if (UIManager.Instance?.bossHPBarUI == null || bossComponent == null)
            return;

        UIManager.Instance.bossHPBarUI.InitForBossFight(
            bossComponent.GetComponent<BossHealthBar>());
    }

    private void ApplyBossRoomLevel(GameObject bossObject)
    {
        if (bossObject == null)
            return;

        if (parentRoom == null)
            parentRoom = GetComponent<Room>();

        int scaledLevel = parentRoom != null
            ? parentRoom.GetScaledEnemyLevel()
            : 1;

        var enemyStats = bossObject.GetComponent<EnemyStats>();
        if (enemyStats != null)
        {
            enemyStats.level = scaledLevel;
        }

        var phase2Boss = bossObject.GetComponent<FocalorsPhase2AI>();
        if (phase2Boss != null)
        {
            phase2Boss.SetEnemyLevel(scaledLevel);
        }
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
        NotifyBossFightActivity(false);
        UIManager.Instance?.dialogueUI?.StopDialogueSequence();

        // death animation > transfor to phase 1 > dialogue > cleanup
        focalorsPhase2Instance.DeathAndTransform();
    }

    public void TransformAfterDefeat() // anim event at the end of phase 2 death animation
    {
        Vector3 pos = focalorsPhase2Instance != null
            ? focalorsPhase2Instance.transform.position
            : focalorsPhase2SpawnPoint.position;
        Enemy defeatedBoss = focalorsPhase2Instance != null
            ? focalorsPhase2Instance.GetComponent<Enemy>()
            : null;
        defeatedBoss?.GrantDeathRewards();
        defeatedBoss?.NotifyDeathObservers();
        CleanupAfterBossDefeat();
        GameObject f = Instantiate(focalorsPhase1Prefab, pos, Quaternion.identity);
        f.transform.SetParent(transform);
        f.GetComponent<FocalorsPhase1AI>().SetCanAct(false);
        f.GetComponent<FocalorsPhase1AI>().SetImmune(true);

        // phase 2 death dialogue 
        if(withDialogue)
        {
            AudioManager.Instance?.PlayVictoryTransition();

            if (UIManager.Instance?.dialogueUI == null)
            {
                Win();
                return;
            }

            UIManager.Instance.dialogueUI.StartDialogueSequence(phase2EndDialogues, Win);
        }
        else
        {
            Win();
        }
    }

    private void Win()
    {
        AudioManager.Instance?.PlayVictoryMusic();
        GameManager.Instance.OnBossRoomCleared();
    }

    private void CleanupAfterBossDefeat()
    {
        NotifyBossFightActivity(false);

        if (focalorsPhase2Instance != null)
        {
            Destroy(focalorsPhase2Instance.gameObject);
            focalorsPhase2Instance = null;
        }

        if (UIManager.Instance?.bossHPBarUI != null)
            UIManager.Instance.bossHPBarUI.SetActive(false);

        currentBoss = null;
    }

    private void OnDestroy()
    {
        NotifyBossFightActivity(false);

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

    private void NotifyBossFightActivity(bool isActive)
    {
        if (parentRoom == null)
            parentRoom = GetComponent<Room>();

        if (parentRoom != null)
        {
            OnBossFightActivityChanged?.Invoke(parentRoom, isActive);
        }
    }

    private void NotifyBossPhaseProgressed(int phaseIndex)
    {
        if (parentRoom == null)
            parentRoom = GetComponent<Room>();

        if (parentRoom != null)
        {
            OnBossPhaseProgressed?.Invoke(parentRoom, Mathf.Clamp(phaseIndex, 0, 3));
        }
    }
}
