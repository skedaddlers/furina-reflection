using UnityEngine;
using System.Collections.Generic;

public enum RoomType
{
    Start,
    Normal,
    Boss,
    Shop
}

[System.Serializable]
public class RoomData
{
    public int id;
    public int[] neighbors = new int[4]; // forward, right, back, left
    public Vector3 worldPosition;
    public RoomType roomType;
}
public class RoomManager : MonoBehaviour
{
    public GameObject[] roomPrefabs;
    public Transform player;
    public float roomOffset = 15f;
    public int totalRooms = 5;

    private Dictionary<int, Room> roomInstances = new();
    private Dictionary<int, RoomData> roomDataMap = new();
    private int currentRoomID = 1;
    private int roomsGenerated = 0;

    public void Initialize()
    {
        GenerateRoomGraph();
        InstantiateRooms();
        MovePlayerToRoom(1, -1); // masuk ke room 1 (spawn room)
    }
    public void GenerateRoomGraph()
    {
        // placeholder graph data, will be replaced by procedural generation
        roomDataMap[1] = new RoomData { id = 1, neighbors = new int[] { 3, 0, 0, 0 }, roomType = RoomType.Start };
        roomDataMap[2] = new RoomData { id = 2, neighbors = new int[] { 0, 6, 3, 0 }, roomType = RoomType.Normal };
        roomDataMap[3] = new RoomData { id = 3, neighbors = new int[] { 2, 0, 1, 6 }, roomType = RoomType.Normal };
        roomDataMap[6] = new RoomData { id = 6, neighbors = new int[] { 0, 0, 3, 2 }, roomType = RoomType.Shop };

        // Hitung posisi fisik berdasarkan graph
        Vector3 basePos = Vector3.zero;
        foreach (var kvp in roomDataMap)
        {
            var data = kvp.Value;
            data.worldPosition = new Vector3(data.id * roomOffset, 0, 0); // linear placement
        }
    }

    void InstantiateRooms()
    {
        foreach (var data in roomDataMap.Values)
        {
            GameObject prefab = roomPrefabs[(int)data.roomType];
            GameObject instance = Instantiate(prefab, data.worldPosition, Quaternion.identity);
            Room room = instance.GetComponent<Room>();
            room.roomIndex = data.id;
            room.roomNeighbors = data.neighbors;
            room.Initialize(data);
            roomInstances[data.id] = room;
        }
    }

    public void MovePlayerToRoom(int nextRoomID, int fromDoorIndex)
    {
        Debug.Log($"Request move to Room {nextRoomID} from door {fromDoorIndex}");
        if (!roomInstances.ContainsKey(nextRoomID)) return;

        currentRoomID = nextRoomID;
        Room nextRoom = roomInstances[nextRoomID];

        Vector3 spawnPos;

        // Default spawn
        spawnPos = nextRoom.transform.TransformPoint(nextRoom.playerSpawn);

        // Kalau ada door yang cocok (pintu masuk)
        if (fromDoorIndex >= 0 && fromDoorIndex < nextRoom.doors.Length && nextRoom.doors[fromDoorIndex])
        {
            Transform doorTransform = nextRoom.doors[fromDoorIndex].transform;
            spawnPos = doorTransform.position; // gunakan world-space
            Debug.Log($"Spawning at door {fromDoorIndex} → worldPos {spawnPos}");
        }

        // Lepaskan dulu parent agar tidak terpengaruh posisi lokal
        player.SetParent(null, true);

        // Set posisi world player langsung
        player.position = spawnPos;
        player.rotation = nextRoom.transform.rotation;

        // Parent kembali ke room setelah diposisikan
        player.SetParent(nextRoom.transform, true);

        Debug.Log($"Moved to Room {nextRoomID}");
    }

}