using UnityEngine;
using System.Collections;

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
    public Transform cameraTransform;
    // === Combat lock ===
    [Header("Combat Lock / Auto Aim")]
    public float autoAimRadius = 12f;            // radius cari musuh
    public float combatLockSeconds = 2.0f;       // durasi auto-face setelah serang
    public float breakDistance = 15f;            // jarak putus lock
    public LayerMask enemyMask;                  // optional: filter physics

    private float _combatLockUntil = -1f;
    private Transform _combatTarget;
    private bool InCombatLock => Time.time < _combatLockUntil && _combatTarget != null;


    private Animator animator;
    private PlayerCombat playerCombat;
    private CharacterController controller;
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
    }

    void Update()
    {
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
        float speed = isSprinting ? runSpeed : walkSpeed;

        if (inputDir.magnitude >= 0.1f) {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            Vector3 moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

            // GANTI logika rotasi: kalau lock → hadap musuh; kalau tidak → hadap arah gerak (lama)
            if (InCombatLock && !isSprinting) {
                FaceTarget(_combatTarget, 1.0f); // sedikit lebih cepat dari rotasi biasa kalau mau: 1.2f
            } else {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * rotationSpeed);
            }

            controller.Move(moveDir * speed * Time.deltaTime);
            animator.SetFloat("WalkSpeed", isSprinting ? 1f : 0.5f, 0.1f, Time.deltaTime);
        } else {
            // diam di tempat → kalau lock, tetap hadap target
            if (InCombatLock && !isSprinting) FaceTarget(_combatTarget, 1.2f);
            animator.SetFloat("WalkSpeed", 0f, 0.1f, Time.deltaTime);
        }

        // Lompat

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Attack dengan cooldown
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
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
            playerCombat.ForceCancelAttack();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
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
        if (set == null || set.type == WeaponAnimType.Melee || set.type == WeaponAnimType.Bow || set.type == WeaponAnimType.BombThrow)
            _animBinder?.PlayAttack(); // pakai trigger Attack
        else if (set.type == WeaponAnimType.OneHandGun || set.type == WeaponAnimType.TwoHandGun)
            _animBinder?.PlayShoot();  // pakai trigger Shoot
        else if (set.type == WeaponAnimType.ChannelLaser)
            _animBinder?.SetChannel(true); // mulai channel; matikan saat mouse up

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
            controller.Move(dodgeDir * dashSpeed * Time.deltaTime);
            yield return null;
        }

        isDodging = false;
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
}