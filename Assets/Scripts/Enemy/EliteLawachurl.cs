using UnityEngine;
using System.Collections;

public class EliteLawachurl : EnemyAI
{
    public float specialAttackCooldown = 5f;
    private float lastSpecialAttackTime;

    protected override void Awake()
    {
        base.Awake();
        // Additional initialization for Elite Lawachurl if needed
    }
    public override void SpecialAttack()
    {
        StopChasing();
        // Implementation of elite lawachurl's special attack
        Debug.Log("Lawachurl performs a powerful special attack!");
        lastSpecialAttackTime = Time.time;
        isPerformingSpecialAttack = true;
        StartCoroutine(EndSpecialAttackAfterDelay(2f)); // Assume special attack lasts 2 seconds
        // Add special attack logic here (e.g., area damage, stun, etc.)
    }

    private IEnumerator EndSpecialAttackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isPerformingSpecialAttack = false;
    }

    public bool CanPerformSpecialAttack()
    {
        return Time.time - lastSpecialAttackTime >= specialAttackCooldown;
    }
}