using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum RoomType { Start, Normal, Boss, Shop, Event, Elite }

[System.Serializable]
public class RoomData
{
    public int id;
    public int[] neighbors = new int[4]; // forward, right, back, left
    public Vector3 worldPosition;
    public RoomType roomType;
    public int distanceFromStart;
    public float difficultyWeight;
}

[System.Serializable]
public class DungeonConfig
{
    [Header("Room Count Configuration")]
    public int minRooms = 8;
    public int maxRooms = 16;

    [Header("Room Type Distribution")]
    [Range(0f , 1f)] public float normalRoomRatio = 0.5f;
    [Range(0f , 1f)] public float shopRoomRatio = 0.2f;
    [Range(0f , 1f)] public float eventRoomRatio = 0.15f;
    [Range(0f , 1f)] public float eliteRoomRatio = 0.15f;

    [Header("Guaranteed Rooms")]
    public int guaranteedShops = 2;        // At least 2 shops
    public int guaranteedElites = 1;       // At least 1 elite
    public int guaranteedEvents = 2;       // At least 2 events
    public bool guaranteeEarlyShop = true; // Shop within 3 rooms of start

    [Header("Room Placement Rules")]
    public int minDistanceBetweenShops = 3;
    public int minDistanceBetweenElites = 4;
    public int eliteMinDistanceFromStart = 3;

    [Header("Room Connection Settings")]
    public int maxConnectionsPerRoom = 3;
    public float loopChance = 0.3f;

    [Header("Generation Seed")]
    public int seed = -1; // -1 = random

    [Header("DDA Modifiers")]
    public float eliteSpawnModifier = 1.0f;  // Multiplier from DDA
    public float eventBenefitModifier = 1.0f; // How beneficial events are
}

[System.Serializable]
public class DungeonLayout
{
    public Dictionary<int, RoomData> roomDataMap = new();
    public Dictionary<int, Vector2Int> roomGridPositions = new();
    public int gridMinX, gridMaxX, gridMinY, gridMaxY;
}

public class RoomGenerator : MonoBehaviour
{
    [Header("Generator Settings")]
    public DungeonConfig dungeonConfig = new DungeonConfig();
    public float roomOffset = 15f;

    // output
    public DungeonLayout Layout { get; private set; } = new DungeonLayout();

    // internal
    private int nextRoomID = 1;
    private readonly Vector2Int[] DIRS = {
        new Vector2Int(0, 1),   // forward (0)
        new Vector2Int(1, 0),   // right   (1)
        new Vector2Int(0, -1),  // back    (2)
        new Vector2Int(-1, 0),  // left    (3)
    };
    private Dictionary<int, Vector2Int> gridPositions = new();
    private Dictionary<Vector2Int, int> occupied = new();
    private Dictionary<int, int> roomDistances = new();

    // ====== PUBLIC API ======
    public DungeonLayout Generate()
    {
        GenerateRoomGraph();
        CalculateWorldPositions();
        return Layout;
    }

