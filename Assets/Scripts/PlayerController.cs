using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    public Transform cameraTransform;
    public GameObject sword; // reference ke objek sword
    public float timePassedToSheathe = 2f; // waktu untuk sheathe sword
    private float timeSinceLastAttack = 0f;

    private Animator animator;
    private PlayerCombat playerCombat;
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isAttacking;

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

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float speed = isRunning ? runSpeed : walkSpeed;

        if (inputDir.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            Vector3 moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);

            controller.Move(moveDir * speed * Time.deltaTime);

            animator.SetFloat("WalkSpeed", isRunning ? 1f : 0.5f, 0.1f, Time.deltaTime);
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

        // Dodge
        if (Input.GetKeyDown(KeyCode.LeftControl) && isGrounded && !isAttacking)
        {
            animator.SetTrigger("Dodge");
            Vector3 dodgeDir = transform.forward;
            controller.Move(dodgeDir * runSpeed * 1.5f * Time.deltaTime);
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
