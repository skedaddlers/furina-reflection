using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorTrigger : MonoBehaviour
{
    [Tooltip("Indeks arah pintu pada urutan [forward, right, back, left]")]
    public int directionIndex;

    public Room parentRoom;

    void Start()
    {

        // Pastikan collider-nya trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Panggil perpindahan room
            UIManager.Instance.ShowGoToNextRoomButton();
            parentRoom.GoToNeighbor(directionIndex);
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
