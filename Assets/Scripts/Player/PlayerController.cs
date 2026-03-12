using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using DDAMAPEKitFramework;

public class PlayerController : MonoBehaviour, IStaggerable
{
    public PlayerStats stats;
    public SkillManager skillManager;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.3f;
    public float holdThreshold = 0.25f;
    public float speedMultiplier = 1f;
    public Transform cameraTransform;

    [Header("Stamina Settings")]
    public float dodgeStaminaCost = 25f;
    public float sprintStaminaCostPerSecond = 15f;

    [Header("Combat Lock / Auto Aim")]
    public float autoAimRadius = 12f;
    public float combatLockSeconds = 2.0f;
    public float breakDistance = 15f;
    public LayerMask enemyMask;

    [Header("Bow Aim Settings")]
    public GameObject bowCrosshair;
    public float bowAimFOV = 40f;
    public float bowFovLerpSpeed = 15f;
    public GameObject cameraTarget;

    private bool isBowAiming = false;
    [SerializeField] private CinemachineCamera mainCam;

    private float defaultFOV;
    private float _combatLockUntil = -1f;
    private Transform _combatTarget;
    private bool InCombatLock => Time.time < _combatLockUntil && _combatTarget != null;

    private Animator animator;
    private PlayerCombat playerCombat;
    private CharacterController controller;
    [SerializeField] private PlayerAnimationBinder _animBinder;

    private Vector3 velocity;
    private bool isGrounded;
    [SerializeField] private bool isDodging = false;
    private bool isSprinting = false;

    private float shiftPressedTime;
    private bool shiftHeld = false;
    private bool dashTriggered = false;
    private bool isStaggered = false;
    private Coroutine dodgeRoutine;
    private Coroutine staggerRoutine;
    public bool IsStaggered => isStaggered;
    [Header("Stagger Settings")]
    [SerializeField] private string staggerTrigger = "Hit";

    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        animator = GetComponent<Animator>();
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<CharacterController>();
        playerCombat = GetComponent<PlayerCombat>();
        skillManager = GetComponent<SkillManager>();
        _animBinder = GetComponent<PlayerAnimationBinder>();

        mainCam = GameObject.FindGameObjectWithTag("CinemachineCamera")
            ?.GetComponent<CinemachineCamera>();

        if (mainCam != null)
            defaultFOV = mainCam.Lens.FieldOfView;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        if (HandlePause()) return;