    // ====== GENERATION STEPS ======
    void GenerateRoomGraph()
{
    Layout.roomDataMap.Clear();
    nextRoomID = 1;

    // Seed
    if (dungeonConfig.seed == -1)
        dungeonConfig.seed = Random.Range(0, int.MaxValue);
    Random.InitState(dungeonConfig.seed);

    int totalRooms = Random.Range(dungeonConfig.minRooms, dungeonConfig.maxRooms + 1);
    Debug.Log($"[RoomGenerator] totalRooms={totalRooms} seed={dungeonConfig.seed}");

    // Start
    var start = new RoomData {
        id = nextRoomID++,
        neighbors = new int[4],
        roomType = RoomType.Start,
        distanceFromStart = 0
    };

    Layout.roomDataMap[start.id] = start;

    gridPositions.Clear();
    occupied.Clear();
    gridPositions[start.id] = Vector2Int.zero;
    occupied[Vector2Int.zero] = start.id;

    // DFS layout: build rooms first (no boss yet)
    GenerateBaseLayoutDFS(start.id, totalRooms);

    // optional loops (sebelum boss biar jarak final akurat)
    if (Random.value < dungeonConfig.loopChance)
        AddLoops();

    // recompute shortest-path distances (penting kalau ada loop)
    RecalculateShortestDistancesFromStart(start.id);

    // boss = dead-end terjauh (atau fallback)
    SelectAndMarkBossAsFarthestDeadEnd(start.id);

    // assign types excluding Start & Boss
    AssignRoomTypes();

    // update weights after all distances set
    UpdateDifficultyWeights();

    // Validate distribution (early shop, etc.)
    ValidateRoomDistribution();
}
void UpdateDifficultyWeights()
{
    foreach (var room in Layout.roomDataMap.Values)
    {
        room.difficultyWeight = Mathf.Clamp01(0.5f + 0.1f * room.distanceFromStart);
    }
}

void GenerateBaseLayoutDFS(int startId, int targetRooms)
{
    // DFS pakai stack
    var stack = new Stack<int>();
    stack.Push(startId);

    while (Layout.roomDataMap.Count < targetRooms && stack.Count > 0)
    {
        int current = stack.Peek();

        // stop kalau udah kebanyakan koneksi
        if (GetConnectionCount(current) >= dungeonConfig.maxConnectionsPerRoom)
        {
            stack.Pop();
            continue;
        }

        int dir = GetRandomEmptyDirection(current);
        if (dir == -1)
        {
            // dead end -> backtrack (ini yang bikin branches kebentuk)
            stack.Pop();
            continue;
        }

        // create new room
        var newRoom = new RoomData
        {
            id = nextRoomID++,
            neighbors = new int[4],
            roomType = RoomType.Normal, // sementara
        };

        // connect
        Layout.roomDataMap[current].neighbors[dir] = newRoom.id;
        newRoom.neighbors[Opp(dir)] = current;

        // position
        Vector2Int pos = gridPositions[current] + DIRS[dir];
        gridPositions[newRoom.id] = pos;
        occupied[pos] = newRoom.id;

        Layout.roomDataMap[newRoom.id] = newRoom;

        // DFS: selalu deepen dulu biar “jauh”
        stack.Push(newRoom.id);

        // OPTIONAL: bikin room cabang jadi “leaf” lebih sering
        // (kalau lo mau branch lebih banyak, uncomment + tweak nilainya)
        
        if (Random.value < 0.25f)
        {
            // jangan explore room baru ini, jadi cabang pendek
            stack.Pop();
        }
        
    }
}
void RecalculateShortestDistancesFromStart(int startId)
{
    roomDistances.Clear();

    // init distances
    foreach (var id in Layout.roomDataMap.Keys)
        roomDistances[id] = int.MaxValue;

    var q = new Queue<int>();
    roomDistances[startId] = 0;
    Layout.roomDataMap[startId].distanceFromStart = 0;
    q.Enqueue(startId);

    while (q.Count > 0)
    {
        int cur = q.Dequeue();
        int baseDist = roomDistances[cur];

        for (int d = 0; d < 4; d++)
        {
            int nb = Layout.roomDataMap[cur].neighbors[d];
            if (nb == 0) continue;

            if (roomDistances[nb] > baseDist + 1)
            {
                roomDistances[nb] = baseDist + 1;
                Layout.roomDataMap[nb].distanceFromStart = baseDist + 1;
                q.Enqueue(nb);
            }
        }
    }
}
void SelectAndMarkBossAsFarthestDeadEnd(int startId)
{
    // reset boss kalau ada yang ke-tag boss dari run sebelumnya
    foreach (var r in Layout.roomDataMap.Values)
        if (r.roomType == RoomType.Boss) r.roomType = RoomType.Normal;

    // dead-end = degree 1 (bukan start)
    var farthestDeadEnd = Layout.roomDataMap
        .Where(kvp => kvp.Key != startId && GetConnectionCount(kvp.Key) == 1)
        .OrderByDescending(kvp => kvp.Value.distanceFromStart)
        .Select(kvp => kvp.Key)
        .FirstOrDefault();

    if (farthestDeadEnd != 0)
    {
        Layout.roomDataMap[farthestDeadEnd].roomType = RoomType.Boss;
        return;
    }

    // fallback: ambil node terjauh
    int farthest = Layout.roomDataMap
        .Where(kvp => kvp.Key != startId)
        .OrderByDescending(kvp => kvp.Value.distanceFromStart)
        .Select(kvp => kvp.Key)
        .FirstOrDefault();

    if (farthest != 0)
        Layout.roomDataMap[farthest].roomType = RoomType.Boss;
}


