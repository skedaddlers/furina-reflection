using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "SingerOfManyWaters", menuName = "Furina/Skills/Singer Of Many Waters")]
public class SingerOfManyWaters : SkillBase
{
    [Header("Singer Settings")]
    public GameObject singerPrefab;
    public float spawnDistance = 2f;
    public float healInterval = 1f;
    public float floatHeight = 1.5f;
    public float staminaConsumptionReductionPercentBuff = 0.35f;

    private PlayerStats activePlayerStats;
    private GameObject activeSinger;
    private bool isActive = false;

    private void OnEnable()
    {
        isActive = false;
        activeSinger = null;
        activePlayerStats = null;
    }

    public override void OnSkillActivate(GameObject caster)
    {
        base.OnSkillActivate(caster);

        if (isActive) return;

        isActive = true;

        Debug.Log($"{skillName} activated by {caster.name}");

        // Play cast sound
        if (castSound != null)
        {
            AudioManager.Instance.PlayVoiceLine(castSound);
        }
        activePlayerStats = caster.GetComponent<PlayerStats>();
        if(activePlayerStats != null && isUpgraded)
        {
            activePlayerStats.staminaConsumptionReductionPercent += staminaConsumptionReductionPercentBuff;
        }

        // Spawn singer
        SpawnSinger(caster);

        // Start heal coroutine
        MonoBehaviour casterMono = caster.GetComponent<MonoBehaviour>();
        if (casterMono != null)
        {
            casterMono.StartCoroutine(SingerHealEffect(caster));
        }
    }

    private void SpawnSinger(GameObject caster)
    {
        // Calculate spawn position behind the player
        Vector3 spawnPosition = caster.transform.position - caster.transform.forward * spawnDistance;
        spawnPosition.y += floatHeight;

        if (singerPrefab != null)
        {
            activeSinger = Object.Instantiate(singerPrefab, spawnPosition, Quaternion.identity);
            activeSinger.transform.localRotation = singerPrefab.transform.localRotation;
        }
        else
        {
            // Create dummy singer
            activeSinger = CreateDummySinger(spawnPosition);
        }

        // Add singer behaviour
        SingerBehaviour behaviour = activeSinger.GetComponent<SingerBehaviour>();
        if (behaviour == null)
        {
            behaviour = activeSinger.AddComponent<SingerBehaviour>();
        }
        behaviour.Initialize(caster, spawnDistance, floatHeight);

        // Spawn effect prefab on singer
        if (effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(effectPrefab, activeSinger.transform.position, Quaternion.identity, activeSinger.transform);
            Object.Destroy(effect, duration);
        }

        Debug.Log($"Singer spawned at {spawnPosition}");
    }

    private GameObject CreateDummySinger(Vector3 position)
    {
        // Create a dummy singer (sphere with trail)
        GameObject singer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        singer.name = "SingerOfManyWaters";
        singer.transform.position = position;
        singer.transform.localScale = Vector3.one * 0.6f;

        // Set color to blue/aqua
        Renderer renderer = singer.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.2f, 0.8f, 1f); // Aqua color
            renderer.material = mat;
        }

        // Remove collider
        Collider col = singer.GetComponent<Collider>();
        if (col != null)
        {
            Object.Destroy(col);
        }

        return singer;
    }

    private IEnumerator SingerHealEffect(GameObject caster)
    {
        float elapsed = 0f;
        PlayerStats playerStats = caster.GetComponent<PlayerStats>();
        Health targetHealth = playerStats != null ? playerStats.health : caster.GetComponent<Health>();

        while (elapsed < duration && isActive)
        {
            // Heal player
            if (targetHealth != null)
            {
                targetHealth.Heal(healAmount);
                Debug.Log($"{skillName}: Healed {healAmount} HP. Elapsed: {elapsed}/{duration}");
            }
            else
            {
                Debug.LogWarning($"{skillName}: No Health found on caster, stopping.");
                break;
            }

            // Play impact sound for heal feedback
            if (impactSound != null && activeSinger != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, activeSinger.transform.position);
            }

            yield return new WaitForSeconds(healInterval);
            elapsed += healInterval;
        }

        OnSkillEnd(caster);
    }

    public override void OnSkillEnd(GameObject caster)
    {
        if (!isActive) return;

        base.OnSkillEnd(caster);

        // Destroy singer
        if (activeSinger != null)
        {
            Object.Destroy(activeSinger);
            activeSinger = null;
        }

        if(activePlayerStats != null && isUpgraded)
        {
            activePlayerStats.staminaConsumptionReductionPercent -= staminaConsumptionReductionPercentBuff;
        }

        isActive = false;

        Debug.Log($"{skillName} ended - Singer dismissed");
    }

    public override bool CanUseSkill(GameObject caster)
    {
        if (isActive) return false;

        PlayerStats playerStats = caster.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            return playerStats.CurrentMana >= manaCost;
        }
        // Enemies or non-player casters: allow
        return true;
    }
}
