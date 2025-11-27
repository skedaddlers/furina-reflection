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
        health = GetComponent<Health>();
    }

    void Update()
    {
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