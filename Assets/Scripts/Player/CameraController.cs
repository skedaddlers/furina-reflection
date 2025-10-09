using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;           
    public float distance = 5f;        
    public float height = 2f;          
    public float rotationSpeed = 3f;   
    public float zoomSpeed = 2f;       
    public float minDistance = 2f;     
    public float maxDistance = 8f;     
    public Transform player;           

    private float yaw;
    private float pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        // Zoom pakai scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // Kamera bebas rotasi pakai mouse
        yaw += Input.GetAxis("Mouse X") * rotationSpeed;
        pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
        pitch = Mathf.Clamp(pitch, -20f, 60f);

        // Reset kamera ke belakang player kalau klik middle mouse
        if (Input.GetMouseButton(2))
        {
            float targetYaw = player.eulerAngles.y;
            yaw = Mathf.LerpAngle(yaw, targetYaw, Time.deltaTime * 5f);
        }

        // Hitung posisi kamera
        Vector3 offset = new Vector3(0, height, -distance);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPosition = target.position + rotation * offset;

        transform.position = desiredPosition;
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