        UpdateGroundedState();
        if (isStaggered)
        {
            ApplyGravity();
            UpdateCamera();
            return;
        }
        HandleShiftInput();
        HandleCombatLockMaintenance();
        HandleMovement();
        ApplyGravity();
        UpdateCamera();
        HandleCombatInput();
        HandleSkillInput();
    }

    // =========================================================
    // CORE SECTIONS
    // =========================================================

    bool HandlePause()
    {
        if (!GameManager.Instance.IsPaused) return false;

        ExitBowAim();

        return true;
    }

    void UpdateGroundedState()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;
    }

    // =========================================================
    // SHIFT / DODGE / SPRINT
    // =========================================================

    void HandleShiftInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && isGrounded && !isDodging)
        {
                CancelCombatLock();
                shiftPressedTime = Time.time;
                shiftHeld = true;

                if (stats.HasEnoughStamina(dodgeStaminaCost))
                {
                    StartDodge();
                    dashTriggered = true;
                    playerCombat.ForceCancelAttack();
                }
            }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            shiftHeld = false;

            if (!dashTriggered &&
                (Time.time - shiftPressedTime) < holdThreshold &&
                !isDodging && isGrounded)
            {
                if (stats.HasEnoughStamina(dodgeStaminaCost))
                    StartDodge();
            }
            else
            {
                isSprinting = false;
            }

            dashTriggered = false;
        }

        bool wantsSprint =
            shiftHeld &&
            (Time.time - shiftPressedTime) >= holdThreshold &&
            !isDodging &&
            isGrounded &&
            stats.HasEnoughStamina(sprintStaminaCostPerSecond * 0.25f);

        if (wantsSprint)
        {
            isSprinting = true;
            dashTriggered = true;
            CancelCombatLock();
        }
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    void HandleCombatLockMaintenance()
    {
        if (InCombatLock)
        {
            if (_combatTarget == null ||
                Vector3.Distance(transform.position, _combatTarget.position) > breakDistance)
            {
                CancelCombatLock();
            }
        }
    }
    
    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 inputDir = new Vector3(h, 0, v).normalized;

        float baseMoveSpeed = stats != null ? stats.moveSpeed : walkSpeed;
        float sprintMoveSpeed = stats != null ?
            stats.moveSpeed + Mathf.Max(0f, runSpeed - walkSpeed) :
            runSpeed;

        float speed = (isSprinting ? sprintMoveSpeed : baseMoveSpeed) * speedMultiplier;

        if (isSprinting)
        {
            if (!stats.TrySpendStamina(sprintStaminaCostPerSecond * Time.deltaTime))
                isSprinting = false;
        }

        if (inputDir.magnitude >= 0.1f)
        {
            MoveWithInput(inputDir, speed, baseMoveSpeed);
        }
        else
        {
            HandleIdleRotation();
            animator.SetFloat("WalkSpeed", 0f, 0.1f, Time.deltaTime);
        }
    }

    void MoveWithInput(Vector3 inputDir, float speed, float baseMoveSpeed)
    {
        if (isBowAiming)
        {
            HandleBowMovement(baseMoveSpeed);
            return;
        }

        float targetAngle =
            Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg +
            cameraTransform.eulerAngles.y;

        Vector3 moveDir =
            Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

        if (InCombatLock && !isSprinting)
            FaceTarget(_combatTarget, 1f);
        else
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moveDir),
                Time.deltaTime * rotationSpeed
            );

        controller.Move(moveDir * speed * Time.deltaTime);
        animator.SetFloat("WalkSpeed", isSprinting ? 1f : 0.5f, 0.1f, Time.deltaTime);
    }

    void HandleBowMovement(float baseMoveSpeed)
    {
        Vector3 camFwd = cameraTransform.forward;
        camFwd.y = 0;
        camFwd.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(camRight),
            Time.deltaTime * rotationSpeed * 2f
        );

        Vector3 moveDir =
            camFwd * Input.GetAxis("Vertical") +
            camRight * Input.GetAxis("Horizontal");

        controller.Move(moveDir.normalized * baseMoveSpeed * speedMultiplier * Time.deltaTime);
        animator.SetFloat("WalkSpeed", moveDir.magnitude > 0 ? 0.5f : 0f);
    }

    void HandleIdleRotation()
    {
        if (isBowAiming)
        {
            Vector3 camRight = cameraTransform.right;
            camRight.y = 0;

            if (camRight.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(camRight);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    Time.deltaTime * rotationSpeed * 2f
                );
            }
        }
        else if (InCombatLock && !isSprinting)
        {
            FaceTarget(_combatTarget, 1.2f);
        }
    }

    // =========================================================
    // GRAVITY
    // =========================================================

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // =========================================================
    // CAMERA
    // =========================================================

    void UpdateCamera()
    {
        if (mainCam == null) return;

        float targetFov = isBowAiming ? bowAimFOV : defaultFOV;
        float sideOffset = isBowAiming ? 0.5f : 0f;

        cameraTarget.transform.localPosition = Vector3.Lerp(
            cameraTarget.transform.localPosition,
            new Vector3(
                cameraTarget.transform.localPosition.x,
                cameraTarget.transform.localPosition.y,
                sideOffset),
            Time.deltaTime * bowFovLerpSpeed
        );

        mainCam.Lens.FieldOfView = Mathf.Lerp(
            mainCam.Lens.FieldOfView,
            targetFov,
            Time.deltaTime * bowFovLerpSpeed
        );
    }

    // =========================================================
    // COMBAT INPUT
    // =========================================================

    void HandleCombatInput()
    {
        if (!HasBowEquipped)
        {
            if (Input.GetMouseButtonDown(0) && !isDodging && !playerCombat.IsAttacking)
            {
                if (playerCombat.TryUseWeapon())
                    PerformAttack();
            }
        }
        else
        {
            HandleBowInput();
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
            playerCombat.ForceCancelAttack();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ExitBowAim();
            playerCombat.loadout.Swap();
            playerCombat.ForceCancelAttack();
        }
    }

    void HandleSkillInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) skillManager.TryUseSkill(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) skillManager.TryUseSkill(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) skillManager.TryUseSkill(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) skillManager.TryUseSkill(3);
    }

    // =========================================================
    // REST OF YOUR ORIGINAL METHODS (UNCHANGED LOGIC)
    // =========================================================
    

    void PerformAttack()
    {
        var t = FindNearestEnemy(autoAimRadius);
        if (t) StartCombatLock(t);

        var set = _animBinder?.currentAnimSet;
        if (set == null || set.type == WeaponAnimType.Melee || set.type == WeaponAnimType.BombThrow)
            _animBinder?.PlayAttack(); // pakai trigger Attack
        else if (set.type == WeaponAnimType.OneHandGun || set.type == WeaponAnimType.TwoHandGun)
            _animBinder?.PlayShoot();  // pakai trigger Shoot
        else if (set.type == WeaponAnimType.ChannelLaser)
            _animBinder?.SetChannel(true); // mulai channel; matikan saat mouse up
        else if (set.type == WeaponAnimType.Bow)
            _animBinder?.PlayShoot();  // pakai trigger Shoot

        // untuk laser channel, hentikan saat input dilepas:
        // di Update():
        // if (set.type == ChannelLaser && Input.GetMouseButtonUp(0)) _animBinder.SetChannel(false);
    }


    void StartDodge()
    {
        if (dodgeRoutine != null)
            StopCoroutine(dodgeRoutine);

        dodgeRoutine = StartCoroutine(Dodge());
    }

    IEnumerator Dodge()
    {
        if (stats != null && !stats.TrySpendStamina(dodgeStaminaCost))
        {
            dodgeRoutine = null;
            yield break;
        }

        isDodging = true;
        animator.SetTrigger("Dodge");

        float startTime = Time.time;
        Vector3 dodgeDir = transform.forward;
        stats?.health?.SetInvulnerable(true);
        CombatEventManager.RaiseDodgeAttempt();
        while (Time.time < startTime + dashDuration)
        {
            dodgeDir = transform.forward;
            controller.Move(dodgeDir * dashSpeed * speedMultiplier * Time.deltaTime);
            yield return null;
        }

        isDodging = false;
        stats?.health?.SetInvulnerable(false);
        dodgeRoutine = null;
    }

    void HandleBowInput()
    {
        // mulai aim saat mouse down
        if (Input.GetMouseButtonDown(0) && !isDodging && !isBowAiming)
        {
            BeginBowAim();
        }

        // kalau mau nanti ditambah charge logic, bisa pakai Input.GetMouseButton(0) di sini

        // lepas -> tembak
        if (Input.GetMouseButtonUp(0) && isBowAiming)
        {
            ReleaseBowShot();
        }
    }

    void BeginBowAim()
    {
        isBowAiming = true;

        // stop sprint & combat lock biar gak ganggu aim
        CancelCombatLock();
        isSprinting = false;
        shiftHeld = false;
        dashTriggered = false;

        // kasih tau animator + UI crosshair
        _animBinder?.SetAim(true); // kita bikin fungsi ini di binder
        if (bowCrosshair != null)
            bowCrosshair.SetActive(true);
    }

    void ReleaseBowShot()
    {
        ExitBowAim();

        // baru beneran pake weapon (cek mana, cooldown, dll)
        // if (!playerCombat.IsAttacking)
        // {
        //     bool success = playerCombat.TryUseWeapon();
        //     if (success)
        //     {
        //         PerformAttack(); // ini bakal mainin anim bow (Attack / Shoot) sesuai animSet
        //     }
        // }
    }

    void ExitBowAim()
    {
        isBowAiming = false;
        _animBinder?.SetAim(false);
        if (bowCrosshair != null)
            bowCrosshair.SetActive(false);
    }

    
    void StartCombatLock(Transform t, float extraSeconds = 0f) {
        if (t == null) return;
        _combatTarget = t;
        _combatLockUntil = Mathf.Max(_combatLockUntil, Time.time + combatLockSeconds + extraSeconds);
    }

    void CancelCombatLock() {
        _combatTarget = null;
        _combatLockUntil = -1f;
    }

    Transform FindNearestEnemy(float radius) {
        // pakai OverlapSphere, bisa pakai enemyMask kalau di-set
        Collider[] hits = (enemyMask.value != 0)
            ? Physics.OverlapSphere(transform.position, radius, enemyMask)
            : Physics.OverlapSphere(transform.position, radius);

        Transform best = null;
        float bestDist = Mathf.Infinity;
        foreach (var h in hits) {
            if (!h.CompareTag("Enemy")) continue; // konsisten dgn EnemyAI :contentReference[oaicite:4]{index=4}
            float d = Vector3.Distance(transform.position, h.transform.position);
            if (d < bestDist) { bestDist = d; best = h.transform; }
        }
        return best;
    }

    void FaceTarget(Transform t, float rotSpeedMul = 1f) {
        if (!t) return;
        Vector3 dir = t.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        var targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed * rotSpeedMul);
    }

    public void ApplyStagger(StaggerInfo info)
    {
        if (!isActiveAndEnabled || info.duration <= 0f)
            return;

        CancelActionsForStagger();
        if (staggerRoutine != null)
            StopCoroutine(staggerRoutine);

        staggerRoutine = StartCoroutine(StaggerRoutine(info));
    }

    private void CancelActionsForStagger()
    {
        if (dodgeRoutine != null)
        {
            StopCoroutine(dodgeRoutine);
            dodgeRoutine = null;
        }

        isDodging = false;
        isSprinting = false;
        shiftHeld = false;
        dashTriggered = false;
        isStaggered = false;

        stats?.health?.SetInvulnerable(false);
        playerCombat?.ForceCancelAttack();
        // skillManager?.ForceCancelAllSkills();
        CancelCombatLock();
        ExitBowAim();
        _animBinder?.SetChannel(false);

        if (animator != null)
        {
            animator.SetFloat("WalkSpeed", 0f);
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Shoot");
            animator.ResetTrigger("Dodge");
            animator.ResetTrigger("Jump");
            if (_animBinder != null && _animBinder.currentAnimSet != null)
            {
                animator.ResetTrigger(_animBinder.currentAnimSet.attackTrigger);
                animator.ResetTrigger(_animBinder.currentAnimSet.shootTrigger);
            }
        }
    }

    private IEnumerator StaggerRoutine(StaggerInfo info)
    {
        isStaggered = true;
        if (animator != null && !string.IsNullOrEmpty(staggerTrigger))
            animator.SetTrigger(staggerTrigger);

        float elapsed = 0f;
        float knockbackDuration = info.causesKnockback && info.knockbackDistance > 0f
            ? Mathf.Min(0.15f, info.duration)
            : 0f;
        Vector3 knockbackDirection = info.ResolveKnockbackDirection(transform);
        float knockbackSpeed = knockbackDuration > 0f
            ? info.knockbackDistance / knockbackDuration
            : 0f;

        while (elapsed < info.duration)
        {
            float dt = Time.deltaTime;
            if (knockbackDuration > 0f && elapsed < knockbackDuration)
            {
                controller?.Move(knockbackDirection * knockbackSpeed * dt);
            }

            elapsed += dt;
            yield return null;
        }

        isStaggered = false;
        staggerRoutine = null;
    }

    public void ResetAllStates()
    {
        if (staggerRoutine != null)
        {
            StopCoroutine(staggerRoutine);
            staggerRoutine = null;
        }
        if (dodgeRoutine != null)
        {
            StopCoroutine(dodgeRoutine);
            dodgeRoutine = null;
        }
        isStaggered = false;
        isBowAiming = false;
        isDodging = false;
        isSprinting = false;
        shiftHeld = false;
        dashTriggered = false;
        CancelCombatLock();
        ExitBowAim();
        _animBinder?.SetChannel(false);
        playerCombat?.ForceCancelAttack();
        skillManager?.ForceCancelAllSkills();
        stats?.health?.SetInvulnerable(false);
    }

    bool HasBowEquipped
    {
        get
        {
            var set = _animBinder?.currentAnimSet;
            return set != null && set.type == WeaponAnimType.Bow;
        }
    }
}

