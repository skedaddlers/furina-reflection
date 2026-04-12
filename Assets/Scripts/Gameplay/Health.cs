using UnityEngine;
using System;
using System.Collections;
using DDAMAPEKitFramework;

public enum DamageSource
{
    Melee,
    Ranged,
    Skill
}

public class Health : MonoBehaviour
{
    public float maxHealth = 100;
    public float shieldAmount = 0;
    public float maxShield = 100;
    public AudioClip hitSFX;
    private float currentHealth;
    public float CurrentHealth => currentHealth;
    private bool isInvulnerable = false;
    public bool IsInvulnerable => isInvulnerable;
    private readonly System.Collections.Generic.Dictionary<int, float> _externalHealingMultipliers =
        new System.Collections.Generic.Dictionary<int, float>();
    [Header("Stagger Defaults")]
    [SerializeField] private bool enableStaggerOnHit = true;
    [SerializeField] private float defaultMeleeStaggerDuration = 0.2f;
    [SerializeField] private float defaultRangedStaggerDuration = 0.14f;
    [SerializeField] private float defaultSkillStaggerDuration = 0.18f;

    [Header("Voice Effects")]
    public AudioClip[] hitVoiceLines; // optional, untuk suara saat kena hit
    public float hitVoiceLineChance = 0.5f; // peluang untuk memutar suara saat kena hit

    // on death event
    public Action onDeath;
    public Action<float, float> onHealthChanged; // (current, max)
    public Action onShieldDestroyed; // dipanggil saat shield habis karena serangan

    // Shield system - returns remaining damage after absorption
    public Func<float, float> onTakeDamage;

    void Awake()
    {
        currentHealth = maxHealth;
        maxShield = maxHealth;
    }

    public void TakeDamage(
        float amount,
        bool isCrit = false,
        DamageSource source = DamageSource.Melee,
        bool applyStagger = true,
        float staggerDuration = -1f,
        bool causesKnockback = false,
        float knockbackDistance = 0f,
        Transform hitInstigator = null,
        bool bypassShield = false
    )
    {
        if (currentHealth <= 0) return;
        if (isInvulnerable)
        {
            if (CompareTag("Player"))
            {
                CombatEventManager.RaiseSuccessfulDodge();
                PlayerActionTracker.Instance.RegisterDodge();
            }
            // Debug.Log($"{gameObject.name} is invulnerable. Damage ignored.");
            return;
        }

        float finalDamage = amount;

        // Let shield absorb damage first unless this hit explicitly bypasses shields.
        if (!bypassShield && shieldAmount > 0)
        {
            float damageAbsorbed = Mathf.Min(shieldAmount, finalDamage);
            shieldAmount -= damageAbsorbed;
            if (shieldAmount <= 0)
            {
                shieldAmount = 0;
                onShieldDestroyed?.Invoke();
            }
            finalDamage -= damageAbsorbed;
            // Debug.Log($"{gameObject.name} shield absorbed {damageAbsorbed} damage. Remaining shield: {shieldAmount}");
        }

        // Apply remaining damage to health
        if (finalDamage > 0)
        {
            currentHealth -= finalDamage;
            AudioManager.Instance?.PlaySFXNoOverlap(hitSFX, randomizePitch: true);
            // Debug.Log($"{gameObject.name} took {finalDamage} damage. Health: {currentHealth}");
            Enemy enemy = GetComponent<Enemy>();
            if (enemy != null)
            {
                if (source == DamageSource.Melee)
                {
                    CombatEventManager.RaiseMeleeAttack(finalDamage);
                }
                else if (source == DamageSource.Ranged)
                {
                    CombatEventManager.RaiseRangedAttack(finalDamage);
                }
                else if (source == DamageSource.Skill)
                {
                    CombatEventManager.RaiseSkillAttack(finalDamage);
                }
                
                if (enemy.healthBar != null)
                {
                    UIManager.Instance.damageNumberUI.ShowDamagePopup(finalDamage, enemy.healthBar.position, isCrit);
                    StartCoroutine(HitFlash(enemy));
                }
                else
                {
                    UIManager.Instance.damageNumberUI.ShowDamagePopup(finalDamage, transform.position + Vector3.up, isCrit);
                }
            }
            else
            {
                CombatEventManager.RaiseDamageTaken(finalDamage);
                UIManager.Instance.damageNumberUI.ShowDamagePopup(finalDamage, transform.position + Vector3.up, isCrit);
            }
        }
        else
        {
            // Debug.Log($"{gameObject.name} damage fully absorbed by shield!");
        }

        if (currentHealth < 0)
            currentHealth = 0;

        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        TryApplyStagger(
            source,
            finalDamage,
            applyStagger,
            staggerDuration,
            causesKnockback,
            knockbackDistance,
            hitInstigator
        );
    }
    
    IEnumerator HitFlash(Enemy enemy)
    {
        if (enemy.enemyRenderer == null) yield break;

        Color originalColor = enemy.rendererColor;
        enemy.enemyRenderer.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        enemy.enemyRenderer.material.color = originalColor;
        // Debug.Log($"{gameObject.name} hit flash ended.");
    }

    public void SetImmune(bool value)
    {
        isInvulnerable = value;
    }

