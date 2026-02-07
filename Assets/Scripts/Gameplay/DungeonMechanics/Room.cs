using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Room : MonoBehaviour
{
    public RoomType roomType;
    public int roomIndex;

    [Header("Difficulty / Progression Info")]
    public int distanceFromStart;
    public float difficultyWeight = 1f;

    [Header("Room Structure")]
    public SpawnTrigger spawnTrigger = null;
    public EventTrigger eventTrigger = null;

    [Tooltip("Order: [forward, right, back, left]")]
    public int[] roomNeighbors = new int[4];

    [Tooltip("Order: [forward, right, back, left]")]
    public GameObject[] doors = new GameObject[4];

    [Header("Enemy Spawn Settings")]
    public int waveCount = 1;
    public int timeBetweenWaves = 5;
    public int maxEnemies = 3;

    [Tooltip("This will be overridden by difficulty system")]
    public int enemyCount = 3;

    public Vector3 spawnAreaSize = new Vector3(10f, 5f, 10f);
    public LayerMask groundMask;
    public LayerMask obstacleMask;
    public float minSpawnDistance = 2f;
    public GameObject enemyPrefab;

    [Header("Runtime Info")]
    public bool isCleared = false;
    public bool isInCombat = false;
    public int currentWave = 1;

    public List<GameObject> spawnedEnemies = new List<GameObject>();

    public static System.Action<Room> OnRoomCleared;
    public static System.Action<Room> OnWaveCleared;
    public static System.Action<Room> OnRoomCombatStarted;

    private bool isLocked = false;
    private int lastEnterFrom = -1;

    public Vector3 playerSpawn = Vector3.zero;

    #region Initialization

    public void Initialize(RoomData data)
    {
        roomIndex = data.id;
        roomNeighbors = data.neighbors;
        roomType = data.roomType;

        distanceFromStart = data.distanceFromStart;
        difficultyWeight = data.difficultyWeight;

        SetupDoors();
        SetupTriggers();
    }

    private void SetupDoors()
    {
        for (int i = 0; i < doors.Length; i++)
        {
            bool hasNeighbor = roomNeighbors[i] != 0;

            if (doors[i] == null) continue;

            doors[i].SetActive(hasNeighbor);

            if (!hasNeighbor) continue;

            var trigger = doors[i].GetComponent<DoorTrigger>();
            if (trigger != null)
            {
                trigger.parentRoom = this;
                trigger.directionIndex = i;
            }
        }
    }

    private void SetupTriggers()
    {
        if (spawnTrigger != null)
            spawnTrigger.parentRoom = this;

        if (eventTrigger != null)
            eventTrigger.parentRoom = this;
    }

    #endregion

    #region Player Interaction

    public void OnPlayerEnter(int fromDirection)
    {
        lastEnterFrom = fromDirection;

        if (IsSafeRoom() || isCleared)
        {
            UnlockAllDoors();
            DisableTriggers();
            return;
        }

        LockAllDoors();
        UnlockDoor(fromDirection);
        EnableSpawnTrigger();
    }

    public void OnDoorInteract(int direction)
    {
        if (isLocked && direction != lastEnterFrom)
        {
            Debug.Log("Room is locked! Cannot exit through this door.");
            return;
        }

        int nextRoomID = roomNeighbors[direction];
        if (nextRoomID == 0) return;

        GameManager.Instance.roomManager.MovePlayerToRoom(nextRoomID, GetOpposite(direction));
    }

    public void GoToNeighbor(int direction)
    {
        int nextRoomID = roomNeighbors[direction];
        if (nextRoomID == 0) return;

        GameManager.Instance.roomManager.MovePlayerToRoom(nextRoomID, GetOpposite(direction));
    }

    private bool IsSafeRoom()
    {
        return roomType == RoomType.Start || roomType == RoomType.Shop;
    }

    private void DisableTriggers()
    {
        if (spawnTrigger != null) spawnTrigger.gameObject.SetActive(false);
        if (eventTrigger != null) eventTrigger.gameObject.SetActive(false);
    }

    private void EnableSpawnTrigger()
    {
        if (spawnTrigger != null) spawnTrigger.gameObject.SetActive(true);
    }

    #endregion

    #region Combat

    public void BeginCombat()
    {
        if (isCleared || isInCombat) return;

        isInCombat = true;

        OnRoomCombatStarted?.Invoke(this);

        ApplyDifficultySnapshots();

        if (roomType == RoomType.Elite)
            enemyCount = 1;

        Debug.Log($"Room {roomIndex} beginning combat with {enemyCount} enemies.");

        SpawnEnemiesInRoom();
        LockAllDoors();
    }

    private void ApplyDifficultySnapshots()
    {
        var diff = GlobalDifficultyState.Instance;

        if (diff != null)
            enemyCount = diff.GetEnemyCountForRoom(this);

        enemyCount = Mathf.Clamp(enemyCount, 1, maxEnemies);
    }

    private void HandleEnemyDeath(GameObject enemy)
    {
        spawnedEnemies.Remove(enemy);

        if (spawnedEnemies.Count > 0) 
            return;

        if (currentWave >= waveCount)
        {
            ClearRoom();
        }
        else
        {
            StartNextWave();
        }

        var minimap = FindObjectOfType<MinimapUI>();
        if (minimap != null)
            minimap.VisitRoom(roomIndex);
    }

    private void ClearRoom()
    {
        isCleared = true;
        isInCombat = false;

        UnlockAllDoors();

        Debug.Log($"Room {roomIndex} Cleared!");
        OnRoomCleared?.Invoke(this);

        if (roomType == RoomType.Boss)
            GameManager.Instance.OnBossRoomCleared();
    }

    private void StartNextWave()
    {
        currentWave++;
        Debug.Log($"Room {roomIndex} Wave {currentWave} cleared. Next wave in {timeBetweenWaves} seconds.");

        OnWaveCleared?.Invoke(this);
        StartCoroutine(SpawnNextWaveAfterDelay());
    }

    private IEnumerator SpawnNextWaveAfterDelay()
    {
        yield return new WaitForSeconds(timeBetweenWaves);
        SpawnEnemiesInRoom();
    }

    #endregion

    #region Enemy Spawning

    public void SpawnEnemiesInRoom()
    {
        Debug.Log($"Spawning enemies in Room {roomIndex}");

        ClearExistingEnemies();

        List<GameObject> enemiesToSpawn = GetEnemiesForRoom();
        if (enemiesToSpawn.Count == 0)
        {
            Debug.LogWarning($"Room {roomIndex} has no enemy prefabs available to spawn.");
            return;
        }

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = enemiesToSpawn.Count * 10;

        while (spawned < enemiesToSpawn.Count && attempts < maxAttempts)
        {
            attempts++;

            if (!TryGetSpawnPosition(out Vector3 spawnPos))
                continue;

            if (!IsSpawnPositionValid(spawnPos))
                continue;

            SpawnEnemy(enemiesToSpawn[spawned], spawnPos);
            spawned++;
        }

        Debug.Log($"Spawned {spawned} enemies in Room {roomIndex}");
    }

    private bool TryGetSpawnPosition(out Vector3 spawnPos)
    {
        spawnPos = Vector3.zero;

        Vector3 randomPos = transform.position + new Vector3(
            Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
            spawnAreaSize.y / 2f,
            Random.Range(-spawnAreaSize.z / 2f, spawnAreaSize.z / 2f)
        );

        Debug.DrawRay(randomPos, Vector3.down * (spawnAreaSize.y + 1f), Color.red, 2f);

        if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, spawnAreaSize.y, groundMask))
        {
            spawnPos = hit.point + Vector3.up * 0.1f;
            return true;
        }

        return false;
    }

    private bool IsSpawnPositionValid(Vector3 spawnPos)
    {
        bool blocked = Physics.CheckSphere(spawnPos, 0.5f, obstacleMask);
        if (blocked) return false;

        foreach (var enemy in spawnedEnemies)
        {
            if (enemy == null) continue;

            if (Vector3.Distance(spawnPos, enemy.transform.position) < minSpawnDistance)
                return false;
        }

        return true;
    }

    private void SpawnEnemy(GameObject prefab, Vector3 spawnPos)
    {
        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
        spawnedEnemies.Add(enemy);

        var health = enemy.GetComponent<Health>();
        if (health != null)
        {
            health.onDeath += () => HandleEnemyDeath(enemy);
        }
    }

    private void ClearExistingEnemies()
    {
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        spawnedEnemies.Clear();
    }

    private List<GameObject> GetEnemiesForRoom()
    {
        var library = Library.Instance;
        var result = new List<GameObject>();

        if (library != null)
        {
            List<GameObject> pool =
                roomType == RoomType.Elite ? library.eliteEnemies : library.commonEnemies;

            result = Helpers.GetRandomItemsAllowRepeats(pool, enemyCount, roomIndex);
        }

        if (result.Count == 0)
        {
            Debug.LogWarning($"Fallback enemy selection for Room {roomIndex}. Please populate {(roomType == RoomType.Elite ? "elite" : "common")} enemies in GlobalLibrary.");

            if (enemyPrefab != null)
            {
                for (int i = 0; i < enemyCount; i++)
                    result.Add(enemyPrefab);
            }
        }

        return result;
    }

    #endregion

    #region Doors

    public void LockAllDoors()
    {
        isLocked = true;

        foreach (var door in doors)
        {
            if (door == null) continue;

            var trig = door.GetComponent<DoorTrigger>();
            if (trig != null)
                trig.SetLocked(true);
        }
    }

    public void UnlockAllDoors()
    {
        isLocked = false;

        foreach (var door in doors)
        {
            if (door == null) continue;

            var trig = door.GetComponent<DoorTrigger>();
            if (trig != null)
                trig.SetLocked(false);
        }
    }

    private void UnlockDoor(int dir)
    {
        if (dir < 0 || dir > 3) return;
        if (doors[dir] == null) return;
        if (roomNeighbors[dir] == 0) return;

        var trig = doors[dir].GetComponent<DoorTrigger>();
        if (trig != null)
            trig.SetLocked(false);
    }

    private int GetOpposite(int dir)
    {
        return dir switch
        {
            0 => 2,
            1 => 3,
            2 => 0,
            3 => 1,
            _ => -1
        };
    }

    #endregion

    #region Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawCube(transform.position, spawnAreaSize);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, spawnAreaSize);
    }
#endif

    #endregion
}