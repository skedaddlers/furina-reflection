using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "WaterAspirations", menuName = "Furina/Skills/Water Aspirations")]
public class WaterAspirations : SkillBase
{
    // upgraded ver give def when shield is broken before duration ends until the duration ends
    public float defBonus = 15f;

    private GameObject activeCaster;
    private GameObject currentEffectInstance;
    private PlayerStats activePlayerStats;
    private EnemyStats activeEnemyStats;
    private float currentShieldAmount;
    private float elapsed;
    private bool isActive = false;
    private bool defBonusApplied = false;

    private void OnEnable()
    {
        // Reset state when ScriptableObject is loaded
        isActive = false;
        activeCaster = null;
        activePlayerStats = null;
        currentShieldAmount = 0f;
        elapsed = 0f;
    }

    public override void OnSkillActivate(GameObject caster)
    {
        base.OnSkillActivate(caster);

        if (isActive)
        {
            Debug.LogWarning($"{skillName}: Already active, refreshing shield!");
            currentShieldAmount = shieldAmount;
            return;
        }

        activeCaster = caster;
        activePlayerStats = caster.GetComponent<PlayerStats>();
        EnemyStats activeEnemyStats = caster.GetComponent<EnemyStats>();

        Health targetHealth = activePlayerStats != null ? activePlayerStats.health : caster.GetComponent<Health>();

        if (targetHealth == null)
        {
            Debug.LogWarning($"{skillName}: Missing PlayerStats or Health component!");
            return;
        }

        Debug.Log($"{skillName} activated by {caster.name}");

        // Play cast sound
        if (castSound != null)
        {
            AudioManager.Instance.PlayVoiceLine(castSound);
        }

        // Spawn effect prefab
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, caster.transform.position, Quaternion.identity, caster.transform);
            currentEffectInstance = effect;
            Destroy(effect, duration);
        }

        if (ongoingSound != null)
        {
            AudioManager.Instance.PlaySFXNoOverlap(ongoingSound, randomizePitch: false, duration: duration);
        }

        // Apply shield
        currentShieldAmount = shieldAmount;

        targetHealth.AddShield(currentShieldAmount);

        isActive = true;

        Debug.Log($"{skillName}: Shield applied! Amount: {currentShieldAmount}");

        // Start shield duration coroutine
        MonoBehaviour casterMono = caster.GetComponent<MonoBehaviour>();
        if (casterMono != null)
        {
            casterMono.StartCoroutine(ShieldDuration(caster));
        }
    }

    private IEnumerator ShieldDuration(GameObject caster)
    {
        elapsed = 0f;

        var health = activePlayerStats != null ? activePlayerStats.health : caster.GetComponent<Health>();

        while (elapsed < duration && isActive)
        {
            if (health != null)
            {
                bool shieldBroken = health.shieldAmount <= 0;

                if (shieldBroken)
                {
                    if (isUpgraded && !defBonusApplied)
                    {
                        ApplyDefBonus();

                        if (currentEffectInstance != null)
                        {
                            Destroy(currentEffectInstance);
                            currentEffectInstance = null;
                        }
                    }
                    else if (!isUpgraded)
                    {
                        Debug.Log($"{skillName}: Shield broke early, ending skill.");
                        break;
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        OnSkillEnd(caster);
    }

    private void ApplyDefBonus()
    {
        if (activePlayerStats != null)
        {
            activePlayerStats.baseDefense += defBonus;
            defBonusApplied = true;
            Debug.Log($"{skillName}: Shield broke early! DEF bonus applied: +{defBonus}");
        }
    }


    public override void OnSkillEnd(GameObject caster)
    {
        if (!isActive) return;

        base.OnSkillEnd(caster);
        
        if (defBonusApplied && activePlayerStats != null)
        {
            activePlayerStats.baseDefense -= defBonus;
            defBonusApplied = false;
            Debug.Log($"{skillName}: DEF bonus removed.");
        }

        isActive = false;
        activeCaster = null;
        if (currentEffectInstance != null)
        {
            Destroy(currentEffectInstance);
            currentEffectInstance = null;
        }

        if (activePlayerStats != null && activePlayerStats.health != null)
        {
            activePlayerStats.health.RemoveShield();
        }
        else
        {
            var h = caster.GetComponent<Health>();
            if (h != null) h.RemoveShield();
        }
        activePlayerStats = null;
        currentShieldAmount = 0f;
        Debug.Log($"{skillName} ended");
    }

    public override bool CanUseSkill(GameObject caster)
    {
        PlayerStats playerStats = caster.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            return playerStats.CurrentMana >= manaCost;
        }
        // Enemies or non-player casters: ignore mana gating
        return true;
    }
}
