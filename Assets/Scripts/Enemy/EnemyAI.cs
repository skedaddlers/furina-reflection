using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    public bool isRanged = false;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    public int damage = 10;
    public float detectionRange = 10f;
    public Projectile projectilePrefab;

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

    public virtual void RangedAttack()
    {
        LookAtPlayer();
        if (Vector3.Distance(player.position, transform.position) <= attackRange + 0.5f)
        {
            if (projectilePrefab != null)
            {
                Vector3 dir = (player.position - transform.position).normalized;
                GameObject go = Instantiate(projectilePrefab.gameObject, transform.position + dir * 1f + Vector3.up * 1.5f, Quaternion.LookRotation(dir));
                var proj = go.GetComponent<Projectile>();
                if (proj == null) proj = go.AddComponent<Projectile>();

                proj.Init(dir, this.transform);
            }
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
            if(isRanged)
            {
                animator.SetTrigger("RangedAttack");
            }
            else
            {
                animator.SetTrigger("Attack");
            }
            lastAttackTime = Time.time;
        }
    }

    public virtual void LookAtPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // keep only horizontal rotation
        if (direction != Vector3.zero)
        {
            StartCoroutine(RotateTowards(direction));
        }
    }

    private IEnumerator RotateTowards(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.5f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            yield return null;
        }
        transform.rotation = targetRotation;
    }

    public virtual void SpecialAttack()
    {
        // to be overridden by subclasses
    }

    public virtual bool CanPerformSpecialAttack()
    {
        // Default: common enemy nggak punya special
        return false;
    }
}
