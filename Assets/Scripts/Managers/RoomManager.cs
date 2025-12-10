using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RoomManager : MonoBehaviour
{
    [Header("Prefabs & Refs")]
    public GameObject[] roomPrefabs;     // index = (int)RoomType
    public Transform player;

    // runtime
    private Dictionary<int, Room> roomInstances = new();
    private int currentRoomID = 1;

    // expose layout untuk UI
    public DungeonLayout Layout { get; private set; }

    // ====== PUBLIC API ======
    public void Initialize(DungeonLayout layout)
    {
        Layout = layout;
        InstantiateRooms();
        MovePlayerToRoom(1, -1);

        var diff = GlobalDifficultyState.Instance;
        if (diff != null)
        {
            diff.SetTotalRooms(Layout?.roomDataMap?.Count ?? 0);
        }

        var minimap = FindObjectOfType<MinimapUI>();
        if (minimap != null)
        {
            minimap.InitializeMinimap(this);   // ← overload baru yang menerima RoomManager
            minimap.SetCurrentRoom(1);
        }
    }

    public void MovePlayerToRoom(int nextRoomID, int fromDoorIndex)
    {
        if (!roomInstances.ContainsKey(nextRoomID)) return;

        currentRoomID = nextRoomID;
        Room nextRoom = roomInstances[nextRoomID];

        // default spawn
        Vector3 spawnPos = nextRoom.transform.TransformPoint(nextRoom.playerSpawn);

        // jika masuk lewat pintu tertentu
        if (fromDoorIndex >= 0 && fromDoorIndex < nextRoom.doors.Length && nextRoom.doors[fromDoorIndex])
        {
            Transform door = nextRoom.doors[fromDoorIndex].transform;
            spawnPos = door.position + door.forward * 1.5f;
        }

        player.SetParent(null, true);
        player.SetParent(nextRoom.transform, true);
        Debug.Log($"Before move: Player Pos {player.position}, Room {nextRoomID} Pos {nextRoom.transform.position}");
        player.position = spawnPos;
        player.rotation = nextRoom.transform.rotation;
        Debug.Log($"After move: Player Pos {player.position}, Room {nextRoomID} Pos {nextRoom.transform.position}");

        nextRoom.OnPlayerEnter(fromDoorIndex);

        var minimap = FindObjectOfType<MinimapUI>();
        if (minimap != null) minimap.SetCurrentRoom(nextRoomID);
    }

    // ====== INTERNAL ======
    void InstantiateRooms()
    {
        foreach (var data in Layout.roomDataMap.Values)
        {
            GameObject prefab = roomPrefabs[(int)data.roomType];
            GameObject instance = Instantiate(prefab, data.worldPosition, Quaternion.identity);
            // set name agar gampang dicari di hierarchy
            instance.name = $"Room_{data.id}_{data.roomType}";
            Room room = instance.GetComponent<Room>();
            room.Initialize(data);
            roomInstances[data.id] = room;
        }
    }
}
