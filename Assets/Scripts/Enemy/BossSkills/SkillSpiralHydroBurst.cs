using UnityEngine;
using System.Collections;
using UnityEngine.AI;

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
        Quaternion baseRotation = GetBaseFacingRotation();
        float spinAngle = 0f;
        float elapsed = 0f;
        float interval = Mathf.Max(0.01f, fireInterval);
        float spinDegreesPerSecond = angleStepPerWindow / interval;
        NavMeshAgent agent = boss.GetComponent<NavMeshAgent>();
        bool restoreAgentRotation = false;
        bool updateRotationBackup = false;
        float angularSpeedBackup = 0f;

        if (agent != null)
        {
            restoreAgentRotation = true;
            updateRotationBackup = agent.updateRotation;
            angularSpeedBackup = agent.angularSpeed;
            agent.updateRotation = false;
            agent.angularSpeed = 0f;
        }

        boss.transform.rotation = baseRotation;

        if (!string.IsNullOrEmpty(spinBool))
            boss.Animator.SetBool(spinBool, true);

        try
        {
            while (elapsed < spinDuration)
            {
                FireWindow(baseRotation, spinAngle);

                float wait = Mathf.Min(interval, spinDuration - elapsed);
                if (wait > 0f)
                {
                    yield return RotateBossForDuration(baseRotation, spinAngle, wait, spinDegreesPerSecond);
                    spinAngle += spinDegreesPerSecond * wait;
                }

                elapsed += wait;
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(spinBool))
                boss.Animator.SetBool(spinBool, false);

            if (restoreAgentRotation && agent != null)
            {
                agent.updateRotation = updateRotationBackup;
                agent.angularSpeed = angularSpeedBackup;
            }
        }
    }

    private IEnumerator RotateBossForDuration(Quaternion baseRotation, float startSpinAngle, float duration, float degreesPerSecond)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = Mathf.Min(Time.deltaTime, duration - elapsed);
            elapsed += dt;
            float currentSpinAngle = startSpinAngle + (degreesPerSecond * elapsed);
            boss.transform.rotation = baseRotation * Quaternion.Euler(0f, currentSpinAngle, 0f);
            yield return null;
        }

        boss.transform.rotation = baseRotation * Quaternion.Euler(0f, startSpinAngle + (degreesPerSecond * duration), 0f);
    }

    private Quaternion GetBaseFacingRotation()
    {
        if (boss.TargetPlayer == null)
            return boss.transform.rotation;

        Vector3 toPlayer = boss.TargetPlayer.position - boss.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude <= 0.0001f)
            return boss.transform.rotation;

        return Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
    }

    private void FireWindow(Quaternion baseRotation, float spinAngle)
    {
        PlayCastSound();
        int count = Mathf.Max(1, projectilesPerWindow);
        float startAngle = -spreadAngle * 0.5f;
        float step = count > 1 ? spreadAngle / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float localSpread = startAngle + (step * i);
            float worldAngle = startAngleOffset + spinAngle + localSpread;
            Quaternion shotRotation = baseRotation * Quaternion.Euler(0f, worldAngle, 0f);
            Vector3 dir = shotRotation * Vector3.forward;
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
