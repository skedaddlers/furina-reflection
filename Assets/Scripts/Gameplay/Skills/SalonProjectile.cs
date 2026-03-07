using UnityEngine;

public class SalonProjectile : MonoBehaviour
{
    private float damage;
    private float speed;
    private string enemyTag;
    private Vector3 direction;
    private float lifetime = 5f;
    private float spawnTime;

    public void Initialize(float dmg, float spd, string tag)
    {
        damage = dmg;
        speed = spd;
        enemyTag = tag;
        spawnTime = Time.time;

        // Set collider as trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Add rigidbody for trigger detection
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;
    }

    void Update()
    {
        // Move projectile
        transform.position += direction * speed * Time.deltaTime;

        // Destroy after lifetime
        if (Time.time > spawnTime + lifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if hit enemy
        if (other.CompareTag(enemyTag))
        {
            Health health = other.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage, false, DamageSource.Skill);
            }

            // Destroy projectile on hit
            Destroy(gameObject);
        }
    }
}