using UnityEngine;
using System;

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    [Header("Core")]
    public int level = 1;
    public float baseAttack = 10f;
    public float baseDefense = 0f;
    public float moveSpeed = 6f;

    [Header("Crit & Luck")]
    [Range(0f, 1f)] public float critRate = 0.1f;     // 10%
    public float critMultiplier = 1.5f;               // 150%
    public float luck = 0f;

    [Header("Mana")]
    public int maxMana = 10;
    public float manaRegenPerSecond = 1.0f;

    [SerializeField] private int _currentMana;
    public int CurrentMana => _currentMana;

    public event Action<int, int> onManaChanged; // (current, max)

    void Awake()
    {
        _currentMana = maxMana;
        onManaChanged?.Invoke(_currentMana, maxMana);
    }

    void Update()
    {
        // Regen pasif
        if (_currentMana < maxMana)
        {
            float regen = manaRegenPerSecond * Time.deltaTime;
            _currentMana = Mathf.Min(maxMana, _currentMana + Mathf.FloorToInt(regen));
            onManaChanged?.Invoke(_currentMana, maxMana);
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
}
