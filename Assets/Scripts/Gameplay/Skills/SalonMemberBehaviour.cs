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
        StartCoroutine(SalonMemberAI());
    }

    private IEnumerator SalonMemberAI()
    {
        while (true)
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

            yield return new WaitForSeconds(0.1f);
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
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(skillData.enemyTag);

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

        // Calculate damage
        float finalDamage = skillData.damageAmount;
        if (owner != null)
        {
            PlayerStats playerStats = owner.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                finalDamage += playerStats.baseAttack * 0.5f; // 50% of player's attack
                finalDamage = playerStats.RollDamage(finalDamage);
            }
        }

        // Create dummy projectile
        FireDummyProjectile(finalDamage);

        // Play impact sound
        if (skillData.impactSound != null)
        {
            AudioSource.PlayClipAtPoint(skillData.impactSound, transform.position);
        }
    }

    private void FireDummyProjectile(float damage)
    {
        // Create a simple sphere projectile
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "SalonProjectile";
        projectile.transform.position = transform.position;
        projectile.transform.localScale = Vector3.one * 0.3f;

        // Set color
        Renderer renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.blue;
            renderer.material = mat;
        }

        // Add projectile behaviour
        SalonProjectile proj = projectile.AddComponent<SalonProjectile>();
        proj.Initialize(damage, skillData.projectileSpeed, skillData.enemyTag);

        // Set direction towards target
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        proj.SetDirection(direction);
    }
}