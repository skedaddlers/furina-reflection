using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorTrigger : MonoBehaviour
{
    [Tooltip("Indeks arah pintu pada urutan [forward, right, back, left]")]
    public int directionIndex;

    public Room parentRoom;
    private bool isPlayerNearby = false;

    void Start()
    {

        // Pastikan collider-nya trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Update()
    {
        // Kalau player di area & menekan F → pindah ke room tetangga
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.F))
        {
            parentRoom.GoToNeighbor(directionIndex);
            isPlayerNearby = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            string txt = "Press <b>F</b> to Enter";
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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(1, 2, 0.2f));
    }
#endif
}
