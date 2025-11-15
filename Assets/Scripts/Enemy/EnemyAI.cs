using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    public int damage = 10;
    public float detectionRange = 10f;

    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private float lastAttackTime;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float distance = Vector3.Distance(player.position, transform.position);
    }

    // Panggil dari animation event di animasi Attack
    public void DealDamage()
    {
        if (Vector3.Distance(player.position, transform.position) <= attackRange + 0.5f)
        {
            player.GetComponent<Health>()?.TakeDamage(damage);
        }
    }

    public bool SeePlayer()
    {
        return Vector3.Distance(player.position, transform.position) <= detectionRange;
    }

    public bool InAttackRange()
    {
        return Vector3.Distance(player.position, transform.position) <= attackRange;
    }

    public void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
        animator.SetFloat("WalkSpeed", 1f);
    }


    public void StopChasing()
    {
        agent.isStopped = true;
        animator.SetFloat("WalkSpeed", 0f);
    }
    
    public void AttackPlayer()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            animator.SetTrigger("Attack");
            lastAttackTime = Time.time;
        }
    }
}
