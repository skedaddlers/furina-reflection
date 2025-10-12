using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

[System.Serializable]
public class DungeonConfig
{
    [Header("Room Count Configuration")]
    public int minRooms = 8;
    public int maxRooms = 16;
    public int shopRoomCount = 3;
    
    [Header("Room Connection Settings")]
    public int maxConnectionsPerRoom = 3;
    public float loopChance = 0.3f; // chance to create additional connections
    
    [Header("Generation Seed")]
    public int seed = -1; // -1 for random seed
}

public class RoomManager : MonoBehaviour
{

    [System.Serializable]
    public class MinimapData
    {
        public Dictionary<int, Vector2Int> roomGridPositions;
        public int gridMinX, gridMaxX, gridMinY, gridMaxY;
        public Dictionary<int, RoomData> roomDataMap; // Add this
    }    
    public GameObject[] roomPrefabs;
    public Transform player;
    public float roomOffset = 15f;
    public DungeonConfig dungeonConfig = new DungeonConfig();

    public MinimapData minimapData = new MinimapData
    {
        roomGridPositions = new Dictionary<int, Vector2Int>(),
        roomDataMap = new Dictionary<int, RoomData>()
    };
    private Dictionary<int, Room> roomInstances = new();
    private int currentRoomID = 1;
    private int nextRoomID = 1;
    
    // For generation tracking
    private HashSet<int> visitedRooms = new HashSet<int>();
    private Queue<int> roomsToProcess = new Queue<int>();
    private readonly Vector2Int[] DIRS = new[]
    {
        new Vector2Int(0, 1),   // forward (0)
        new Vector2Int(1, 0),   // right   (1)
        new Vector2Int(0, -1),  // back    (2)
        new Vector2Int(-1, 0),  // left    (3)
    };
    private Dictionary<int, Vector2Int> gridPositions = new();
    private Dictionary<Vector2Int, int> occupied = new();

    public void Initialize()
    {
        GenerateRoomGraph();
        InstantiateRooms();
        MovePlayerToRoom(1, -1); // masuk ke room 1 (spawn room)
        MinimapUI minimap = FindObjectOfType<MinimapUI>();
        if (minimap != null)
        {
            minimap.InitializeMinimap();
            minimap.SetCurrentRoom(1);
        }
    }

    public void GenerateRoomGraph()
    {
        minimapData.roomDataMap.Clear();
        visitedRooms.Clear();
        roomsToProcess.Clear();
        nextRoomID = 1;
        
        // Set seed
        if (dungeonConfig.seed == -1)
            dungeonConfig.seed = Random.Range(0, int.MaxValue);
        Random.InitState(dungeonConfig.seed);
        
        // Determine total rooms
        int totalRooms = Random.Range(dungeonConfig.minRooms, dungeonConfig.maxRooms + 1);
        
        Debug.Log($"Generating dungeon with {totalRooms} rooms, seed: {dungeonConfig.seed}");
        
        // Step 1: Create Start Room
        RoomData startRoom = new RoomData 
        { 
            id = nextRoomID++, 
            neighbors = new int[4], 
            roomType = RoomType.Start 
        };
        minimapData.roomDataMap[startRoom.id] = startRoom;
        roomsToProcess.Enqueue(startRoom.id);

        gridPositions.Clear();
        occupied.Clear();

        gridPositions[startRoom.id] = Vector2Int.zero;
        occupied[Vector2Int.zero] = startRoom.id;

        
        // Step 2: Generate main path to boss using BFS
        List<int> mainPath = new List<int>();
        GenerateMainPath(startRoom.id, totalRooms, mainPath);
        
        // Step 3: Add branches and shop rooms
        AddBranchesAndShops(totalRooms, mainPath);
        
        // Step 4: Add some loops for interesting navigation
        if (Random.value < dungeonConfig.loopChance)
            AddLoops();
        
        // Step 5: Place boss room at the farthest point
        PlaceBossRoom(mainPath);
        
        // Step 6: Calculate world positions
        CalculateWorldPositions();
        
        // Debug output
        PrintDungeonStructure();
    }
    
