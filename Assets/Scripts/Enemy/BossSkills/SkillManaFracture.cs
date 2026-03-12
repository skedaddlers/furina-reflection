using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SkillManaFracture : BossSkill
{
    [Header("Bubble Count")]
    [SerializeField] private int minBubbleCount = 3;
    [SerializeField] private int maxBubbleCount = 5;

    [Header("Placement")]
    [SerializeField] private float minSpawnRadius = 1.5f;
    [SerializeField] private float maxSpawnRadius = 5f;
    [SerializeField] private float minBubbleSpacing = 1f;
    [SerializeField] private Vector3 bubbleOffset = new Vector3(0f, 0.05f, 0f);
    [SerializeField] private int placementAttemptsPerBubble = 12;

    [Header("Telegraph & Timing")]
    [SerializeField] private float bubbleRadius = 1.25f;
    [SerializeField] private int bubbleSegments = 24;
    [SerializeField] private float initialTelegraphTime = 0.8f;
    [SerializeField] private float delayBetweenExplosions = 0.3f;

    [Header("Effects")]
    [SerializeField] private GameObject bubbleEffectPrefab;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private float bubbleEffectLifetime = 3f;
    [SerializeField] private float explosionEffectLifetime = 2f;

    [Header("On Hit")]
    [SerializeField] private bool dealDamageOnHit = true;
    [SerializeField] private int manaDrainOnHit = 2;

    public override IEnumerator ExecuteRoutine()
    {
        if (boss == null || boss.TargetPlayer == null) yield break;

        Vector3 center = boss.TargetPlayer.position;
        center.y = boss.transform.position.y;

        int count = Random.Range(Mathf.Max(1, minBubbleCount), Mathf.Max(minBubbleCount, maxBubbleCount) + 1);
        List<Vector3> explosionPositions = GenerateBubblePositions(center, count);
        float startupDelay = Mathf.Max(0f, initialTelegraphTime);
        float sequentialDelay = Mathf.Max(0f, delayBetweenExplosions);

        List<Telegraph> telegraphs = new List<Telegraph>(explosionPositions.Count);
        List<GameObject> bubbleEffects = new List<GameObject>(explosionPositions.Count);

        for (int i = 0; i < explosionPositions.Count; i++)
        {
            Vector3 position = explosionPositions[i] + bubbleOffset;

            if (boss.telegraphPrefab != null)
            {
                Telegraph t = Instantiate(boss.telegraphPrefab, position, Quaternion.identity);
                float totalTelegraphTime = startupDelay + sequentialDelay * Mathf.Max(0, explosionPositions.Count - i);
                t.ConfigureCircle(bubbleRadius, bubbleSegments, totalTelegraphTime);
                Destroy(t.gameObject, totalTelegraphTime + 0.1f);
                telegraphs.Add(t);
            }
            else
            {
                telegraphs.Add(null);
            }

            if (bubbleEffectPrefab != null)
            {
                GameObject fx = Instantiate(bubbleEffectPrefab, position, Quaternion.identity);
                Destroy(fx, bubbleEffectLifetime);
                bubbleEffects.Add(fx);
            }
            else
            {
                bubbleEffects.Add(null);
            }
        }

        boss.Animator.SetTrigger(animationTrigger);
        if (startupDelay > 0f)
            yield return new WaitForSeconds(startupDelay);

        for (int i = 0; i < explosionPositions.Count; i++)
        {
            Vector3 explosionPos = explosionPositions[i] + bubbleOffset;

            if (telegraphs[i] != null)
                Destroy(telegraphs[i].gameObject);

            if (bubbleEffects[i] != null)
                Destroy(bubbleEffects[i]);

            if (explosionEffectPrefab != null)
            {
                GameObject explosionFx = Instantiate(explosionEffectPrefab, explosionPos, Quaternion.identity);
                Destroy(explosionFx, explosionEffectLifetime);
            }

            if (IsTargetInsideExplosion(explosionPos))
            {
                if (dealDamageOnHit)
                    boss.DealSpecialDamage();

                DrainTargetMana();
            }

            if (i < explosionPositions.Count - 1 && sequentialDelay > 0f)
                yield return new WaitForSeconds(sequentialDelay);
        }
    }

    private List<Vector3> GenerateBubblePositions(Vector3 center, int count)
    {
        List<Vector3> positions = new List<Vector3>(count);
        float minSpacingSqr = minBubbleSpacing * minBubbleSpacing;
        float innerRadius = Mathf.Max(0f, minSpawnRadius);
        float outerRadius = Mathf.Max(innerRadius, maxSpawnRadius);

        for (int i = 0; i < count; i++)
        {
            bool found = false;
            int attempts = Mathf.Max(1, placementAttemptsPerBubble);

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                Vector2 randomDirection = Random.insideUnitCircle;
                if (randomDirection.sqrMagnitude <= 0.0001f)
                    randomDirection = Vector2.right;
                randomDirection.Normalize();

                Vector2 randomCircle = randomDirection * Random.Range(innerRadius, outerRadius);
                Vector3 candidate = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

                bool overlaps = false;
                for (int p = 0; p < positions.Count; p++)
                {
                    Vector3 delta = candidate - positions[p];
                    delta.y = 0f;
                    if (delta.sqrMagnitude < minSpacingSqr)
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    positions.Add(candidate);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                float angle = i / Mathf.Max(1f, (float)count) * Mathf.PI * 2f;
                Vector3 fallback = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Mathf.Max(minSpawnRadius, 0.2f);
                positions.Add(fallback);
            }
        }

        return positions;
    }

    private bool IsTargetInsideExplosion(Vector3 center)
    {
        if (boss.TargetPlayer == null) return false;

        Vector3 delta = boss.TargetPlayer.position - center;
        delta.y = 0f;
        return delta.sqrMagnitude <= bubbleRadius * bubbleRadius;
    }

    private void DrainTargetMana()
    {
        if (manaDrainOnHit <= 0 || boss.TargetPlayer == null) return;

        PlayerStats stats = boss.TargetPlayer.GetComponent<PlayerStats>();
        if (stats == null) return;

        stats.UseMana(manaDrainOnHit);
    }
}