    void GenerateBaseLayout(int startId, int totalRooms)
    {
        var frontier = new Queue<int>();
        frontier.Enqueue(startId);
        
        while (Layout.roomDataMap.Count < totalRooms && frontier.Count > 0)
        {
            int current = frontier.Dequeue();
            int connectionsLeft = dungeonConfig.maxConnectionsPerRoom - GetConnectionCount(current);
            
            for (int i = 0; i < connectionsLeft && Layout.roomDataMap.Count < totalRooms; i++)
            {
                int dir = GetRandomEmptyDirection(current);
                if (dir == -1) break;
                
                var newRoom = new RoomData { 
                    id = nextRoomID++, 
                    neighbors = new int[4], 
                    roomType = RoomType.Normal, // Temporary, will reassign
                    distanceFromStart = roomDistances[current] + 1,
                };
                
                // Connect rooms
                Layout.roomDataMap[current].neighbors[dir] = newRoom.id;
                newRoom.neighbors[Opp(dir)] = current;
                
                // Update positions
                Vector2Int pos = gridPositions[current] + DIRS[dir];
                gridPositions[newRoom.id] = pos;
                occupied[pos] = newRoom.id;
                roomDistances[newRoom.id] = newRoom.distanceFromStart;
                
                Layout.roomDataMap[newRoom.id] = newRoom;
                
                // Add to frontier if it has space for more connections
                if (GetEmptyDirectionCount(newRoom.id) > 0)
                    frontier.Enqueue(newRoom.id);
            }
            
            // Re-add current room if it still has space
            if (GetEmptyDirectionCount(current) > 0 && Random.value < 0.5f)
                frontier.Enqueue(current);
        }

        foreach (var kvp in Layout.roomDataMap)
        {
            var room = kvp.Value;
            // contoh: makin jauh makin berat, tapi clamp dikit
            room.difficultyWeight = Mathf.Clamp01(0.5f + 0.1f * room.distanceFromStart);
        }
        
        // Add some loops
        if (Random.value < dungeonConfig.loopChance) 
            AddLoops();
    }

    void AssignRoomTypes()
    {
        // Get all non-start rooms sorted by distance from start
        var assignableRooms = Layout.roomDataMap
        .Where(kvp => kvp.Value.roomType != RoomType.Start &&
                      kvp.Value.roomType != RoomType.Boss)
        .OrderBy(kvp => kvp.Value.distanceFromStart)
        .ThenBy(kvp => Random.value)
        .Select(kvp => kvp.Key)
        .ToList();

    int totalAssignable = assignableRooms.Count;

    int shopCount = Mathf.Max(
        dungeonConfig.guaranteedShops,
        Mathf.RoundToInt(totalAssignable * dungeonConfig.shopRoomRatio)
    );

    int eliteCount = Mathf.Max(
        dungeonConfig.guaranteedElites,
        Mathf.RoundToInt(totalAssignable * dungeonConfig.eliteRoomRatio * dungeonConfig.eliteSpawnModifier)
    );

    int eventCount = Mathf.Max(
        dungeonConfig.guaranteedEvents,
        Mathf.RoundToInt(totalAssignable * dungeonConfig.eventRoomRatio)
    );
        
        // Place guaranteed early shop
        if (dungeonConfig.guaranteeEarlyShop && shopCount > 0)
        {
            var earlyRooms = assignableRooms
                .Where(id => roomDistances[id] <= 2 && roomDistances[id] > 0)
                .ToList();
            
            if (earlyRooms.Count > 0)
            {
                int shopId = earlyRooms[Random.Range(0, earlyRooms.Count)];
                Layout.roomDataMap[shopId].roomType = RoomType.Shop;
                assignableRooms.Remove(shopId);
                shopCount--;
            }
        }
        
        // Place shops with spacing
        PlaceRoomsWithSpacing(assignableRooms, RoomType.Shop, shopCount, dungeonConfig.minDistanceBetweenShops);
        
        // Place elites with spacing and distance requirement
        var eliteEligible = assignableRooms
            .Where(id => roomDistances[id] >= dungeonConfig.eliteMinDistanceFromStart)
            .ToList();
        PlaceRoomsWithSpacing(eliteEligible, RoomType.Elite, eliteCount, dungeonConfig.minDistanceBetweenElites);
        
        // Remove assigned elites from assignable list
        foreach (var id in eliteEligible)
        {
            if (Layout.roomDataMap[id].roomType == RoomType.Elite)
                assignableRooms.Remove(id);
        }
        
        // Place events randomly
        for (int i = 0; i < eventCount && assignableRooms.Count > 0; i++)
        {
            int index = Random.Range(0, assignableRooms.Count);
            Layout.roomDataMap[assignableRooms[index]].roomType = RoomType.Event;
            assignableRooms.RemoveAt(index);
        }
        
        // Remaining rooms stay as Normal
        foreach (var id in assignableRooms)
        {
            Layout.roomDataMap[id].roomType = RoomType.Normal;
        }
    }

