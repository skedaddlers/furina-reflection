using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorTrigger : MonoBehaviour
{
    [Tooltip("Indeks arah pintu pada urutan [forward, right, back, left]")]
    public int directionIndex;

    public Room parentRoom;
    private bool isPlayerNearby = false;
    
    [SerializeField]
    private bool isLocked = false;

    void Start()
    {

        // Pastikan collider-nya trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Update()
    {
        if (isLocked)
            return;
        // Kalau player di area & menekan F → pindah ke room tetangga
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.F))
        {
            parentRoom.OnDoorInteract(directionIndex);
            isPlayerNearby = false;
            UIManager.Instance.ShowInterractionUI(false, "");
        }
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            string txt = isLocked ? "Door is Locked" : "Press <b>F</b> to Enter";
            UIManager.Instance.ShowInterractionUI(true, txt);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            UIManager.Instance.ShowInterractionUI(false, "");
        }
    }

#if UNITY_EDITOR
    // Tambahan visualisasi kecil di editor
    void OnDrawGizmos()
    {
        Gizmos.color = isLocked ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(1, 2, 0.2f));
    }
#endif
}
