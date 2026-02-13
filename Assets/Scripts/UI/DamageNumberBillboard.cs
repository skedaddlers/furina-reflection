using UnityEngine;

public class DamageNumberBillboard : MonoBehaviour
{
    public Transform cameraTransform;
    public float rollDegrees = 0f;
    public float destroyAfterSeconds = 1f;
    public Vector3 RandomizeIntensity = new Vector3(0.5f, 0, 0);

    void Awake()
    {
        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }
    }

    void Start()
    {
        Destroy(gameObject, destroyAfterSeconds);

        transform.position += new Vector3(
            Random.Range(-RandomizeIntensity.x, RandomizeIntensity.x),
            Random.Range(-RandomizeIntensity.y, RandomizeIntensity.y),
            Random.Range(-RandomizeIntensity.z, RandomizeIntensity.z));
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 dir = cameraTransform.position - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(-dir);
        if (rollDegrees != 0f)
        {
            look *= Quaternion.Euler(0f, 0f, -rollDegrees);
        }
        transform.rotation = look;
    }
}