    void PlaceRoomsWithSpacing(List<int> eligibleRooms, RoomType roomType, int count, int minSpacing)
    {
        var placed = new List<int>();
        var available = new List<int>(eligibleRooms);
        
        for (int i = 0; i < count && available.Count > 0; i++)
        {
            // Filter by spacing requirement
            var validRooms = available.Where(id => 
            {
                foreach (var placedId in placed)
                {
                    if (GetPathDistance(id, placedId) < minSpacing)
                        return false;
                }
                return true;
            }).ToList();
            
            if (validRooms.Count == 0)
                validRooms = available; // Fallback if spacing impossible
            
            int chosenId = validRooms[Random.Range(0, validRooms.Count)];
            Layout.roomDataMap[chosenId].roomType = roomType;
            placed.Add(chosenId);
            available.Remove(chosenId);
        }
    }

    int GetPathDistance(int room1, int room2)
    {
        // Simple Manhattan distance in grid
        var pos1 = gridPositions[room1];
        var pos2 = gridPositions[room2];
        return Mathf.Abs(pos1.x - pos2.x) + Mathf.Abs(pos1.y - pos2.y);
    }

    void ValidateRoomDistribution()
    {
        // Count current distribution
        var typeCounts = new Dictionary<RoomType, int>();
        foreach (RoomType type in System.Enum.GetValues(typeof(RoomType)))
            typeCounts[type] = 0;
        
        foreach (var room in Layout.roomDataMap.Values)
            typeCounts[room.roomType]++;
        
        // Log distribution
        Debug.Log($"[Room Distribution] Start:{typeCounts[RoomType.Start]} " +
                  $"Normal:{typeCounts[RoomType.Normal]} Elite:{typeCounts[RoomType.Elite]} " +
                  $"Event:{typeCounts[RoomType.Event]} Shop:{typeCounts[RoomType.Shop]} " +
                  $"Boss:{typeCounts[RoomType.Boss]}");
        
        // Check for early shop
        bool hasEarlyShop = Layout.roomDataMap.Values.Any(r => 
            r.roomType == RoomType.Shop && r.distanceFromStart <= 2);
        
        if (!hasEarlyShop && dungeonConfig.guaranteeEarlyShop)
        {
            Debug.LogWarning("[RoomGenerator] No early shop found, converting a room...");
            // Convert nearest normal room to shop
            var candidate = Layout.roomDataMap.Values
                .Where(r => r.roomType == RoomType.Normal && r.distanceFromStart <= 2)
                .OrderBy(r => r.distanceFromStart)
                .FirstOrDefault();
            
            if (candidate != null)
                candidate.roomType = RoomType.Shop;
        }
    }

    void AddLoops()
    {
        var all = new List<int>(Layout.roomDataMap.Keys);
        int loopsToAdd = Random.Range(1, 3);

        for (int i = 0; i < loopsToAdd; i++)
        {
            var candidate = all.Where(r => GetEmptyDirectionCount(r) > 0 && Layout.roomDataMap[r].roomType != RoomType.Boss).ToList();
            if (candidate.Count == 0) break;

            int r1 = candidate[Random.Range(0, candidate.Count)];
            foreach (var r2 in all)
            {
                if (r2 == r1) continue;
                if (Layout.roomDataMap[r2].roomType == RoomType.Boss) continue;

                Vector2Int p1 = gridPositions[r1], p2 = gridPositions[r2];
                if (Mathf.Abs(p1.x - p2.x) + Mathf.Abs(p1.y - p2.y) != 1) continue;

                int dir = DirFromTo(p1, p2);
                int opp = Opp(dir);

                if (Layout.roomDataMap[r1].neighbors[dir] == 0 && Layout.roomDataMap[r2].neighbors[opp] == 0)
                {
                    Layout.roomDataMap[r1].neighbors[dir] = r2;
                    Layout.roomDataMap[r2].neighbors[opp] = r1;
                    break;
                }
            }
        }
    }

