using UnityEngine;
using System.Collections.Generic;

public enum GameEventType
{
    DiceGamble,     // 1. Roll dice, reward atau curse
    FlatReward,     // 2. Dapet gold/xp/hp langsung
    TemporaryBuff,  // 3. Buff sementara (damage up, dll)
    GrantItem,      // 4. Dapet skill/weapon
    UpgradeItem,    // 5. Upgrade skill/weapon
    StartBattle     // 6. Mulai battle di room ini
}

[System.Serializable]
public class GameEventOption
{
    public string id;
    public string displayName;
    [TextArea]
    public string description;

    public GameEventType eventType;

    [Header("Frequency / Weight")]
    [Tooltip("Semakin tinggi, semakin sering event ini kepilih")]
    public float weight = 1f;

    [Header("Numeric Rewards (dipakai FlatReward / Dice success)")]
    public int goldAmount;
    public int xpAmount;
    public int healAmount;

    [Header("Dice Gamble Settings")]
    [Tooltip("Berapa sisi dadu (contoh 6)")]
    public int diceSides = 6;
    [Tooltip("Kalau hasil > threshold = sukses, else gagal")]
    public int successThreshold = 4;
    [Tooltip("Damage HP ke player saat gagal (curse)")]
    public int curseDamage = 10;

    [Header("Temporary Buff Settings")]
    [Tooltip("Multiplier damage, misal 1.3 = +30%")]
    public float damageBuffMultiplier = 1.3f;
    public float buffDuration = 10f;

    [Header("Battle Event Settings")]
    [Tooltip("Berapa musuh yang akan dispawn saat event battle")]
    public int battleEnemyCount = 5;
}

public class EventRoomManager : MonoBehaviour
{
    [Header("Setup")]
    public Room parentRoom;
    public List<GameEventOption> availableEvents = new List<GameEventOption>();
    public int choicesPerEvent = 3;
    public EventRoomUI eventUI;

    private bool eventResolved = false;
    private List<GameEventOption> currentChoices = new List<GameEventOption>();

    // cache ke player
    private PlayerStats playerStats;
    private Health playerHealth;

