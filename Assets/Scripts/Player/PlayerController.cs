using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
public class PlayerController : MonoBehaviour
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
    public float holdThreshold = 0.25f; // waktu untuk bedain tap vs hold (dalam detik)
    public float speedMultiplier = 1f;
    public Transform cameraTransform;
    // === Combat lock ===
    [Header("Combat Lock / Auto Aim")]
    public float autoAimRadius = 12f;            // radius cari musuh
    public float combatLockSeconds = 2.0f;       // durasi auto-face setelah serang
    public float breakDistance = 15f;            // jarak putus lock
    public LayerMask enemyMask;                  // optional: filter physics
    
    [Header("Bow Aim Settings")]
    public GameObject bowCrosshair;      // assign di inspector
    public float bowAimFOV = 40f;        // FOV saat aim
    public float bowFovLerpSpeed = 15f;  // seberapa cepat lerp FOV
    public GameObject cameraTarget;

    private bool isBowAiming = false;
    [SerializeField]
    private CinemachineCamera mainCam;
    private float defaultFOV;
    private float _combatLockUntil = -1f;
    private Transform _combatTarget;
    private bool InCombatLock => Time.time < _combatLockUntil && _combatTarget != null;


    private Animator animator;
    private PlayerCombat playerCombat;
    private CharacterController controller;
    [SerializeField]
    private PlayerAnimationBinder _animBinder;
    private Vector3 velocity;
    private bool isGrounded;
    [SerializeField]
    private bool isDodging = false;
    private bool isSprinting = false;

    private float shiftPressedTime;
    private bool shiftHeld = false;
    private bool dashTriggered = false;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<CharacterController>();
        playerCombat = GetComponent<PlayerCombat>();
        skillManager = GetComponent<SkillManager>();
        _animBinder = GetComponent<PlayerAnimationBinder>();
        // find main camera with tag CinemachineCamera
        mainCam = GameObject.FindGameObjectWithTag("CinemachineCamera").GetComponent<CinemachineCamera>();
        if (mainCam != null)
        {
            defaultFOV = mainCam.Lens.FieldOfView;
        }
    }

    void Update()
    {
        if (GameManager.Instance.IsPaused) 
        {
            if (isBowAiming)
            {
                isBowAiming = false;
                _animBinder?.SetAim(false);
                if (bowCrosshair != null)
                    bowCrosshair.SetActive(false);
            }
            return;
        }

        // Cek apakah player nyentuh tanah
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;

        // Handle Dodge & Sprint
        if (Input.GetKeyDown(KeyCode.LeftShift) && isGrounded && !isDodging)
        {
            CancelCombatLock();
            shiftPressedTime = Time.time;
            shiftHeld = true;
            StartCoroutine(Dodge()); // langsung dodge dulu
            dashTriggered = true;
            playerCombat.ForceCancelAttack(); 
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            shiftHeld = false;
            if (!dashTriggered && (Time.time - shiftPressedTime) < holdThreshold && !isDodging && isGrounded)
            {
                // Tap -> dash
                StartCoroutine(Dodge());
            }
            else
            {
                // Lepas sprint
                isSprinting = false;
            }
            dashTriggered = false; // Reset untuk input berikutnya
        }

        bool wantsSprintNow = shiftHeld && (Time.time - shiftPressedTime) >= holdThreshold && !isDodging && isGrounded;
        if (wantsSprintNow)
        {
            isSprinting = true;
            dashTriggered = true;
            CancelCombatLock(); // sprint membatalkan auto-face
        }
        
        if (!isSprinting) {
            // jaga target selama window aktif
            if (InCombatLock) {
                // putus kalau terlalu jauh / target lenyap
                if (!_combatTarget || Vector3.Distance(transform.position, _combatTarget.position) > breakDistance) {
                    _combatTarget = FindNearestEnemy(autoAimRadius);
                    if (_combatTarget == null) CancelCombatLock();
                }
            } else {
                // tidak dalam window, tapi kalau ada musuh sangat dekat, boleh reacquire halus
                var nearby = FindNearestEnemy(autoAimRadius * 0.7f);
                if (nearby) { _combatTarget = nearby; } // tanpa memperpanjang waktu; hanya quality-of-life facing
            }
        }

        // Movement
        float speed = (isSprinting ? runSpeed : walkSpeed) * speedMultiplier;

        if (inputDir.magnitude >= 0.1f)
        {
            if (isBowAiming)
            {
                // --- Rotate whole upper body (no need to edit animation) ---
                Vector3 camFwd = cameraTransform.forward;
                camFwd.y = 0;
                camFwd.Normalize();

                // --- Strafe movement style ---
                Vector3 camRight = cameraTransform.right;
                camRight.y = 0;

                Quaternion lookRot = Quaternion.LookRotation(camRight);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    lookRot,
                    Time.deltaTime * rotationSpeed * 2f // lebih cepat biar aim responsif
                );
                Vector3 moveDir =
                    camFwd * Input.GetAxis("Vertical") +
                    camRight * Input.GetAxis("Horizontal");

                controller.Move(moveDir.normalized * walkSpeed * speedMultiplier * Time.deltaTime);

                animator.SetFloat("WalkSpeed", moveDir.magnitude > 0 ? 0.5f : 0f);
            }
            else
            {
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
                Vector3 moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

                if (InCombatLock && !isSprinting)
                {
                    FaceTarget(_combatTarget, 1.0f);
                }
                else
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(moveDir),
                        Time.deltaTime * rotationSpeed
                    );
                }

                controller.Move(moveDir * speed * Time.deltaTime);
                animator.SetFloat("WalkSpeed", isSprinting ? 1f : 0.5f, 0.1f, Time.deltaTime);
            }
        }
        else
        {
            if (isBowAiming)
            {
                // diam tapi tetep hadap ke camera
                Vector3 camRight = cameraTransform.right;
                camRight.y = 0f;
                if (camRight.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(camRight);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed * 2f);
                }
            }
            else
            {
                if (InCombatLock && !isSprinting) FaceTarget(_combatTarget, 1.2f);
            }

            animator.SetFloat("WalkSpeed", 0f, 0.1f, Time.deltaTime);
        }

        // Lompat

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (mainCam != null)
        {
            float targetFov = isBowAiming ? bowAimFOV : defaultFOV;
            float sideOffset = isBowAiming ? 0.5f : 0f; // contoh offset saat aim
            cameraTarget.transform.localPosition = Vector3.Lerp(
                cameraTarget.transform.localPosition,
                new Vector3(cameraTarget.transform.localPosition.x, cameraTarget.transform.localPosition.y, sideOffset),
                Time.deltaTime * bowFovLerpSpeed
            );
            mainCam.Lens.FieldOfView = Mathf.Lerp(
                mainCam.Lens.FieldOfView,
                targetFov,
                Time.deltaTime * bowFovLerpSpeed
            );
        }

        // Attack dengan cooldown
        if (!HasBowEquipped)
        {
            // NORMAL weapon (melee / gun / laser, dll)
            if (Input.GetMouseButtonDown(0) && !isDodging)
            {
                if (!playerCombat.IsAttacking)
                {
                    bool success = playerCombat.TryUseWeapon();
                    if (success)
                    {
                        PerformAttack();
                    }
                }
            }
        }
        else
        {
            // BOW: hold to aim, release to shoot
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
            isBowAiming = false;
            playerCombat.loadout.Swap();
            playerCombat.ForceCancelAttack();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            skillManager.TryUseSkill(0);
            playerCombat.ForceCancelAttack();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            skillManager.TryUseSkill(1);
            playerCombat.ForceCancelAttack();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            skillManager.TryUseSkill(2);
            playerCombat.ForceCancelAttack();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            skillManager.TryUseSkill(3);
            playerCombat.ForceCancelAttack();
        }
    }
    

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


    IEnumerator Dodge()
    {
        isDodging = true;
        animator.SetTrigger("Dodge");

        float startTime = Time.time;
        Vector3 dodgeDir = transform.forward;

        while (Time.time < startTime + dashDuration)
        {
            dodgeDir = transform.forward;
            controller.Move(dodgeDir * dashSpeed * speedMultiplier * Time.deltaTime);
            yield return null;
        }

        isDodging = false;
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
        isBowAiming = false;

        _animBinder?.SetAim(false);
        if (bowCrosshair != null)
            bowCrosshair.SetActive(false);

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

    public void ResetAllStates()
    {
        isBowAiming = false;
        isDodging = false;
        isSprinting = false;
        shiftHeld = false;
        dashTriggered = false;
        CancelCombatLock();
        _animBinder?.SetAim(false);
        if (bowCrosshair != null)
            bowCrosshair.SetActive(false);
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