    void GenerateMainPath(int startId, int totalRooms, List<int> mainPath)
    {
        mainPath.Add(startId);
        int currentRoom = startId;
        int pathLength = Mathf.Min(totalRooms / 2, 6); // Main path shouldn't be too long
        
        for (int i = 0; i < pathLength && nextRoomID <= totalRooms; i++)
        {
            // Find an empty direction
            int direction = GetRandomEmptyDirection(currentRoom);
            if (direction == -1) break;
            
            // Create new room
            RoomData newRoom = new RoomData
            {
                id = nextRoomID++,
                neighbors = new int[4],
                roomType = RoomType.Normal
            };
            
            // Connect rooms bidirectionally
            minimapData.roomDataMap[currentRoom].neighbors[direction] = newRoom.id;
            newRoom.neighbors[GetOppositeDirection(direction)] = currentRoom;

            Vector2Int pos = gridPositions[currentRoom] + DIRS[direction];
            gridPositions[newRoom.id] = pos;
            occupied[pos] = newRoom.id;


            minimapData.roomDataMap[newRoom.id] = newRoom;
            mainPath.Add(newRoom.id);
            currentRoom = newRoom.id;
        }
    }
    
    void AddBranchesAndShops(int targetRoomCount, List<int> mainPath)
    {
        List<int> roomsWithSpace = new List<int>(mainPath);
        int shopsPlaced = 0;
        int normalRoomsToAdd = targetRoomCount - minimapData.roomDataMap.Count - 1; // -1 for boss room

        while ((minimapData.roomDataMap.Count < targetRoomCount - 1) && roomsWithSpace.Count > 0)
        {
            // Pick a random room to branch from
            int branchPoint = roomsWithSpace[Random.Range(0, roomsWithSpace.Count)];
            int direction = GetRandomEmptyDirection(branchPoint);
            
            if (direction == -1)
            {
                roomsWithSpace.Remove(branchPoint);
                continue;
            }
            
            // Decide room type
            RoomType newRoomType = RoomType.Normal;
            if (shopsPlaced < dungeonConfig.shopRoomCount && Random.value < 0.3f)
            {
                newRoomType = RoomType.Shop;
                shopsPlaced++;
            }
            
            // Create new room
            RoomData newRoom = new RoomData
            {
                id = nextRoomID++,
                neighbors = new int[4],
                roomType = newRoomType
            };
            
            // Connect rooms
            minimapData.roomDataMap[branchPoint].neighbors[direction] = newRoom.id;
            newRoom.neighbors[GetOppositeDirection(direction)] = branchPoint;

            Vector2Int pos = gridPositions[branchPoint] + DIRS[direction];
            gridPositions[newRoom.id] = pos;
            occupied[pos] = newRoom.id;

            minimapData.roomDataMap[newRoom.id] = newRoom;
            
            // Add to potential branch points if it's not a shop
            if (newRoomType == RoomType.Normal && GetEmptyDirectionCount(newRoom.id) > 0)
            {
                roomsWithSpace.Add(newRoom.id);
            }
        }
    }
    
    void AddLoops()
    {
        // Try to add a few extra connections between rooms
        List<int> allRooms = new List<int>(minimapData.roomDataMap.Keys);
        int loopsToAdd = Random.Range(1, 3);
        
        for (int i = 0; i < loopsToAdd; i++)
        {
            // Pick a random room with space
            var candidateRooms = allRooms.Where(r => 
                GetEmptyDirectionCount(r) > 0 && 
                minimapData.roomDataMap[r].roomType != RoomType.Boss
            ).ToList();
            
            if (candidateRooms.Count == 0) break;
            
            int room1 = candidateRooms[Random.Range(0, candidateRooms.Count)];
            foreach (var room2 in allRooms)
            {
                if (room2 == room1) continue;
                if (minimapData.roomDataMap[room2].roomType == RoomType.Boss) continue;

                Vector2Int p1 = gridPositions[room1];
                Vector2Int p2 = gridPositions[room2];
                int manhattan = Mathf.Abs(p1.x - p2.x) + Mathf.Abs(p1.y - p2.y);
                if (manhattan != 1) continue; // cuma boleh 4-arah

                // tentukan dir dari room1 ke room2
                int dir = -1;
                if      (p2 == p1 + DIRS[0]) dir = 0;
                else if (p2 == p1 + DIRS[1]) dir = 1;
                else if (p2 == p1 + DIRS[2]) dir = 2;
                else if (p2 == p1 + DIRS[3]) dir = 3;

                if (dir == -1) continue;
                int opposite = GetOppositeDirection(dir);

                // hanya jika slot kosong di kedua sisi
                if (minimapData.roomDataMap[room1].neighbors[dir] == 0 &&
                    minimapData.roomDataMap[room2].neighbors[opposite] == 0)
                {
                    minimapData.roomDataMap[room1].neighbors[dir] = room2;
                    minimapData.roomDataMap[room2].neighbors[opposite] = room1;
                    break;
                }
            }
        }
    }
    
