using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    public int damage = 10;
    public float detectionRange = 10f;

    public bool isPerformingSpecialAttack = false;
    protected Transform player;
    protected NavMeshAgent agent;
    protected Animator animator;
    protected float lastAttackTime;

    protected virtual void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    protected virtual void Update()
    {
        
    }

    // Panggil dari animation event di animasi Attack
    public virtual void DealDamage()
    {
        if (Vector3.Distance(player.position, transform.position) <= attackRange + 0.5f)
        {
            // further implementation include defense, resistances, etc.
            // but for now just directly reduce health
            player.GetComponent<Health>()?.TakeDamage(damage);
        }
    }

    public virtual bool SeePlayer()
    {
        return Vector3.Distance(player.position, transform.position) <= detectionRange;
    }

    public virtual bool InAttackRange()
    {
        return Vector3.Distance(player.position, transform.position) <= attackRange;
    }

    public virtual void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
        animator.SetFloat("WalkSpeed", 1f);
    }


    public virtual void StopChasing()
    {
        agent.isStopped = true;
        animator.SetFloat("WalkSpeed", 0f);
    }
    
    public virtual void AttackPlayer()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            animator.SetTrigger("Attack");
            lastAttackTime = Time.time;
        }
    }

    public virtual void SpecialAttack()
    {
        // to be overridden by subclasses
    }
}
