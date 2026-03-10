using UnityEngine;
using System.Collections;
using UnityEngine.AI;

// special attack: gains a shield
public class EliteAbyssMage : EnemyAI
{
    public float specialAttackCooldown = 5f;
    private float lastSpecialAttackTime;

    [Header("Special Attack Settings")]
    public ParticleSystem shieldEffect;
    public float shieldDuration = 3.0f;
    public float shieldAmount = 50f;
    private bool isShieldActive = false;

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

        DoSpecialAttack();
    }

    private void DoSpecialAttack()
    {
        isPerformingSpecialAttack = true;
        lastSpecialAttackTime = Time.time;

        // Trigger anim khusus kalau ada
        if (animator != null)
            animator.SetTrigger("SpecialAttack");
    }

    // shield method through animation event
    public void GainShield()
    {
        if (shieldEffect != null && !isShieldActive)
        {
            ParticleSystem effect = Instantiate(shieldEffect, transform.position, Quaternion.identity);
            effect.transform.parent = this.transform;
            Destroy(effect.gameObject, shieldDuration + 0.5f);
            effect.Play();
            isShieldActive = true;
            StartCoroutine(ShieldDuration());
            GetComponent<Health>()?.AddShield(shieldAmount);
        }
        isPerformingSpecialAttack = false;
    }

    private IEnumerator ShieldDuration()
    {
        // Tambah shield ke enemy
        // currentHealth += shieldAmount;
        // if (currentHealth > maxHealth)
        //     currentHealth = maxHealth;

        yield return new WaitForSeconds(shieldDuration);
        // Hapus shield 
        // currentHealth -= shieldAmount;
        // if (currentHealth < 0)
        //     currentHealth = 0;
        GetComponent<Health>()?.RemoveShield();
        isShieldActive = false;
    }


    public override bool CanPerformSpecialAttack()
    {
        return !isPerformingSpecialAttack &&
               Time.time - lastSpecialAttackTime >= specialAttackCooldown &&
               player != null;
    }

    protected override void OnStaggerStarted()
    {
        if (isShieldActive)
        {
            GetComponent<Health>()?.RemoveShield();
            isShieldActive = false;
        }
    }
}
