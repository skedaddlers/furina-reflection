using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 3f;
    public float damage = 5f;
    public LayerMask hitMask; // set agar kena Enemy/Obstacle

    private Vector3 _dir;
    private float _timer;

    public void Init(Vector3 dir, float speed, float lifeTime, float damage)
    {
        _dir = dir.normalized;
        this.speed = speed;
        this.lifeTime = lifeTime;
        this.damage = damage;
        _timer = 0f;
    }

    void Update()
    {
        transform.position += _dir * speed * Time.deltaTime;
        _timer += Time.deltaTime;
        if (_timer >= lifeTime) Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        // Cek layer mask
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0) return;

        // Coba damage target yang punya Health
        var health = other.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        // Hancurkan saat kena apapun yang valid
        Destroy(gameObject);
    }
}
