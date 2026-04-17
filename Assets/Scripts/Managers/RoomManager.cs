using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RoomManager : MonoBehaviour
{
    public static System.Action<Room> OnRoomEntered;

    [Header("Prefabs & Refs")]
    public GameObject[] roomPrefabs;     // index = (int)RoomType
    public GameObject[] normalRoomPrefabs;
    public Transform player;

    public AudioClip doorOpenSound;

    // runtime
    private Dictionary<int, Room> roomInstances = new();
    private int currentRoomID = 1;

    public DungeonLayout Layout { get; private set; }
    public int CurrentRoomID => currentRoomID;
    public Room CurrentRoom => GetRoomById(currentRoomID);
    private HashSet<int> visitedRooms = new HashSet<int>();

    // ====== PUBLIC API ======
    public void Initialize(DungeonLayout layout)
    {
        Layout = layout;
        InstantiateRooms();

        var diff = GlobalDifficultyState.Instance;
        if (diff != null)
        {
            int progressionRoomCount = Layout != null && Layout.roomDataMap != null
                ? Layout.roomDataMap.Values.Count(room => room.roomType != RoomType.Start && room.roomType != RoomType.Shop)
                : 0;
            diff.SetTotalRooms(progressionRoomCount);
        }
        // Debug.Log($"RoomManager initialized with {roomInstances.Count} rooms.");
        var minimap = FindObjectOfType<MinimapUI>();
        if (minimap != null)
        {
            // Debug.Log("Initializing Minimap UI from RoomManager...");
            minimap.InitializeMinimap(this);   // ← overload baru yang menerima RoomManager
            minimap.SetCurrentRoom(1);
        }
        MovePlayerToRoom(1, -1);
    }

    public void MovePlayerToRoom(int nextRoomID, int fromDoorIndex)
    {
        if (!roomInstances.ContainsKey(nextRoomID)) return;
        if (IsRoomVisited(nextRoomID) == false)
        {
            var diff = GlobalDifficultyState.Instance;
            Room room = roomInstances[nextRoomID];
            int totalItem = diff != null ? diff.GetItemDropCountForRoom(room) : GetBaseItemSpawnCount(room);
            room.SpawnItemsInRoom(totalItem);
            MarkRoomVisited(nextRoomID);
        }
        currentRoomID = nextRoomID;
        Room nextRoom = roomInstances[nextRoomID];

        // Parent first (important so local transforms behave predictably)
        player.SetParent(nextRoom.transform);

        // Default spawn = local spawn point
        Vector3 spawnWorldPos = nextRoom.transform.TransformPoint(nextRoom.playerSpawn);

        // If entered from a door, override spawn
        if (fromDoorIndex >= 0 && fromDoorIndex < nextRoom.doors.Length && nextRoom.doors[fromDoorIndex] != null)
        {
            Transform door = nextRoom.doors[fromDoorIndex].transform;
            Vector3 toCenter = (nextRoom.transform.position - door.position).normalized;

            spawnWorldPos = door.position + toCenter * 1.5f;
            // Debug.Log($"Spawning at door {fromDoorIndex} WORLD position {spawnWorldPos}");
        }

        // Debug.Log($"Before move: Player World Pos {player.position}");
        var cc = player.gameObject.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.position = spawnWorldPos;
        player.rotation = Quaternion.identity;

        // Debug.Log($"After move: Player World Pos {player.position}");

        nextRoom.OnPlayerEnter(fromDoorIndex);
        OnRoomEntered?.Invoke(nextRoom);

        var minimap = FindObjectOfType<MinimapUI>();
        if (minimap != null) minimap.SetCurrentRoom(nextRoomID);
        if (cc != null) cc.enabled = true;
    }

    // ====== INTERNAL ======
    void InstantiateRooms()
    {
        foreach (var data in Layout.roomDataMap.Values)
        {
            GameObject prefab = null;
            if(data.roomType == RoomType.Normal)
            {
                prefab = normalRoomPrefabs[Random.Range(0, normalRoomPrefabs.Length)];
            }
            else
            {
                prefab = roomPrefabs[(int)data.roomType];
            }

            GameObject instance = Instantiate(prefab, data.worldPosition, Quaternion.identity);
            // set name agar gampang dicari di hierarchy
            instance.name = $"Room_{data.id}_{data.roomType}";
            Room room = instance.GetComponent<Room>();
            room.Initialize(data);
            roomInstances[data.id] = room;
        }
    }

    public void MarkRoomVisited(int roomId)
    {
        if (!visitedRooms.Contains(roomId))
        {
            visitedRooms.Add(roomId);
        }
    }

    public bool IsRoomVisited(int roomId)
    {
        return visitedRooms.Contains(roomId);
    }

    public Room GetRoomById(int roomId)
    {
        if (roomInstances.ContainsKey(roomId))
        {
            return roomInstances[roomId];
        }
        return null;
    }

    private int GetBaseItemSpawnCount(Room room)
    {
        if (room == null)
            return 0;

        int minItems = Mathf.Max(0, room.minItemSpawn);
        int maxItems = Mathf.Max(minItems, room.maxItemSpawn);
        return Random.Range(minItems, maxItems + 1);
    }
}
