using UnityEngine;

public class GlobalDifficultyState : MonoBehaviour
{
    public static GlobalDifficultyState Instance { get; private set; }

    [Header("Base Enemy Settings")]
    [Tooltip("Jumlah musuh dasar di room normal awal")]
    public float baseEnemyCount = 3f;

    [Tooltip("Seberapa cepat jumlah musuh naik per jarak dari start (linear)")]
    public float enemyCountProgressionPerDistance = 0.15f;

    [Header("Room Type Multipliers")]
    public float normalRoomMultiplier = 1.0f;
    public float eliteRoomMultiplier  = 1.5f;
    public float bossRoomMultiplier   = 2.0f;

    [Header("Global DDA Multiplier")]
    [Tooltip("Diubah oleh sistem DDA. 1 = default, >1 lebih susah, <1 lebih mudah")]
    public float globalDifficultyMultiplier = 1.0f;
    public float minDifficultyMultiplier = 0.5f;
    public float maxDifficultyMultiplier = 2.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int GetEnemyCountForRoom(Room room)
    {
        if (room == null) return 0;

        // 1. faktor jarak dari start (distanceFromStart boleh 0,1,2,...)
        float distanceFactor = 1f + enemyCountProgressionPerDistance * room.distanceFromStart;

        // 2. faktor tipe ruangan
        float typeFactor = room.roomType switch
        {
            RoomType.Elite => eliteRoomMultiplier,
            RoomType.Boss  => bossRoomMultiplier,
            _              => normalRoomMultiplier
        };

        // 3. faktor global dari DDA
        float dda = Mathf.Clamp(globalDifficultyMultiplier, 
                                minDifficultyMultiplier, 
                                maxDifficultyMultiplier);

        // 4. hitung
        float raw = baseEnemyCount * distanceFactor * typeFactor * dda;
        Debug.Log($"[GlobalDifficultyState] Calculated enemy count for Room {room.roomIndex} (Distance: {room.distanceFromStart}, Type: {room.roomType}) => Raw: {raw}");
        int result = Mathf.RoundToInt(raw);
        result = Mathf.Clamp(result, 1, room.maxEnemies);

        return result;
    }
}
