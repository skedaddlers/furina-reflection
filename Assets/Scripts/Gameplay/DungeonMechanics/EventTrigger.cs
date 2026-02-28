using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EventTrigger : MonoBehaviour
{
    public Room parentRoom;
    private bool isPlayerNearby = false;
    private Vector3 initialPosition;

    void Start()
    {
        // Pastikan collider-nya trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
        initialPosition = transform.position;
    }

    void Update()
    {
        // rotate y
        transform.Rotate(Vector3.up, 30f * Time.deltaTime);
        float bobbing = Mathf.Sin(Time.time * 2f) * 0.05f; // bobbing effect
        transform.position = initialPosition + Vector3.up * bobbing;
        // Kalau player di area & menekan F → pindah ke room tetangga
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.F))
        {
            isPlayerNearby = false;
            this.gameObject.SetActive(false);
            if(parentRoom.roomType == RoomType.Event)
            {
                parentRoom.GetComponent<EventRoomManager>()?.StartEventRoom();
            }
            UIManager.Instance.ShowInterractionUI(false, "");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            string txt = "Press <b>F</b> to start event";
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
