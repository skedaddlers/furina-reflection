using UnityEngine;
using System.Collections;

public class SalonMemberBehaviour : MonoBehaviour
{
    private GameObject owner;
    private SalonSolitaire skillData;
    private float lastAttackTime;
    private Transform currentTarget;

    public void Initialize(GameObject caster, SalonSolitaire skill)
    {
        owner = caster;
        skillData = skill;
        lastAttackTime = -skill.attackInterval; // Allow immediate first attack

        // Start following and attacking
    }

    void Update()
    {
        // Follow owner at a distance
        FollowOwner();
        // Find and attack enemies
        FindTarget();

        if (currentTarget != null && Time.time >= lastAttackTime + skillData.attackInterval)
        {
            AttackTarget();
            lastAttackTime = Time.time;
        }
    }

    private void FollowOwner()
    {
        if (owner == null) return;

        // Calculate offset from owner
        Vector3 directionToOwner = owner.transform.position - transform.position;
        float distanceToOwner = directionToOwner.magnitude;

        // If too far from owner, move closer
        if (distanceToOwner > skillData.spawnRadius * 2f)
        {
            Vector3 targetPosition = owner.transform.position - directionToOwner.normalized * skillData.spawnRadius;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 3f);
        }

        // Float effect (bobbing up and down)
        float bobOffset = Mathf.Sin(Time.time * 2f + transform.GetInstanceID()) * 0.1f;
        transform.position = new Vector3(transform.position.x, owner.transform.position.y + 1f + bobOffset, transform.position.z);
    }

    private void FindTarget()
    {
        currentTarget = null;
        float closestDistance = skillData.attackRange;

        // Find all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(skillData.targetTag);

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentTarget = enemy.transform;
            }
        }
    }

    private void AttackTarget()
    {
        if (currentTarget == null || skillData == null) return;

        // Look at target
        transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));

        transform.Rotate(-90f, 0f, 0f); // Adjust for model orientation if needed
        // Calculate damage
        float baseDamage = skillData.damageAmount;
        if (owner != null)
        {
            PlayerStats playerStats = owner.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                baseDamage += playerStats.baseAttack * 0.5f; // 50% of player's attack
            }
        }

        FireProjectile(baseDamage);

        // Play impact sound
        if (skillData.impactSound != null)
        {
            AudioManager.Instance.PlayClipAtPoint(skillData.impactSound, transform.position);
        }
    }

    private void FireProjectile(float damage)
    {
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        GameObject projectile = Object.Instantiate(skillData.projectilePrefab, transform.position + direction * 0.5f, Quaternion.LookRotation(direction));
        Projectile projComponent = projectile.GetComponent<Projectile>();
        LayerMask targetMask = LayerMask.GetMask(skillData.targetTag);
        if (projComponent != null)
        {
            projComponent.Init(direction, skillData.projectileSpeed, projComponent.lifeTime, damage, owner.transform, targetMask, true);
        }
    }
}
