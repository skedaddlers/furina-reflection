using UnityEngine;
using System;

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

    public event Action<int, int> onManaChanged; // (current, max)
    public event Action<float, float> onStaminaChanged; // (current, max)

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
    }

    void Update()
    {
        HandleStaminaRegen();
        // Regen pasif
        if (_currentMana < maxMana)
        {
            _manaRegenBuffer += manaRegenPerSecond * Time.deltaTime;
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
        if (_currentStamina < amount) return false;

        _currentStamina -= amount;
        if (resetDelay) _staminaRegenDelayTimer = staminaRegenDelay;
        onStaminaChanged?.Invoke(_currentStamina, maxStamina);
        return true;
    }

    public void SpendStamina(float amount, bool resetDelay = true)
    {
        if (amount <= 0f) return;
        _currentStamina = Mathf.Max(0f, _currentStamina - amount);
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

    public float RollDamage(float baseDmg)
    {
        // crit sederhana
        bool isCrit = UnityEngine.Random.value < critRate;
        return isCrit ? baseDmg * critMultiplier : baseDmg;
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
        // You can add more logic here for what happens when the player levels up
    }
}
