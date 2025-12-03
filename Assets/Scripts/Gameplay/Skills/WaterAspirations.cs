using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "WaterAspirations", menuName = "Furina/Skills/Water Aspirations")]
public class WaterAspirations : SkillBase
{
    private GameObject activeCaster;
    private PlayerStats activePlayerStats;
    private float currentShieldAmount;
    private bool isActive = false;

    private void OnEnable()
    {
        // Reset state when ScriptableObject is loaded
        isActive = false;
        activeCaster = null;
        activePlayerStats = null;
        currentShieldAmount = 0f;
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

        if (activePlayerStats == null || activePlayerStats.health == null)
        {
            Debug.LogWarning($"{skillName}: Missing PlayerStats or Health component!");
            return;
        }

        Debug.Log($"{skillName} activated by {caster.name}");

        // Play cast sound
        if (castSound != null)
        {
            AudioSource.PlayClipAtPoint(castSound, caster.transform.position);
        }

        // Spawn effect prefab
        if (effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(effectPrefab, caster.transform.position, Quaternion.identity, caster.transform);
            Object.Destroy(effect, duration);
        }

        // Apply shield
        currentShieldAmount = shieldAmount;

        // Subscribe to damage event to absorb damage
        activePlayerStats.health.onTakeDamage += AbsorbDamage;

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
        float elapsed = 0f;

        while (elapsed < duration && isActive && currentShieldAmount > 0)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        OnSkillEnd(caster);
    }

    private float AbsorbDamage(float incomingDamage)
    {
        if (!isActive || currentShieldAmount <= 0) return incomingDamage;

        float absorbed = Mathf.Min(currentShieldAmount, incomingDamage);
        currentShieldAmount -= absorbed;
        float remainingDamage = incomingDamage - absorbed;

        Debug.Log($"{skillName}: Absorbed {absorbed} damage. Shield remaining: {currentShieldAmount}");

        // Play impact sound when shield absorbs damage
        if (impactSound != null && activeCaster != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, activeCaster.transform.position);
        }

        // Shield broke
        if (currentShieldAmount <= 0)
        {
            Debug.Log($"{skillName}: Shield broke!");
            OnSkillEnd(activeCaster);
        }

        return remainingDamage;
    }

    public override void OnSkillEnd(GameObject caster)
    {
        if (!isActive) return;

        base.OnSkillEnd(caster);

        // Unsubscribe from damage event
        if (activePlayerStats != null && activePlayerStats.health != null)
        {
            activePlayerStats.health.onTakeDamage -= AbsorbDamage;
        }

        isActive = false;
        activeCaster = null;
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
        return base.CanUseSkill(caster);
    }
}