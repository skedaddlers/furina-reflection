using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;
    [Header("Core")]
    public int level = 1;
    public Health health;
    public float baseAttack = 10f;
    public float baseDefense = 0f;
    public float moveSpeed = 6f;

    [Header("Crit & Luck")]
    [Range(0f, 1f)] public float critRate = 0.1f;     // 10%
    public float critMultiplier = 1.5f;               // 150%
    public float luck = 0f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaRegenPerSecond = 20f;
    public float staminaRegenDelay = 1.0f;
    public float staminaConsumptionReductionPercent = 0f; // e.g., 0.2 for 20% reduction
    [SerializeField] private float _currentStamina;
    public float CurrentStamina => _currentStamina;
    private float _staminaRegenDelayTimer = 0f;
    private float _staminaRegenBuffer = 0f;

    [Header("Mana")]
    public int maxMana = 10;
    public int MaxMana => maxMana;
    public float manaRegenPerSecond = 1.0f;

    [Header("Leveling")]
    public int currentXP = 0;
    public int xpToNextLevel = 100;
    public float xpMultiplier = 1.0f;
    public float xpGrowthRate = 1.2f;
    public LevelManager levelManager;
    public UpgradeManager upgradeManager;

    [Header("Economy")]
    public int gold = 0;
    public int Gold
    {
        get => gold;
        set => gold = Mathf.Max(0, value);
    }

    [SerializeField] private int _currentMana;
    public int CurrentMana => _currentMana;

    private float _manaRegenBuffer = 0f;
    private readonly Dictionary<int, float> _externalManaRegenMultipliers = new Dictionary<int, float>();

    public event Action<int, int> onManaChanged; // (current, max)
    public event Action<float, float> onStaminaChanged; // (current, max)

    [SerializeField] private float _currentDamageBuffMultiplier = 1f;
    private float _damageBuffTimer = 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        _currentMana = maxMana;
        onManaChanged?.Invoke(_currentMana, maxMana);
        _currentStamina = maxStamina;
        onStaminaChanged?.Invoke(_currentStamina, maxStamina);
        health = GetComponent<Health>();
        levelManager = GetComponent<LevelManager>();
        upgradeManager = GetComponent<UpgradeManager>();
    }

    void OnDestroy()
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
        Destroy(go);
    }

    void Update()
    {
        HandleStaminaRegen();
        // Regen pasif
        float effectiveManaRegen = GetEffectiveManaRegenPerSecond();
        if (_currentMana < maxMana && effectiveManaRegen > 0f)
        {
            _manaRegenBuffer += effectiveManaRegen * Time.deltaTime;
            int regenAmount = Mathf.FloorToInt(_manaRegenBuffer);
            if (regenAmount > 0)
            {
                _currentMana = Mathf.Min(maxMana, _currentMana + regenAmount);
                _manaRegenBuffer -= regenAmount;
                onManaChanged?.Invoke(_currentMana, maxMana);
            }
        }
    }

    void HandleStaminaRegen()
    {
        if (_currentStamina >= maxStamina) return;

        if (_staminaRegenDelayTimer > 0f)
        {
            _staminaRegenDelayTimer -= Time.deltaTime;
            return;
        }

        _staminaRegenBuffer += staminaRegenPerSecond * Time.deltaTime;
        float regenAmount = _staminaRegenBuffer;
        if (regenAmount >= 0.5f) // apply in small batches for smoother slider
        {
            _currentStamina = Mathf.Min(maxStamina, _currentStamina + regenAmount);
            _staminaRegenBuffer -= regenAmount;
            onStaminaChanged?.Invoke(_currentStamina, maxStamina);
        }
    }

    public bool TrySpendStamina(float amount, bool resetDelay = true)
    {
        if (amount <= 0f) return true;
        if (_currentStamina < amount * (1f - staminaConsumptionReductionPercent)) return false;

        float reducedAmount = amount * (1f - staminaConsumptionReductionPercent);
        _currentStamina -= reducedAmount;
        if (resetDelay) _staminaRegenDelayTimer = staminaRegenDelay;
        onStaminaChanged?.Invoke(_currentStamina, maxStamina);
        return true;
    }

    public void SpendStamina(float amount, bool resetDelay = true)
    {
        if (amount <= 0f) return;
        float reducedAmount = amount * (1f - staminaConsumptionReductionPercent);
        _currentStamina = Mathf.Max(0f, _currentStamina - reducedAmount);
        if (resetDelay) _staminaRegenDelayTimer = staminaRegenDelay;
        onStaminaChanged?.Invoke(_currentStamina, maxStamina);
    }

    public void AddStamina(float amount)
    {
        if (amount <= 0f) return;
        _currentStamina = Mathf.Min(maxStamina, _currentStamina + amount);
        onStaminaChanged?.Invoke(_currentStamina, maxStamina);
    }

    public void ResetStaminaRegenDelay()
    {
        _staminaRegenDelayTimer = staminaRegenDelay;
    }

    public bool TrySpendMana(int amount)
    {
        if (amount <= 0) return true;
        if (_currentMana < amount) return false;
        _currentMana -= amount;
        onManaChanged?.Invoke(_currentMana, maxMana);
        return true;
    }

    public void UseMana(int amount)
    {
        if (amount <= 0) return;
        _currentMana = Mathf.Max(0, _currentMana - amount);
        onManaChanged?.Invoke(_currentMana, maxMana);
    }

    public void AddMana(int amount)
    {
        if (amount <= 0) return;
        _currentMana = Mathf.Min(maxMana, _currentMana + amount);
        onManaChanged?.Invoke(_currentMana, maxMana);
    }

    public void SetExternalManaRegenMultiplier(int sourceId, float multiplier)
    {
        if (sourceId == 0) return;
        _externalManaRegenMultipliers[sourceId] = Mathf.Max(0f, multiplier);
    }

    public void ClearExternalManaRegenMultiplier(int sourceId)
    {
        if (sourceId == 0) return;
        _externalManaRegenMultipliers.Remove(sourceId);
    }

    public float GetEffectiveManaRegenPerSecond()
    {
        return Mathf.Max(0f, manaRegenPerSecond * GetCombinedExternalManaRegenMultiplier());
    }

    private float GetCombinedExternalManaRegenMultiplier()
    {
        float combined = 1f;
        foreach (var kv in _externalManaRegenMultipliers)
        {
            combined *= Mathf.Max(0f, kv.Value);
        }
        return combined;
    }

    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        currentXP += amount;
        UIManager.Instance.statsUI.UpdateXPUI(currentXP, xpToNextLevel);
        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            LevelUp();
        }
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        UIManager.Instance.statsUI.UpdateGoldUI(Gold);
    }

    public void SpendGold(int amount)
    {
        if (amount <= 0) return;
        Gold = Mathf.Max(0, Gold - amount);
        UIManager.Instance.statsUI.UpdateGoldUI(Gold);
    }

    public bool CanAfford(int amount)
    {
        return Gold >= amount;
    }

    private void LevelUp()
    {
        level++;
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * xpGrowthRate); // Increase XP needed for next level by growth rate
        UIManager.Instance.statsUI.UpdateLevelUI(level);
        UIManager.Instance.statsUI.UpdateXPUI(currentXP, xpToNextLevel);
        levelManager.OnLevelUp(level);
    }

    public bool HasEnoughStamina(float amount)
    {
        float reducedAmount = amount * (1f - staminaConsumptionReductionPercent);
        return _currentStamina >= reducedAmount;
    }

    public float GetCurrentDamageBuffMultiplier()
    {
        return _currentDamageBuffMultiplier;
    }
    public void ApplyTemporaryDamageBuff(float multiplier, float duration)
    {
        _currentDamageBuffMultiplier = multiplier;
        _damageBuffTimer = duration;
        StopAllCoroutines();
        StartCoroutine(DamageBuffCoroutine());
    }

    private IEnumerator DamageBuffCoroutine()
    {
        while (_damageBuffTimer > 0f)
        {
            _damageBuffTimer -= Time.deltaTime;
            yield return null;
        }
        _currentDamageBuffMultiplier = 1f;
    }

    // Upgrade methods called by LevelUpUI
    public void UpgradeHealth()
    {
        float healthIncrease = upgradeManager.GetHealthUpgradeAmount();
        health.SetMaxHealth(health.maxHealth + healthIncrease);
    }

    public void UpgradeAttack()
    {
        baseAttack += upgradeManager.GetAttackUpgradeAmount();
    }

    public void UpgradeDefense()
    {
        baseDefense += upgradeManager.GetDefenseUpgradeAmount();
    }

    public void UpgradeMana()
    {
        maxMana = Mathf.RoundToInt(maxMana + upgradeManager.GetMaxManaUpgradeAmount());
        _currentMana = maxMana; // Refill mana on upgrade
        onManaChanged?.Invoke(_currentMana, maxMana);
        manaRegenPerSecond += upgradeManager.GetManaRegenUpgradeAmount();
    }

    public void UpgradeMoveSpeed()
    {
        moveSpeed += upgradeManager.GetMoveSpeedUpgradeAmount(); // Increase move speed by the upgrade amount
        maxStamina += upgradeManager.GetStaminaUpgradeAmount(); // Also increase stamina by a scaled amount of the move speed upgrade
    }
    

    public void UpgradeCrit()
    {
        float critRateIncrease = upgradeManager.GetCritRateUpgradeAmount();
        float critMultiplierIncrease = upgradeManager.GetCritMultiplierUpgradeAmount();
        critRate = Mathf.Min(1f, critRate + critRateIncrease); // Increase crit rate by an additional 5%, cap at 100%
        critMultiplier += critMultiplierIncrease; // Increase crit multiplier by the upgrade amount
    }


}
