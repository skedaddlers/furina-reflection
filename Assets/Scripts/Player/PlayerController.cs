using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.3f;
    public float holdThreshold = 0.25f; // waktu untuk bedain tap vs hold (dalam detik)
    public Transform cameraTransform;
    public GameObject sword; // reference ke objek sword
    public float attackCooldown = 0.5f; // cooldown antar serangan (dalam detik)

    private Animator animator;
    private PlayerCombat playerCombat;
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    [SerializeField]
    private bool isAttacking;
    private bool isDodging = false;
    private bool isSprinting = false;

    private float shiftPressedTime;
    private bool shiftHeld = false;
    private bool dashTriggered = false;
    
    private float lastAttackTime = 0f; // waktu terakhir attack

    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        playerCombat = GetComponent<PlayerCombat>();
        
        // Sword selalu aktif (atau bisa diatur sesuai kebutuhan)
        if (sword != null)
            sword.SetActive(true);
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
            shiftPressedTime = Time.time;
            shiftHeld = true;
            StartCoroutine(Dodge()); // langsung dodge dulu
            dashTriggered = true;
            isAttacking = false; // Cancel attack jika dodge
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

        if (shiftHeld && (Time.time - shiftPressedTime) >= holdThreshold && !isDodging && isGrounded)
        {
            isSprinting = true;
            dashTriggered = true; // supaya dash gak double
        }

        // Movement
        float speed = isSprinting ? runSpeed : walkSpeed;

        if (inputDir.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            Vector3 moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * rotationSpeed);

            controller.Move(moveDir * speed * Time.deltaTime);

            animator.SetFloat("WalkSpeed", isSprinting ? 1f : 0.5f, 0.1f, Time.deltaTime);
        }
        else
        {
            animator.SetFloat("WalkSpeed", 0f, 0.1f, Time.deltaTime);
        }

        // Lompat
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            isAttacking = false; // Cancel attack jika lompat
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
        }

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Attack dengan cooldown
        if (Input.GetMouseButtonDown(0) && !isAttacking && !isDodging)
        {
            // Cek apakah cooldown sudah selesai
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                FaceNearestEnemy();
                PerformAttack();
            }
        }
    }
    
    void FaceNearestEnemy()
    {
        float detectionRadius = 10f; // jangkauan pencarian musuh, akan di replace dengan weapon range jika ada
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        Transform nearestEnemy = null;
        float nearestDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestEnemy = hit.transform;
                }
            }
        }

        if (nearestEnemy != null)
        {
            Vector3 direction = (nearestEnemy.position - transform.position).normalized;
            direction.y = 0; // tetap di bidang horizontal
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * 2 * Time.deltaTime);
        }
    }

    
    void PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        animator.SetTrigger("Attack");
        
        // Aktifkan sword jika belum aktif
        if (sword != null && !sword.activeSelf)
            sword.SetActive(true);
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

    // Dipanggil dari Animation Event di awal animasi attack (optional)
    public void StartAttack()
    {
        // Bisa digunakan untuk efek atau sound
        if (sword != null)
            sword.SetActive(true);
    }

    // Dipanggil dari Animation Event di akhir animasi attack
    public void EndAttack()
    {
        isAttacking = false;
    }

    // Dipanggil dari Animation Event saat frame hit dari animasi attack
    public void TriggerAttackHit()
    {
        if (playerCombat != null)
            playerCombat.UseWeapon();
    }
}