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
    public float timePassedToSheathe = 2f; // waktu untuk sheathe sword
    private float timeSinceLastAttack = 0f;

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

    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        playerCombat = GetComponent<PlayerCombat>();
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
        }

        if (shiftHeld && (Time.time - shiftPressedTime) >= holdThreshold && !isDodging && isGrounded)
        {
            isSprinting = true;
            dashTriggered = true; // supaya dash gak double
        }

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


        if (Input.GetMouseButtonDown(0) && !isAttacking) // Hanya bisa menyerang jika tidak sedang menyerang
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            // Hanya izinkan serangan jika tidak sedang dalam animasi sheathe
            if (!stateInfo.IsName("Seathing Sword") && !stateInfo.IsName("Slash")) // Tambahkan IsTag("Attack") jika ada transisi antar serangan
            {
                isAttacking = true;
                animator.SetTrigger("Attack");
                // StartAttack();
                // TriggerAttackHit();
            }
        }

        if (!isAttacking && sword.activeSelf)
        {
            timeSinceLastAttack += Time.deltaTime;
            if (timeSinceLastAttack >= timePassedToSheathe)
            {
                animator.SetTrigger("Unequip");
                timeSinceLastAttack = 0f; // reset timer setelah mulai unequip
            }
        }

        // deactivate setelah animasi unequip selesai
        AnimatorStateInfo unequipStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (unequipStateInfo.IsName("Seathing Sword") && unequipStateInfo.normalizedTime >= 0.5f)
        {
            sword.SetActive(false);
            timeSinceLastAttack = 0f; // reset timer setelah animasi unequip selesai
        }
    }
    
    IEnumerator Dodge()
    {
        // isDodging = true;
        // animator.SetFloat("WalkSpeed", 1f, 0.1f, Time.deltaTime);

        // float startTime = Time.time;
        // Vector3 dodgeDir = transform.forward;

        // while (Time.time < startTime + dashDuration)
        // {
        //     controller.Move(dodgeDir * dashSpeed * Time.deltaTime);
        //     yield return null; // tunggu frame berikutnya
        // }

        // isDodging = false;
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

    public void StartAttack()
    {
        sword.SetActive(true);
    }

    public void EndAttack()
    {
        isAttacking = false;
        timeSinceLastAttack = 0f; 
    }

    public void TriggerAttackHit()
    {
        playerCombat.CheckHit();
    }
    
}
