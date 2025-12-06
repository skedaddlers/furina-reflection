using UnityEngine;
using System.Collections;
using UnityEngine.AI;

// special attack: throws a rock that deals aoe damage
public class EliteRuinGuard : EnemyAI
{
    public float specialAttackCooldown = 5f;
    private float lastSpecialAttackTime;

    private Vector3 locked_dir;

    protected override void Awake()
    {
        base.Awake();
        // Additional initialization for Elite Lawachurl if needed
    }
    public override void SpecialAttack()
    {
        if (!CanPerformSpecialAttack())
            return;
        StopChasing();
        LookAtPlayer(); 

        DoThrowRockAttack();
    }

    private void DoThrowRockAttack()
    {
        isPerformingSpecialAttack = true;
        lastSpecialAttackTime = Time.time;

        // Trigger anim khusus kalau ada
        if (animator != null)
            animator.SetTrigger("SpecialAttack");
        locked_dir = (player.position - transform.position).normalized;
    }

    // throw method through animation event
    public void ThrowRock()
    {
        if (player != null)
        {
            StartCoroutine(DoThrowRockEffect());
        }
    }
    private IEnumerator DoThrowRockEffect()
    {
        // Buat projectile rock
        if (projectilePrefab != null)
        {
            Vector3 dir = locked_dir;
            GameObject go = Instantiate(projectilePrefab.gameObject, transform.position + dir * 1f + Vector3.up * 1.5f, Quaternion.LookRotation(dir));
            var proj = go.GetComponent<Projectile>();
            if (proj == null) proj = go.AddComponent<Projectile>();

            proj.Init(dir, this.transform);
        }

        yield return new WaitForSeconds(0.5f); // tunggu anim selesai

        isPerformingSpecialAttack = false;
    }

    public override bool CanPerformSpecialAttack()
    {
        return !isPerformingSpecialAttack &&
               Time.time - lastSpecialAttackTime >= specialAttackCooldown &&
               player != null;
    }
}