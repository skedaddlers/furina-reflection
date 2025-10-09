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

        if (distance > attackRange && distance < detectionRange)
        {
            // Jalan ke player
            agent.isStopped = false;
            agent.SetDestination(player.position);
            animator.SetFloat("WalkSpeed", 1f);
        }
        else if (distance >= detectionRange)
        {
            // Idle
            agent.isStopped = true;
            animator.SetFloat("WalkSpeed", 0f);
        }
        else
        {
            // Stop & attack
            agent.isStopped = true;
            animator.SetFloat("WalkSpeed", 0f);

            if (Time.time - lastAttackTime >= attackCooldown)
            {

                animator.SetTrigger("Attack");
                lastAttackTime = Time.time;
            }
        }
    }

    // Panggil dari animation event di animasi Attack
    public void DealDamage()
    {
        if (Vector3.Distance(player.position, transform.position) <= attackRange + 0.5f)
        {
            player.GetComponent<Health>()?.TakeDamage(damage);
        }
    }
}
