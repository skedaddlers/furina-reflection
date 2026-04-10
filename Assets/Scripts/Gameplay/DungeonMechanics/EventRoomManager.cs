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

    [Header("Give Item Settings")]
    public List<Item> itemsToGrant = new List<Item>();
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
    private SkillManager playerSkillManager;
    private Inventory playerInventory;
    private readonly Dictionary<int, SkillSlot> upgradeTargetsByChoiceIndex = new Dictionary<int, SkillSlot>();
    private System.Action activeBattleClearHandler;

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
            playerInventory = playerGO.GetComponent<Inventory>();
            playerSkillManager = playerGO.GetComponent<SkillManager>();
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
        List<GameEventOption> choicesForUI = BuildChoicesForUI(currentChoices);

        if (eventUI != null)
        {
            eventUI.ShowChoices(choicesForUI, this);
        }
        else
        {
            Debug.LogWarning("[EventRoom] eventUI belum di-assign, auto pilih opsi pertama (debug).");
            if (currentChoices.Count > 0)
            {
                ExecuteEvent(currentChoices[0], 0);
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

        ExecuteEvent(chosen, index);

        // untuk semua event non-battle, room dianggap clear langsung
        // untuk StartBattle, room akan clear otomatis lewat sistem Room saat semua musuh mati
        if (chosen.eventType != GameEventType.StartBattle)
        {
            eventResolved = true;
            if (parentRoom != null)
            {
                parentRoom.isCleared = true;
                GlobalDifficultyState.Instance?.RegisterRoomCleared(parentRoom);
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
    private void ExecuteEvent(GameEventOption option, int choiceIndex = -1)
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
                ResolveUpgradeItem(option, choiceIndex);
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
                UIManager.Instance.ShowNotification($"You rolled a {roll} and failed! You take {option.curseDamage} damage.", 3f);
                playerHealth.TakeDamage(
                    option.curseDamage,
                    isCrit: false,
                    source: DamageSource.Skill,
                    applyStagger: false
                );
            }
        }
    }

    // 2. Straight up gets gold/exp/health
    private void ResolveFlatReward(GameEventOption option)
    {
        string rewardText = "";
        if (playerStats != null)
        {
            if (option.goldAmount > 0)
            {
                playerStats.AddGold(option.goldAmount);
                rewardText += $"+{option.goldAmount} Gold\n";
            }
            if (option.xpAmount > 0)
            {
                playerStats.AddXP(option.xpAmount);
                rewardText += $"+{option.xpAmount} XP\n";
            }
        }

        if (playerHealth != null && option.healAmount > 0)
        {
            playerHealth.Heal(option.healAmount); // pastikan Health punya Heal, kalau belum bisa ganti ke TakeDamage(-heal)
            rewardText += $"+{option.healAmount} HP\n";
        }

        UIManager.Instance.ShowNotification($"You Received your Reward!\n{rewardText}", 2f);
    }

    // 3. Gives buffs (30% more damage) for a few seconds
    private void ResolveTemporaryBuff(GameEventOption option)
    {
        playerStats?.ApplyTemporaryDamageBuff(option.damageBuffMultiplier, option.buffDuration);
        UIManager.Instance.ShowNotification($"Damage increased by {(option.damageBuffMultiplier - 1f) * 100f}% for {option.buffDuration} seconds!", 3f);
    }

    // 4. Gets items 
    private void ResolveGrantItem(GameEventOption option)
    {
        if (playerInventory == null)
        {
            Debug.LogWarning("[Event-GrantItem] Inventory player tidak ditemukan.");
            UIManager.Instance.ShowNotification("No inventory found.", 2.5f);
            return;
        }

        int grantedCount = 0;
        foreach (var item in option.itemsToGrant)
        {
            if (item != null)
            {
                GameObject itemGO = Instantiate(item.gameObject);
                itemGO.GetComponent<Item>().SetVisibleInWorld(false); // sembunyiin dulu di world
                bool added = playerInventory.TryAddItem(itemGO.GetComponent<Item>());
                if (added) grantedCount++;
            }
        }

        if (grantedCount > 0)
        {
            UIManager.Instance.ShowNotification($"You received {grantedCount} item(s)!", 3f);
        }
    }

    // 5. Upgrades skills/weapons
    private void ResolveUpgradeItem(GameEventOption option, int choiceIndex)
    {
        if (playerSkillManager == null)
        {
            Debug.LogWarning("[Event-UpgradeItem] SkillManager player tidak ditemukan.");
            UIManager.Instance.ShowNotification("No skill system found.", 2.5f);
            return;
        }

        SkillSlot targetSlot = null;
        if (!upgradeTargetsByChoiceIndex.TryGetValue(choiceIndex, out targetSlot) || targetSlot == null || targetSlot.skill == null)
        {
            List<SkillSlot> upgradeableSlots = playerSkillManager.GetUpgradeableOwnedSkills();
            if (upgradeableSlots.Count > 0)
                targetSlot = upgradeableSlots[Random.Range(0, upgradeableSlots.Count)];
        }

        if (targetSlot == null || targetSlot.skill == null)
        {
            Debug.Log("[Event-UpgradeItem] Tidak ada skill milik player yang bisa di-upgrade.");
            UIManager.Instance.ShowNotification("Nothing happens...", 2.5f);
            return;
        }

        if (playerSkillManager.TryUpgradeSkill(targetSlot, out SkillBase beforeUpgrade, out SkillBase afterUpgrade))
        {
            string beforeName = beforeUpgrade != null ? beforeUpgrade.skillName : "Skill";
            string afterName = afterUpgrade != null ? afterUpgrade.skillName : beforeName;
            string resultText = beforeName == afterName
                ? $"Upgraded: {afterName}"
                : $"Upgraded: {beforeName} -> {afterName}";

            UIManager.Instance.ShowNotification(resultText, 3f);
            Debug.Log($"[Event-UpgradeItem] {resultText}");
            return;
        }

        Debug.Log("[Event-UpgradeItem] Upgrade gagal dijalankan.");
        UIManager.Instance.ShowNotification("Skill upgrade failed.", 2.5f);

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

        UIManager.Instance.ShowNotification($"Prepare for battle! Complete to gain rewards.", 3f);

        // pastikan room di-mark belum clear supaya BeginCombat jalan normal
        parentRoom.isCleared = false;

        var diff = GlobalDifficultyState.Instance;
        int scaledEnemyCount = diff?.GetEnemyCountForRoom(parentRoom, null, int.MaxValue) ?? option.battleEnemyCount;
        int eventEnemyMinimum = Mathf.Max(1, option.battleEnemyCount);
        int eventEnemyCap = Mathf.Max(parentRoom.maxEnemies, eventEnemyMinimum, scaledEnemyCount);

        parentRoom.ConfigureCombatEnemyBudget(eventEnemyMinimum, eventEnemyCap);

        if (activeBattleClearHandler != null)
            parentRoom.OnRoomClearedLocal -= activeBattleClearHandler;

        // mulai combat di room ini
        activeBattleClearHandler = CreateEventBattleClearHandler(option);
        parentRoom.OnRoomClearedLocal += activeBattleClearHandler;
        parentRoom.BeginCombat();
    }

    private System.Action CreateEventBattleClearHandler(GameEventOption option) => () =>
    {
        // skill upgrade / give health / gold / xp sebagai reward battle
        ResolveFlatReward(option);

        // Unsubscribe supaya event ini gak ke-trigger lagi kalau somehow room ini dipakai lagi
        if (parentRoom != null && activeBattleClearHandler != null)
        {
            parentRoom.OnRoomClearedLocal -= activeBattleClearHandler;
        }

        activeBattleClearHandler = null;
    };



    private List<GameEventOption> BuildChoicesForUI(List<GameEventOption> sourceChoices)
    {
        upgradeTargetsByChoiceIndex.Clear();

        List<GameEventOption> result = new List<GameEventOption>();
        if (sourceChoices == null) return result;

        for (int i = 0; i < sourceChoices.Count; i++)
        {
            GameEventOption source = sourceChoices[i];
            if (source == null) continue;

            GameEventOption uiOption = CloneEventOption(source);
            if (source.eventType == GameEventType.UpgradeItem)
            {
                SkillSlot previewSlot = PickRandomUpgradeableSkillSlot();
                if (previewSlot != null && previewSlot.skill != null)
                {
                    upgradeTargetsByChoiceIndex[i] = previewSlot;
                    string upgradePreview = BuildUpgradePreviewText(previewSlot.skill);
                    if (!string.IsNullOrEmpty(upgradePreview))
                    {
                        uiOption.description = string.IsNullOrEmpty(uiOption.description)
                            ? upgradePreview
                            : $"{uiOption.description}\n{upgradePreview}";
                    }
                }
                else
                {
                    string noTargetText = "Nothing happens...";
                    uiOption.description = string.IsNullOrEmpty(uiOption.description)
                        ? noTargetText
                        : $"{uiOption.description}\n{noTargetText}";
                }
            }

            result.Add(uiOption);
        }

        return result;
    }

    private GameEventOption CloneEventOption(GameEventOption source)
    {
        return new GameEventOption
        {
            id = source.id,
            displayName = source.displayName,
            description = source.description,
            eventType = source.eventType,
            weight = source.weight,
            goldAmount = source.goldAmount,
            xpAmount = source.xpAmount,
            healAmount = source.healAmount,
            diceSides = source.diceSides,
            successThreshold = source.successThreshold,
            curseDamage = source.curseDamage,
            damageBuffMultiplier = source.damageBuffMultiplier,
            buffDuration = source.buffDuration,
            battleEnemyCount = source.battleEnemyCount
        };
    }

    private SkillSlot PickRandomUpgradeableSkillSlot()
    {
        if (playerSkillManager == null) return null;

        List<SkillSlot> upgradeableSlots = playerSkillManager.GetUpgradeableOwnedSkills();
        if (upgradeableSlots.Count == 0)
            return null;

        return upgradeableSlots[Random.Range(0, upgradeableSlots.Count)];
    }

    private string BuildUpgradePreviewText(SkillBase skill)
    {
        if (skill == null) return string.Empty;

        string currentName = string.IsNullOrEmpty(skill.skillName) ? "Unknown Skill" : skill.skillName;
        if (skill.nextLevelSkill != null)
        {
            string upgradeDesc = !string.IsNullOrEmpty(skill.upgradeDescription) ? skill.upgradeDescription : "No upgrade description.";
            return $"Upgrades {currentName}:\n{upgradeDesc}";
        }

        return $"{currentName}";
    }
}
