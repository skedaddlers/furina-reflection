using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f; 
    public float jumpForce = 5f; // Kekuatan lompat
    private Rigidbody rb;
    private bool isGrounded; // Cek apakah player di tanah

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Input lompat (Space)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false; // Supaya tidak lompat terus-terusan di udara
        }
    }

    void FixedUpdate()
    {
        // Gerakan player
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical);
        Vector3 move = direction * speed;

        rb.MovePosition(transform.position + move * Time.fixedDeltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Kalau menyentuh tanah, aktifkan lompat lagi
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
}