    void PlaceBossRoom(List<int> mainPath)
    {
        int bossLoc = -1;

        if (mainPath.Count > 0)
        {
            int last = mainPath[^1];
            if (GetRandomEmptyDirection(last) != -1) bossLoc = last;
        }

        if (bossLoc == -1)
        {
            var deadEnds = Layout.roomDataMap.Where(kvp => GetConnectionCount(kvp.Key) == 1 && kvp.Value.roomType != RoomType.Start)
                                             .Select(k => k.Key).ToList();
            if (deadEnds.Count > 0)
            {
                bossLoc = deadEnds[Random.Range(0, deadEnds.Count)];
                if (GetRandomEmptyDirection(bossLoc) == -1)
                {
                    // jadikan ruangan itu sendiri sebagai boss
                    Layout.roomDataMap[bossLoc].roomType = RoomType.Boss;
                    return;
                }
            }
        }

        if (bossLoc != -1)
        {
            int dir = GetRandomEmptyDirection(bossLoc);
            if (dir != -1)
            {
                var boss = new RoomData { id = nextRoomID++, neighbors = new int[4], roomType = RoomType.Boss };
                Layout.roomDataMap[bossLoc].neighbors[dir] = boss.id;
                boss.neighbors[Opp(dir)] = bossLoc;
                Layout.roomDataMap[boss.id] = boss;

                Vector2Int pos = gridPositions[bossLoc] + DIRS[dir];
                gridPositions[boss.id] = pos;
                occupied[pos] = boss.id;
            }
        }
    }

    void PlaceBossRoom()
    {
        // Find furthest dead-end room
        var deadEnds = Layout.roomDataMap
            .Where(kvp => GetConnectionCount(kvp.Key) == 1 && 
                   kvp.Value.roomType != RoomType.Start)
            .OrderByDescending(kvp => kvp.Value.distanceFromStart)
            .Select(kvp => kvp.Key)
            .ToList();
        
        if (deadEnds.Count > 0)
        {
            // Try to extend from furthest dead end
            int bossLocation = deadEnds[0];
            int dir = GetRandomEmptyDirection(bossLocation);
            
            if (dir != -1)
            {
                // Can extend - create new boss room
                var boss = new RoomData { 
                    id = nextRoomID++, 
                    neighbors = new int[4], 
                    roomType = RoomType.Boss,
                    distanceFromStart = roomDistances[bossLocation] + 1
                };
                
                Layout.roomDataMap[bossLocation].neighbors[dir] = boss.id;
                boss.neighbors[Opp(dir)] = bossLocation;
                Layout.roomDataMap[boss.id] = boss;
                
                Vector2Int pos = gridPositions[bossLocation] + DIRS[dir];
                gridPositions[boss.id] = pos;
                occupied[pos] = boss.id;
            }
            else
            {
                // Can't extend - convert the room itself
                Layout.roomDataMap[bossLocation].roomType = RoomType.Boss;
            }
        }
    }

    void CalculateWorldPositions()
    {
        foreach (var kvp in Layout.roomDataMap)
        {
            int id = kvp.Key;
            var gpos = gridPositions[id];
            kvp.Value.worldPosition = new Vector3(gpos.x * roomOffset, 0, gpos.y * roomOffset);
            Layout.roomGridPositions[id] = gpos;
        }

        Layout.gridMinX = gridPositions.Values.Min(v => v.x);
        Layout.gridMaxX = gridPositions.Values.Max(v => v.x);
        Layout.gridMinY = gridPositions.Values.Min(v => v.y);
        Layout.gridMaxY = gridPositions.Values.Max(v => v.y);
    }

    // ===== helpers =====
    int GetRandomEmptyDirection(int roomId)
    {
        var empty = new List<int>();
        Vector2Int basePos = gridPositions[roomId];

        for (int i = 0; i < 4; i++)
        {
            if (Layout.roomDataMap[roomId].neighbors[i] != 0) continue;
            Vector2Int target = basePos + DIRS[i];
            if (!occupied.ContainsKey(target)) empty.Add(i);
        }
        if (empty.Count == 0) return -1;
        return empty[Random.Range(0, empty.Count)];
    }

    int GetEmptyDirectionCount(int roomId)
    {
        int c = 0;
        for (int i = 0; i < 4; i++) if (Layout.roomDataMap[roomId].neighbors[i] == 0) c++;
        return c;
    }

    int GetConnectionCount(int roomId)
    {
        int c = 0;
        for (int i = 0; i < 4; i++) if (Layout.roomDataMap[roomId].neighbors[i] != 0) c++;
        return c;
    }

    int Opp(int d) => d switch { 0 => 2, 1 => 3, 2 => 0, 3 => 1, _ => -1 };

    int DirFromTo(Vector2Int p1, Vector2Int p2)
    {
        if      (p2 == p1 + DIRS[0]) return 0;
        else if (p2 == p1 + DIRS[1]) return 1;
        else if (p2 == p1 + DIRS[2]) return 2;
        else if (p2 == p1 + DIRS[3]) return 3;
        return -1;
    }
}
