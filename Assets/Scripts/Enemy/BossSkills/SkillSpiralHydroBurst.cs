using UnityEngine;
using System.Collections;

public class SkillSpiralHydroBurst : BossSkill
{
    [Header("Spin")]
    [SerializeField] private float spinDuration = 2.5f;
    [SerializeField] private float fireInterval = 0.18f;
    [SerializeField] private float startAngleOffset = 0f;
    [SerializeField] private float angleStepPerWindow = 28f;
    [SerializeField] private string spinBool = "Spin";

    [Header("Burst Pattern")]
    [SerializeField] private int projectilesPerWindow = 1;
    [SerializeField] private float spreadAngle = 20f;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.5f, 0.5f);

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 14f;
    [SerializeField] private float projectileLifetime = 4f;
    [SerializeField] private float projectileDamage = 10f;

    public override IEnumerator ExecuteRoutine()
    {
        if (boss == null || projectilePrefab == null) yield break;
        FaceTargetInstantly();

        if (!string.IsNullOrEmpty(spinBool))
            boss.Animator.SetBool(spinBool, true);

        float elapsed = 0f;
        float interval = Mathf.Max(0.01f, fireInterval);
        float spinDegreesPerSecond = angleStepPerWindow / interval;

        while (elapsed < spinDuration)
        {
            FireWindow(startAngleOffset);

            float wait = Mathf.Min(interval, spinDuration - elapsed);
            if (wait > 0f)
                yield return RotateBossForDuration(wait, spinDegreesPerSecond);
            elapsed += wait;
        }

        if (!string.IsNullOrEmpty(spinBool))
            boss.Animator.SetBool(spinBool, false);
    }

    private IEnumerator RotateBossForDuration(float duration, float degreesPerSecond)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = Mathf.Min(Time.deltaTime, duration - elapsed);
            elapsed += dt;
            boss.transform.Rotate(0f, degreesPerSecond * dt, 0f, Space.World);
            yield return null;
        }
    }

    private void FaceTargetInstantly()
    {
        if (boss.TargetPlayer == null) return;

        Vector3 toPlayer = boss.TargetPlayer.position - boss.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude <= 0.0001f) return;

        boss.transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
    }

    private void FireWindow(float centerAngleOffset)
    {
        int count = Mathf.Max(1, projectilesPerWindow);
        float startAngle = -spreadAngle * 0.5f;
        float step = count > 1 ? spreadAngle / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float localSpread = startAngle + (step * i);
            float worldAngle = centerAngleOffset + localSpread;
            Vector3 dir = Quaternion.Euler(0f, worldAngle, 0f) * boss.transform.forward;

            Transform spawn = projectileSpawnPoint != null ? projectileSpawnPoint : boss.transform;
            GameObject go = Instantiate(projectilePrefab, spawn.position + spawnOffset, Quaternion.LookRotation(dir));
            Projectile p = go.GetComponent<Projectile>();
            if (p == null) p = go.AddComponent<Projectile>();

            p.Init(
                dir,
                projectileSpeed,
                projectileLifetime,
                projectileDamage,
                boss.transform,
                null,
                true
            );
        }
    }
}
