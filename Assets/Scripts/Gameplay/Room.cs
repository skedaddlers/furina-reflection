using UnityEngine;
using System.Collections.Generic;

public class Room : MonoBehaviour
{
    public RoomType roomType;
    public int roomIndex;
    public SpawnTrigger spawnTrigger = null;
    public int[] roomNeighbors = new int[4]; // [forward, right, back, left]
    public GameObject[] doors = new GameObject[4]; // urutan sama

    [Header("Enemy Spawn Settings")]
    public int enemyCount = 3;
    public Vector3 spawnAreaSize = new Vector3(10f, 5f, 10f); // area spawn di sekitar room
    public LayerMask groundMask;
    public LayerMask obstacleMask;
    public float minSpawnDistance = 2f;
    public GameObject enemyPrefab;
    public List<GameObject> spawnedEnemies = new List<GameObject>();
    
    public Vector3 playerSpawn = new Vector3(0, 0, 0);

    public void Initialize(RoomData data)
    {
        roomIndex = data.id;
        roomNeighbors = data.neighbors;
        roomType = data.roomType;

        // Set doors active/inactive based on neighbors
        for (int i = 0; i < 4; i++)
        {
            if (roomNeighbors[i] != 0)
            {
                doors[i].SetActive(true);
                doors[i].GetComponent<DoorTrigger>().parentRoom = this;
                doors[i].GetComponent<DoorTrigger>().directionIndex = i;
                if (spawnTrigger != null)
                    spawnTrigger.parentRoom = this;

            }
            else
                doors[i].SetActive(false);
        }
    }

    public void SpawnEnemiesInRoom()
    {
        Debug.Log($"Spawning enemies in Room {roomIndex}");
        ClearExistingEnemies();

        int attempts = 0;
        int spawned = 0;
        int maxAttempts = enemyCount * 10; // pembatas biar ga infinite loop

        while (spawned < enemyCount && attempts < maxAttempts)
        {
            attempts++;

            // Tentukan posisi random dalam area room
            Vector3 randomPos = transform.position + new Vector3(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                spawnAreaSize.y / 2, // mulai di atas
                Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
            );

            Debug.DrawRay(randomPos, Vector3.down * (spawnAreaSize.y + 1), Color.red, 2f);
            // Raycast ke bawah untuk cari permukaan lantai
            if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, spawnAreaSize.y, groundMask))
            {
                Vector3 spawnPos = hit.point + Vector3.up * 0.1f; // sedikit di atas lantai

                // Cek apakah area sekitar spawnPos kosong (tidak kena obstacle)
                bool blocked = Physics.CheckSphere(spawnPos, 0.5f, obstacleMask);
                bool tooClose = false;

                foreach (var e in spawnedEnemies)
                {
                    if (Vector3.Distance(spawnPos, e.transform.position) < minSpawnDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!blocked && !tooClose)
                {
                    GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, transform);
                    spawnedEnemies.Add(enemy);
                    spawned++;
                }
            }
        }

        Debug.Log($"Spawned {spawned} enemies in Room {roomIndex}");
    }

    void ClearExistingEnemies()
    {
        foreach (var e in spawnedEnemies)
        {
            if (e != null) Destroy(e);
        }
        spawnedEnemies.Clear();
    }

    public void OnPlayerEnter(int fromDirection)
    {
        Debug.Log($"Player entered Room {roomIndex} from {fromDirection}");
        if (roomNeighbors[fromDirection] != 0)
            doors[fromDirection].SetActive(true);
    }

    public void GoToNeighbor(int direction)
    {
        int nextRoomID = roomNeighbors[direction];
        if (nextRoomID == 0) return;
        GameManager.Instance.roomManager.MovePlayerToRoom(nextRoomID, GetOpposite(direction));
    }

    int GetOpposite(int dir)
    {
        // forward↔back, right↔left
        return dir switch
        {
            0 => 2,
            1 => 3,
            2 => 0,
            3 => 1,
            _ => -1
        };
    }

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Warna transparan untuk area spawn
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawCube(transform.position, spawnAreaSize);

        // Garis outline supaya jelas batasnya
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, spawnAreaSize);
    }
    #endif

}