using UnityEngine;
using System.Collections.Generic;

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

    [Header("Progression Scaling (based on cleared rooms)")]
    public AnimationCurve clearedRoomToMultiplier = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    public float progressionMultiplierMin = 0.8f;
    public float progressionMultiplierMax = 1.5f;

    [Header("Enemy Stat Progression (without DDA)")]
    public bool scaleEnemyStatsWithProgression = true;
    public float progressionEnemyDamageMax = 1.2f;
    public float progressionEnemyHealthMax = 1.4f;
    public float progressionEnemySpeedMax = 1.1f;
    public float progressionEnemyAttackSpeedMax = 1.15f;
    public float progressionEnemyAggroMax = 1.1f;

    [Header("Enemy Level Progression")]
    [Tooltip("Base enemy level used for the start room depth.")]
    public int baseEnemyLevel = 1;
    [Tooltip("How many enemy levels to add per room depth from the start room.")]
    public int levelsPerRoomDepth = 1;
    [Tooltip("Extra levels granted to elite room enemies on top of depth scaling.")]
    public int eliteRoomLevelBonus = 1;
    [Tooltip("Extra levels granted to boss room enemies on top of depth scaling.")]
    public int bossRoomLevelBonus = 2;
    [Tooltip("Safety cap for room-scaled enemy levels.")]
    public int maxEnemyLevel = 99;

    [Header("Enemy Stat Multipliers (set by DDA)")]
    public float enemyDamageMultiplier = 1f;
    public float enemyHealthMultiplier = 1f;
    public float enemySpeedMultiplier = 1f;
    public float enemyAttackSpeedMultiplier = 1f; // >1 = faster attacks (shorter cooldown)
    public float enemyAggroRangeMultiplier = 1f;
    public float enemyMultiplierMin = 0.5f;
    public float enemyMultiplierMax = 2.5f;

    [Header("Debug")]
    [SerializeField] private int currentEnemyCount = 0;
    [SerializeField] private float currentDamageMultiplier = 0;
    [SerializeField] private float currentHealthMultiplier = 0;
    [SerializeField] private float currentSpeedMultiplier = 0;
    [SerializeField] private float currentAttackSpeedMultiplier = 0;
    [SerializeField] private float currentAggroMultiplier = 0;

    private int _totalRooms = 0;
    private int _clearedRooms = 0;
    private readonly HashSet<int> _countedClearedRoomIds = new HashSet<int>();

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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void DestroyInstanceForRestart()
    {
        if (Instance == null) return;
        var go = Instance.gameObject;
        Instance = null;
        Object.Destroy(go);
    }

    private void OnEnable()
    {
        Room.OnRoomCleared += HandleRoomCleared;
    }

    private void OnDisable()
    {
        Room.OnRoomCleared -= HandleRoomCleared;
    }

    public void SetTotalRooms(int totalRooms)
    {
        _totalRooms = Mathf.Max(0, totalRooms);
        ResetProgression();
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

        // 4. faktor progression berdasarkan jumlah room yang sudah diselesaikan
        float progression = GetProgressionMultiplier();

        // 5. hitung
        float raw = baseEnemyCount * distanceFactor * typeFactor * dda * progression;
        Debug.Log($"[GlobalDifficultyState] Calculated enemy count for Room {room.roomIndex} (Distance: {room.distanceFromStart}, Type: {room.roomType}, Cleared: {_clearedRooms}/{_totalRooms}) => Raw: {raw}");
        int result = Mathf.RoundToInt(raw);
        result = Mathf.Clamp(result, 1, room.maxEnemies);

        currentEnemyCount = result; // Untuk debug
        return result;
    }

    public int GetEnemyLevelForRoom(Room room)
    {
        if (room == null)
            return Mathf.Max(1, baseEnemyLevel);

        int level = baseEnemyLevel + Mathf.Max(0, room.distanceFromStart) * Mathf.Max(0, levelsPerRoomDepth);

        switch (room.roomType)
        {
            case RoomType.Elite:
                level += Mathf.Max(0, eliteRoomLevelBonus);
                break;
            case RoomType.Boss:
                level += Mathf.Max(0, bossRoomLevelBonus);
                break;
        }

        return Mathf.Clamp(level, 1, Mathf.Max(1, maxEnemyLevel));
    }

    private float GetProgressionMultiplier()
    {
        float progressionT = GetProgressionT();
        return Mathf.Lerp(progressionMultiplierMin, progressionMultiplierMax, progressionT);
    }

    private void HandleRoomCleared(Room room)
    {
        RegisterRoomCleared(room);
    }

    public void RegisterRoomCleared(Room room)
    {
        if (room == null)
            return;

        if (!_countedClearedRoomIds.Add(room.roomIndex))
            return;

        _clearedRooms = _countedClearedRoomIds.Count;
    }

    public EnemyDifficultySnapshot GetEnemyDifficultySnapshot()
    {
        float Clamp(float v) => Mathf.Clamp(v, enemyMultiplierMin, enemyMultiplierMax);
        float adjDamage = Clamp(enemyDamageMultiplier * GetEnemyStatProgressionMultiplier(progressionEnemyDamageMax));
        float adjHealth = Clamp(enemyHealthMultiplier * GetEnemyStatProgressionMultiplier(progressionEnemyHealthMax));
        float adjSpeed = Clamp(enemySpeedMultiplier * GetEnemyStatProgressionMultiplier(progressionEnemySpeedMax));
        float adjAttackSpeed = Clamp(enemyAttackSpeedMultiplier * GetEnemyStatProgressionMultiplier(progressionEnemyAttackSpeedMax));
        float adjAggro = Clamp(enemyAggroRangeMultiplier * GetEnemyStatProgressionMultiplier(progressionEnemyAggroMax));
        // show only 
        currentDamageMultiplier = adjDamage;
        currentHealthMultiplier = adjHealth;
        currentSpeedMultiplier = adjSpeed;
        currentAttackSpeedMultiplier = adjAttackSpeed;
        currentAggroMultiplier = adjAggro;
        return new EnemyDifficultySnapshot
        {
            damage = adjDamage,
            health = adjHealth,
            speed = adjSpeed,
            attackSpeed = adjAttackSpeed,
            aggro = adjAggro
        };
    }

    public void SetEnemyMultiplier(string stat, float value)
    {
        float v = Mathf.Clamp(value, enemyMultiplierMin, enemyMultiplierMax);
        switch (stat)
        {
            case "damage": enemyDamageMultiplier = v; break;
            case "health": enemyHealthMultiplier = v; break;
            case "speed": enemySpeedMultiplier = v; break;
            case "attackSpeed": enemyAttackSpeedMultiplier = v; break;
            case "aggro": enemyAggroRangeMultiplier = v; break;
        }
    }

    private void ResetProgression()
    {
        _countedClearedRoomIds.Clear();
        _clearedRooms = 0;
    }

    private float GetCompletionRatio()
    {
        if (_totalRooms <= 0)
            return 0f;

        return Mathf.Clamp01((float)_clearedRooms / _totalRooms);
    }

    private float GetProgressionT()
    {
        return Mathf.Clamp01(clearedRoomToMultiplier.Evaluate(GetCompletionRatio()));
    }

    private float GetEnemyStatProgressionMultiplier(float maxMultiplier)
    {
        if (!scaleEnemyStatsWithProgression)
            return 1f;

        return Mathf.Lerp(1f, Mathf.Max(1f, maxMultiplier), GetProgressionT());
    }
}

public struct EnemyDifficultySnapshot
{
    public float damage;
    public float health;
    public float speed;
    public float attackSpeed;
    public float aggro;
}
