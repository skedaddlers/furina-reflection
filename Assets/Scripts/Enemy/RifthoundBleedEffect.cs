using System.Collections;
using UnityEngine;

public class RifthoundBleedEffect : MonoBehaviour
{
    private GameObject vfxInstance;
    private Coroutine bleedRoutine;
    private Transform bleedSource;
    public int SourceId { get; private set; }

    public void Apply(int sourceId, float damagePerTick, float tickInterval, int ticks, Transform source, GameObject bleedVFXPrefab)
    {
        SourceId = sourceId;
        bleedSource = source;

        if (bleedRoutine != null)
        {
            StopCoroutine(bleedRoutine);
        }

        bleedRoutine = StartCoroutine(BleedRoutine(damagePerTick, tickInterval, ticks, bleedVFXPrefab));
    }

    private IEnumerator BleedRoutine(float damagePerTick, float tickInterval, int ticks, GameObject bleedVFXPrefab)
    {
        Health health = GetComponent<Health>();
        if (health == null || damagePerTick <= 0f || ticks <= 0)
        {
            Destroy(this);
            yield break;
        }

        // Spawn VFX
        if (bleedVFXPrefab != null)
        {
            vfxInstance = Instantiate(bleedVFXPrefab, transform);
            vfxInstance.transform.localPosition = Vector3.zero;
            Destroy(vfxInstance, tickInterval * ticks + 0.5f); // Ensure VFX is cleaned up after effect ends
        }

        float safeTickInterval = Mathf.Max(0.01f, tickInterval);
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(safeTickInterval);

            if (health == null || health.CurrentHealth <= 0f)
                break;

            health.TakeDamage(
                damagePerTick,
                isCrit: false,
                source: DamageSource.Skill,
                applyStagger: false,
                hitInstigator: bleedSource
            );
        }

        bleedRoutine = null;
        Destroy(this);
    }
}
