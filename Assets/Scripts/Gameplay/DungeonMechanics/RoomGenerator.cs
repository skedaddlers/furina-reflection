using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum RoomType { Start, Normal, Boss, Shop }

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
    public float loopChance = 0.3f;

    [Header("Generation Seed")]
    public int seed = -1; // -1 = random
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
        var start = new RoomData { id = nextRoomID++, neighbors = new int[4], roomType = RoomType.Start };
        Layout.roomDataMap[start.id] = start;

        gridPositions.Clear();
        occupied.Clear();
        gridPositions[start.id] = Vector2Int.zero;
        occupied[Vector2Int.zero] = start.id;

        // Main path
        var mainPath = new List<int> { start.id };
        GenerateMainPath(start.id, totalRooms, mainPath);

        // Branches & shops
        AddBranchesAndShops(totalRooms, mainPath);

        // Loops
        if (Random.value < dungeonConfig.loopChance) AddLoops();

        // Boss
        PlaceBossRoom(mainPath);

        // bounds grid (diisi saat CalculateWorldPositions)
    }

    void GenerateMainPath(int startId, int totalRooms, List<int> mainPath)
    {
        int current = startId;
        int pathLength = Mathf.Min(totalRooms / 2, 6);

        for (int i = 0; i < pathLength && nextRoomID <= totalRooms; i++)
        {
            int dir = GetRandomEmptyDirection(current);
            if (dir == -1) break;

            var newRoom = new RoomData { id = nextRoomID++, neighbors = new int[4], roomType = RoomType.Normal };

            Layout.roomDataMap[current].neighbors[dir] = newRoom.id;
            newRoom.neighbors[Opp(dir)] = current;

            Vector2Int pos = gridPositions[current] + DIRS[dir];
            gridPositions[newRoom.id] = pos;
            occupied[pos] = newRoom.id;

            Layout.roomDataMap[newRoom.id] = newRoom;
            mainPath.Add(newRoom.id);
            current = newRoom.id;
        }
    }

    void AddBranchesAndShops(int targetCount, List<int> mainPath)
    {
        var roomsWithSpace = new List<int>(mainPath);
        int shopsPlaced = 0;

        while (Layout.roomDataMap.Count < targetCount - 1 && roomsWithSpace.Count > 0)
        {
            int branchPoint = roomsWithSpace[Random.Range(0, roomsWithSpace.Count)];
            int dir = GetRandomEmptyDirection(branchPoint);

            if (dir == -1) { roomsWithSpace.Remove(branchPoint); continue; }

            RoomType type = RoomType.Normal;
            if (shopsPlaced < dungeonConfig.shopRoomCount && Random.value < 0.3f) { type = RoomType.Shop; shopsPlaced++; }

            var newRoom = new RoomData { id = nextRoomID++, neighbors = new int[4], roomType = type };
            Layout.roomDataMap[branchPoint].neighbors[dir] = newRoom.id;
            newRoom.neighbors[Opp(dir)] = branchPoint;

            Vector2Int pos = gridPositions[branchPoint] + DIRS[dir];
            gridPositions[newRoom.id] = pos;
            occupied[pos] = newRoom.id;

            Layout.roomDataMap[newRoom.id] = newRoom;

            if (type == RoomType.Normal && GetEmptyDirectionCount(newRoom.id) > 0)
                roomsWithSpace.Add(newRoom.id);
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