    void PlaceBossRoom(List<int> mainPath)
    {
        // Find the best location for boss room (usually at the end of main path or a dead end)
        int bossLocation = -1;
        
        // First, try to place at the end of main path
        if (mainPath.Count > 0)
        {
            int lastMainRoom = mainPath[mainPath.Count - 1];
            int direction = GetRandomEmptyDirection(lastMainRoom);
            if (direction != -1)
            {
                bossLocation = lastMainRoom;
            }
        }
        
        // If can't place at main path end, find a dead end
        if (bossLocation == -1)
        {
            var deadEnds = minimapData.roomDataMap.Where(kvp =>
                GetConnectionCount(kvp.Key) == 1 && 
                kvp.Value.roomType != RoomType.Start
            ).Select(kvp => kvp.Key).ToList();
            
            if (deadEnds.Count > 0)
            {
                bossLocation = deadEnds[Random.Range(0, deadEnds.Count)];
                int direction = GetRandomEmptyDirection(bossLocation);
                if (direction != -1)
                {
                    // Use this location
                }
                else
                {
                    // Convert this room to boss room
                    minimapData.roomDataMap[bossLocation].roomType = RoomType.Boss;
                    return;
                }
            }
        }
        
        // Create boss room
        if (bossLocation != -1)
        {
            int direction = GetRandomEmptyDirection(bossLocation);
            if (direction != -1)
            {
                RoomData bossRoom = new RoomData
                {
                    id = nextRoomID++,
                    neighbors = new int[4],
                    roomType = RoomType.Boss
                };
                
                minimapData.roomDataMap[bossLocation].neighbors[direction] = bossRoom.id;
                bossRoom.neighbors[GetOppositeDirection(direction)] = bossLocation;
                minimapData.roomDataMap[bossRoom.id] = bossRoom;

                Vector2Int pos = gridPositions[bossLocation] + DIRS[direction];
                gridPositions[bossRoom.id] = pos;
                occupied[pos] = bossRoom.id;
            }
        }
    }
    
    void CalculateWorldPositions()
    {
        foreach (var kvp in minimapData.roomDataMap)
        {
            int id = kvp.Key;
            Vector2Int gpos = gridPositions[id];
            kvp.Value.worldPosition = new Vector3(gpos.x * roomOffset, 0, gpos.y * roomOffset);
        }

        minimapData.roomGridPositions = new Dictionary<int, Vector2Int>(gridPositions);
        minimapData.gridMinX = gridPositions.Values.Min(v => v.x);
        minimapData.gridMaxX = gridPositions.Values.Max(v => v.x);
        minimapData.gridMinY = gridPositions.Values.Min(v => v.y);
        minimapData.gridMaxY = gridPositions.Values.Max(v => v.y);

    }
    