    public void AddShield(float amount)
    {
        if (amount <= 0) return;
        shieldAmount = Mathf.Min(maxShield, shieldAmount + amount);
        // Debug.Log($"{gameObject.name} gained {amount} shield. Current shield: {shieldAmount}");
    }

    public void RemoveShield()
    {
        shieldAmount = 0;
        // Debug.Log($"{gameObject.name} shield removed.");
    }

    public void Heal(float amount)
    {
        if (amount <= 0) return;
        float effectiveAmount = amount * GetCombinedExternalHealingMultiplier();
        if (effectiveAmount <= 0f) return;

        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + effectiveAmount);
        float healedAmount = currentHealth - previousHealth;
        if (healedAmount <= 0f) return;
        
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        if (CompareTag("Player"))
        {
            UIManager.Instance.damageNumberUI.ShowHealPopup(healedAmount, transform.position + Vector3.up);
            CombatEventManager.RaiseHeal(healedAmount);
            if (PlayerActionTracker.Instance != null)
                PlayerActionTracker.Instance.RegisterHeal();
        }
        else
        {
            Enemy enemy = GetComponent<Enemy>();
            if (enemy != null && enemy.healthBar != null)
            {
                UIManager.Instance.damageNumberUI.ShowHealPopup(healedAmount, enemy.healthBar.position);
            }
            else
            {
                UIManager.Instance.damageNumberUI.ShowHealPopup(healedAmount, transform.position + Vector3.up);
            }
        }
        // Debug.Log($"{gameObject.name} healed {amount}. Current health: {currentHealth}");
    }

    public void SetExternalHealingMultiplier(int sourceId, float multiplier)
    {
        if (sourceId == 0) return;
        _externalHealingMultipliers[sourceId] = Mathf.Max(0f, multiplier);
    }

    public void ClearExternalHealingMultiplier(int sourceId)
    {
        if (sourceId == 0) return;
        _externalHealingMultipliers.Remove(sourceId);
    }

    private float GetCombinedExternalHealingMultiplier()
    {
        float combined = 1f;
        foreach (var kv in _externalHealingMultipliers)
        {
            combined *= Mathf.Max(0f, kv.Value);
        }
        return combined;
    }

    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
    }

    private void TryApplyStagger(
        DamageSource source,
        float finalDamage,
        bool applyStagger,
        float staggerDuration,
        bool causesKnockback,
        float knockbackDistance,
        Transform hitInstigator
    )
    {
        if (!enableStaggerOnHit || !applyStagger || finalDamage <= 0f)
            return;

        var staggerable = GetComponent<IStaggerable>();
        if (staggerable == null)
            return;

        float duration = ResolveStaggerDuration(source, staggerDuration);
        if (duration <= 0f)
            return;

        var info = new StaggerInfo(
            duration,
            causesKnockback,
            Mathf.Max(0f, knockbackDistance),
            hitInstigator != null ? hitInstigator.position : Vector3.zero,
            hitInstigator != null
        );
        staggerable.ApplyStagger(info);
        if (GetComponent<Player>() != null)
        {
            if(UnityEngine.Random.value < hitVoiceLineChance && hitVoiceLines != null && hitVoiceLines.Length > 0)
            {
                AudioClip clip = hitVoiceLines[UnityEngine.Random.Range(0, hitVoiceLines.Length)];
                AudioManager.Instance?.PlayVoiceLine(clip);
            }
        }
    }

    private float ResolveStaggerDuration(DamageSource source, float overrideDuration)
    {
        if (overrideDuration >= 0f)
            return overrideDuration;

        switch (source)
        {
            case DamageSource.Melee:
                return defaultMeleeStaggerDuration;
            case DamageSource.Ranged:
                return defaultRangedStaggerDuration;
            case DamageSource.Skill:
                return defaultSkillStaggerDuration;
            default:
                return defaultMeleeStaggerDuration;
        }
    }

    public void SetMaxHealth(float newMaxHealth, bool keepCurrentRatio = true, bool fillOnIncrease = true)
    {
        newMaxHealth = Mathf.Max(1f, newMaxHealth);
        float prevMax = maxHealth;
        float prevCurrent = currentHealth;

        maxHealth = newMaxHealth;
        maxShield = newMaxHealth;

        if (keepCurrentRatio && prevMax > 0f)
        {
            float ratio = prevCurrent / prevMax;
            currentHealth = Mathf.Clamp(ratio * maxHealth, 0f, maxHealth);
        }
        else
        {
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }

        if (!keepCurrentRatio && fillOnIncrease && newMaxHealth > prevMax)
        {
            currentHealth = maxHealth;
        }

        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // public void Heal(float amount)
    // {
    //     currentHealth += amount;
    //     if (currentHealth > maxHealth)
    //         currentHealth = maxHealth;
    //     if (GetComponent<PlayerStats>() != null)
    //     {
    //         onHealthChanged?.Invoke(currentHealth, maxHealth);
    //     }
    // }

    void Die()
    {
        onDeath?.Invoke();
        if (CompareTag("Player"))
        {
            GameManager.Instance.OnPlayerDeath();
            // Debug.Log("Player Died!");
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }
}