    private void Awake()
    {
        if (parentRoom == null)
            parentRoom = GetComponent<Room>();  
        
        if (eventUI == null)
        {
            eventUI = UIManager.Instance?.eventRoomUI;
        }

        playerStats = PlayerStats.Instance;
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            playerHealth = playerGO.GetComponent<Health>();
        }
    }

    public void StartEventRoom()
    {
        if (eventResolved)
        {
            Debug.Log("[EventRoom] Event sudah diselesaikan.");
            return;
        }

        if (availableEvents == null || availableEvents.Count == 0)
        {
            Debug.LogWarning("[EventRoom] Tidak ada event yang dikonfigurasi!");
            return;
        }

        // Pick 3 event berbeda by weighted random
        currentChoices = PickRandomEvents(choicesPerEvent);

        if (eventUI != null)
        {
            eventUI.ShowChoices(currentChoices, this);
        }
        else
        {
            Debug.LogWarning("[EventRoom] eventUI belum di-assign, auto pilih opsi pertama (debug).");
            if (currentChoices.Count > 0)
            {
                ExecuteEvent(currentChoices[0]);
                eventResolved = true;
            }
        }
    }

    public void OnChoiceSelected(int index)
    {
        if (eventResolved) return;
        if (currentChoices == null || index < 0 || index >= currentChoices.Count) return;

        var chosen = currentChoices[index];
        Debug.Log($"[EventRoom] Player memilih event: {chosen.displayName}");

        // tutup UI dulu
        if (eventUI != null)
            eventUI.Hide();

        ExecuteEvent(chosen);

        // untuk semua event non-battle, room dianggap clear langsung
        // untuk StartBattle, room akan clear otomatis lewat sistem Room saat semua musuh mati
        if (chosen.eventType != GameEventType.StartBattle)
        {
            eventResolved = true;
            if (parentRoom != null)
            {
                parentRoom.isCleared = true;
                if (parentRoom.eventTrigger) parentRoom.eventTrigger.gameObject.SetActive(false);
                parentRoom.UnlockAllDoors();
            }
        }
        else
        {
            // battle event: anggap eventResolved secara logis,
            // tapi clear room nya nunggu musuh mati
            if (parentRoom != null)
            {
                if (parentRoom.eventTrigger) parentRoom.eventTrigger.gameObject.SetActive(false);
            }
            eventResolved = true;
        }
    }

    // =========================================
    //  PICK 3 EVENT BERBEDA DENGAN WEIGHT
    // =========================================
    private List<GameEventOption> PickRandomEvents(int count)
    {
        List<GameEventOption> candidates = new List<GameEventOption>(availableEvents);
        List<GameEventOption> result = new List<GameEventOption>();

        count = Mathf.Min(count, candidates.Count);

        for (int i = 0; i < count; i++)
        {
            var chosen = ChooseOneByWeight(candidates);
            if (chosen == null) break;

            result.Add(chosen);
            candidates.Remove(chosen);
        }

        return result;
    }

    private GameEventOption ChooseOneByWeight(List<GameEventOption> pool)
    {
        float totalWeight = 0f;
        foreach (var evt in pool)
        {
            if (evt != null && evt.weight > 0f)
                totalWeight += evt.weight;
        }
        if (totalWeight <= 0f) return null;

        float r = Random.value * totalWeight;
        foreach (var evt in pool)
        {
            if (evt == null || evt.weight <= 0f) continue;
            if (r < evt.weight)
                return evt;
            r -= evt.weight;
        }
        return pool[pool.Count - 1];
    }

    // =========================================
    //  EKSEKUSI EVENT
    // =========================================
    private void ExecuteEvent(GameEventOption option)
    {
        switch (option.eventType)
        {
            case GameEventType.DiceGamble:
                ResolveDiceGamble(option);
                break;

            case GameEventType.FlatReward:
                ResolveFlatReward(option);
                break;

            case GameEventType.TemporaryBuff:
                ResolveTemporaryBuff(option);
                break;

            case GameEventType.GrantItem:
                ResolveGrantItem(option);
                break;

            case GameEventType.UpgradeItem:
                ResolveUpgradeItem(option);
                break;

            case GameEventType.StartBattle:
                ResolveStartBattle(option);
                break;
        }
    }

    // 1. Roll dice, > threshold = reward, else curse
    private void ResolveDiceGamble(GameEventOption option)
    {
        int sides = Mathf.Max(2, option.diceSides);
        int roll = Random.Range(1, sides + 1);

        Debug.Log($"[Event-Dice] Player roll: {roll}/{sides}");

        if (roll > option.successThreshold)
        {
            Debug.Log("[Event-Dice] SUCCESS! Applying reward.");
            ResolveFlatReward(option);
        }
        else
        {
            Debug.Log("[Event-Dice] FAIL! Applying curse.");
            if (playerHealth != null && option.curseDamage > 0)
            {
                playerHealth.TakeDamage(option.curseDamage);
            }
            else
            {
                // fallback
                Debug.Log("[Event-Dice] No Health reference, curse skipped.");
            }
        }
    }

    // 2. Straight up gets gold/exp/health
    private void ResolveFlatReward(GameEventOption option)
    {
        if (playerStats != null)
        {
            if (option.goldAmount > 0)
                playerStats.AddGold(option.goldAmount);
            if (option.xpAmount > 0)
                playerStats.AddXP(option.xpAmount);
        }

        if (playerHealth != null && option.healAmount > 0)
        {
            playerHealth.Heal(option.healAmount); // pastikan Health punya Heal, kalau belum bisa ganti ke TakeDamage(-heal)
        }

        Debug.Log($"[Event-Reward] Gold+{option.goldAmount}, XP+{option.xpAmount}, Heal+{option.healAmount}");
    }

    // 3. Gives buffs (30% more damage) for a few seconds
    private void ResolveTemporaryBuff(GameEventOption option)
    {
        // Di sini kita cuma log + TODO hook ke sistem combat-mu
        Debug.Log($"[Event-Buff] Apply damage buff x{option.damageBuffMultiplier} for {option.buffDuration} seconds (TODO: hook ke player combat)");

        // Contoh kalau nanti kamu punya BuffManager di player:
        // player.GetComponent<PlayerBuffManager>()?.ApplyDamageBuff(option.damageBuffMultiplier, option.buffDuration);
    }

    // 4. Gets skills/weapons
    private void ResolveGrantItem(GameEventOption option)
    {
        Debug.Log("[Event-GrantItem] TODO: grant random skill/weapon ke player.");

        // Contoh hook:
        // SkillManager.Instance?.GrantRandomSkill();
        // atau WeaponManager.Instance?.GrantRandomWeapon();
    }

    // 5. Upgrades skills/weapons
    private void ResolveUpgradeItem(GameEventOption option)
    {
        Debug.Log("[Event-UpgradeItem] TODO: upgrade random skill/weapon player.");

        // Contoh hook:
        // SkillManager.Instance?.UpgradeRandomSkill();
    }

    // 6. Initiate Battle with X, gets rewards
    private void ResolveStartBattle(GameEventOption option)
    {
        if (parentRoom == null)
        {
            Debug.LogWarning("[Event-StartBattle] parentRoom null.");
            return;
        }

        Debug.Log($"[Event-StartBattle] Starting battle with {option.battleEnemyCount} enemies in event room.");

        // pastikan room di-mark belum clear supaya BeginCombat jalan normal
        parentRoom.isCleared = false;

        // override enemyCount khusus battle ini
        parentRoom.enemyCount = Mathf.Max(1, option.battleEnemyCount);

        // mulai combat di room ini
        parentRoom.BeginCombat();
    }
}