    Dictionary<int, Vector2Int> GenerateGridLayout()
    {
        Dictionary<int, Vector2Int> gridPositions = new Dictionary<int, Vector2Int>();
        HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();
        Queue<int> toProcess = new Queue<int>();
        
        // Start from room 1 at center (0,0)
        gridPositions[1] = Vector2Int.zero;
        occupiedPositions.Add(Vector2Int.zero);
        toProcess.Enqueue(1);
        
        // BFS to place all connected rooms
        while (toProcess.Count > 0)
        {
            int currentRoomId = toProcess.Dequeue();
            Vector2Int currentPos = gridPositions[currentRoomId];
            RoomData currentRoom = minimapData.roomDataMap[currentRoomId];
            
            // Direction mapping: forward = north(0,1), right = east(1,0), back = south(0,-1), left = west(-1,0)
            Vector2Int[] directionOffsets = new Vector2Int[]
            {
                new Vector2Int(0, 1),   // forward
                new Vector2Int(1, 0),   // right
                new Vector2Int(0, -1),  // back
                new Vector2Int(-1, 0)   // left
            };
            
            // Process each neighbor
            for (int dir = 0; dir < 4; dir++)
            {
                int neighborId = currentRoom.neighbors[dir];
                if (neighborId != 0 && !gridPositions.ContainsKey(neighborId))
                {
                    Vector2Int neighborPos = currentPos + directionOffsets[dir];
                    
                    // Handle collision - find alternative position
                    if (occupiedPositions.Contains(neighborPos))
                    {
                        neighborPos = FindAlternativePosition(currentPos, occupiedPositions);
                    }
                    
                    gridPositions[neighborId] = neighborPos;
                    occupiedPositions.Add(neighborPos);
                    toProcess.Enqueue(neighborId);
                }
            }
        }
        
        return gridPositions;
    }
    
    Vector2Int FindAlternativePosition(Vector2Int basePos, HashSet<Vector2Int> occupied)
    {
        // Spiral outward to find empty position
        int radius = 1;
        while (radius < 10)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) == radius || Mathf.Abs(y) == radius)
                    {
                        Vector2Int testPos = basePos + new Vector2Int(x, y);
                        if (!occupied.Contains(testPos))
                            return testPos;
                    }
                }
            }
            radius++;
        }
        return basePos + new Vector2Int(radius, 0);
    }
    
    // Helper methods
    int GetRandomEmptyDirection(int roomId)
    {
        List<int> emptyDirs = new List<int>();
        Vector2Int basePos = gridPositions[roomId];

        for (int i = 0; i < 4; i++)
        {
            if (minimapData.roomDataMap[roomId].neighbors[i] != 0) continue;

            Vector2Int target = basePos + DIRS[i];
            if (!occupied.ContainsKey(target)) emptyDirs.Add(i);
        }
            
        if (emptyDirs.Count == 0) return -1;
        return emptyDirs[Random.Range(0, emptyDirs.Count)];
    }
    
    int GetEmptyDirectionCount(int roomId)
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            if (minimapData.roomDataMap[roomId].neighbors[i] == 0)
                count++;
        }
        return count;
    }
    
    int GetConnectionCount(int roomId)
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            if (minimapData.roomDataMap[roomId].neighbors[i] != 0)
                count++;
        }
        return count;
    }
    
    int GetOppositeDirection(int dir)
    {
        // forward↔back, right↔left
        return dir switch
        {
            0 => 2, // forward -> back
            1 => 3, // right -> left
            2 => 0, // back -> forward
            3 => 1, // left -> right
            _ => -1
        };
    }
    
    void PrintDungeonStructure()
    {
        Debug.Log("=== DUNGEON STRUCTURE ===");
        int normalCount = 0, shopCount = 0;

        foreach (var kvp in minimapData.roomDataMap.OrderBy(x => x.Key))
        {
            var room = kvp.Value;
            string neighbors = $"[F:{room.neighbors[0]}, R:{room.neighbors[1]}, B:{room.neighbors[2]}, L:{room.neighbors[3]}]";
            Debug.Log($"Room {room.id} ({room.roomType}) - Neighbors: {neighbors}");
            
            if (room.roomType == RoomType.Normal) normalCount++;
            else if (room.roomType == RoomType.Shop) shopCount++;
        }

        Debug.Log($"Total Rooms: {minimapData.roomDataMap.Count} | Normal: {normalCount} | Shops: {shopCount}");
    }

    void InstantiateRooms()
    {
        foreach (var data in minimapData.roomDataMap.Values)
        {
            GameObject prefab = roomPrefabs[(int)data.roomType];
            GameObject instance = Instantiate(prefab, data.worldPosition, Quaternion.identity);
            Room room = instance.GetComponent<Room>();
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
            spawnPos = doorTransform.position + doorTransform.forward * 1.5f; // spawn agak ke depan pintu
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
        MinimapUI minimap = FindObjectOfType<MinimapUI>();
        if (minimap != null)
        {
            minimap.SetCurrentRoom(nextRoomID);
        }
    }

}