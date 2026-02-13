using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    public enum ProjectileMode
    {
        Straight,       // peluru / arrow biasa
        HitScan,
        Homing,          // mengikuti target (belum diimplementasi)
        Trajectory      // melengkung (belum diimplementasi)
    }

    [Header("Projectile Settings")]
    public ProjectileMode mode = ProjectileMode.Straight;
    public bool isAOE = false;
    public float speed = 20f;
    public float lifeTime = 3f;
    public float damage = 5f;
    public LayerMask hitMask; // set agar kena Enemy/Obstacle

    [Header("Ownership")]
    public Transform owner;        // siapa yang nembak
    public bool ignoreOwner = true; // biar nggak nabrak diri sendiri

    [Header("Visuals")]
    public ParticleSystem hitEffect;
    public LineRenderer laserRenderer;

    [Header("Laser Settings")]
    public float maxDistance = 100f;
    public float laserDuration = 0.1f;

    [Header("Homing Settings")]
    public Transform target; // target homing

    [Header("Trajectory Settings")]
    public float gravityMultiplier = 1f;
    
    [Header("AOE Settings")]
    public float aoeRadius = 3f;

    private Vector3 _dir;
    private Vector3 _velocity;
    private float _timer;
    private bool _laserFired = false;
    private bool _exploded = false; 

    public void Init(
        Vector3 dir, 
        Transform owner = null, 
        LayerMask? customHitMask = null
    )
    {
        _dir = dir.normalized;
        this.owner = owner;
        _timer = 0f;

        if (customHitMask.HasValue)
        {
            hitMask = customHitMask.Value;
        }
        if(mode == ProjectileMode.HitScan)
        {
            FireHitScan();
        }
        if(mode == ProjectileMode.Trajectory)
        {
            _velocity = _dir * speed;
        }

    }
    public void Init(Vector3 dir, float speed, float lifeTime, float damage, Transform owner = null, LayerMask? customHitMask = null)
    {
        _dir = dir.normalized;
        this.speed = speed;
        this.lifeTime = lifeTime;
        this.damage = damage;
        this.owner = owner;
        _timer = 0f;

        if(mode == ProjectileMode.Trajectory)
        {
            _velocity = _dir * speed;
        }

        if(mode == ProjectileMode.HitScan)
        {
            FireHitScan();
        }

        if (customHitMask.HasValue)
        {
            hitMask = customHitMask.Value;
        }
    }

    void Update()
    {
        _timer += Time.deltaTime;

        switch(mode)
        {
            case ProjectileMode.Straight:
                transform.position += _dir * speed * Time.deltaTime;
                break;

            case ProjectileMode.Trajectory:
                _velocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
                transform.position += _velocity * Time.deltaTime;

                if(_velocity.sqrMagnitude > 0.1f)
                {
                    transform.rotation = Quaternion.LookRotation(_velocity.normalized);
                }
                break;

            case ProjectileMode.Homing:
                if(target != null)
                {
                    Vector3 toTarget = (target.position - transform.position).normalized;
                    transform.position += toTarget * speed * Time.deltaTime;
                    transform.rotation = Quaternion.LookRotation(toTarget);
                }
                else
                {
                    transform.position += _dir * speed * Time.deltaTime;
                }
                break;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (ignoreOwner && owner != null)
        {
            if (other.transform == owner || other.transform.IsChildOf(owner))
                return;
        }
        // Cek layer mask
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0) return;

        if(isAOE && !_exploded)
        {
            ExplodeAOE();
        }

        if(mode == ProjectileMode.HitScan)
        {
            // HitScan sudah menembak di Init
            return;
        }

        // Coba damage target yang punya Health
        DealDamage(other);

        // Play hit effect
        if (hitEffect != null)
        {
            ParticleSystem ps = Instantiate(hitEffect, transform.position, Quaternion.identity);
            if (ps != null)
            {
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(ps.gameObject, 2f);
            }
            
        }

        // Hancurkan saat kena apapun yang valid
        Destroy(gameObject);
    }

    void DealDamage(Collider other)
    {
        var health = other.GetComponent<Health>();
        float finalDamage = damage;
        bool didCrit = false;
        if(owner != null)
        {
            var ownerStats = owner.GetComponent<PlayerStats>();
            if(ownerStats != null)
            {
                finalDamage = Helpers.CalculateFinalDamage(
                    damage,
                    other.GetComponent<EnemyStats>()?.defense ?? 0f,
                    ownerStats.critRate,
                    ownerStats.critMultiplier,
                    ownerStats.level - (other.GetComponent<EnemyStats>()?.level ?? 0),
                    ownerStats.GetCurrentDamageBuffMultiplier(),
                    out didCrit
                );
            }
            else if(owner.GetComponent<EnemyStats>() != null)
            {
                var enemyStats = owner.GetComponent<EnemyStats>();
                finalDamage = Helpers.CalculateFinalDamage(
                    damage,
                    other.GetComponent<PlayerStats>()?.baseDefense ?? 0f,
                    enemyStats.critRate,
                    enemyStats.critMultiplier,
                    enemyStats.level - (other.GetComponent<PlayerStats>()?.level ?? 0),
                    1f,
                    out didCrit
                );
            }
        }
        if (health != null)
        {
            health.TakeDamage(finalDamage, didCrit);
        }
    }

    void ExplodeAOE()
    {
        _exploded = true;
        Collider[] hits = Physics.OverlapSphere(
            transform.position, 
            aoeRadius, 
            hitMask
        );
        // visualize
        Debug.DrawLine(transform.position, transform.position + Vector3.up * aoeRadius, Color.red, 1f);
        Debug.DrawLine(transform.position, transform.position + Vector3.down * aoeRadius, Color.red, 1f);
        Debug.DrawLine(transform.position, transform.position + Vector3.left * aoeRadius, Color.red, 1f);
        Debug.DrawLine(transform.position, transform.position + Vector3.right * aoeRadius, Color.red, 1f);
        Debug.DrawLine(transform.position, transform.position + Vector3.forward * aoeRadius, Color.red, 1f);
        Debug.DrawLine(transform.position, transform.position + Vector3.back * aoeRadius, Color.red, 1f);

        foreach (var hit in hits)
        {
            if (ignoreOwner && owner != null)
            {
                if (hit.transform == owner || hit.transform.IsChildOf(owner))
                    continue;
            }
            DealDamage(hit);
        }

        if (hitEffect != null)
        {
            ParticleSystem ps = Instantiate(hitEffect, transform.position, Quaternion.identity);
            if (ps != null)
            {
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(ps.gameObject, 2f);
            }
        }

        Destroy(gameObject);
    }

    void FireHitScan()
    {
        if (_laserFired) return;
        _laserFired = true;

        Ray ray = new Ray(transform.position, _dir);
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            maxDistance,
            hitMask,
            QueryTriggerInteraction.Collide
        );

        // sort by distance biar visual di hit pertama
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (ignoreOwner && owner != null)
            {
                if (hit.collider.transform == owner || hit.collider.transform.IsChildOf(owner))
                    continue;
            }
            Debug.Log("HitScan hit: " + hit.collider.name);
            DealDamage(hit.collider);
        }

        // efek di semua tag enemy yang kena
        if (hits.Length > 0 && hitEffect != null)
        {
            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.CompareTag("Enemy"))
                {
                    ParticleSystem ps = Instantiate(hitEffect, hit.point, Quaternion.identity);
                    if (ps != null)
                    {
                        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
                    }
                    else
                    {
                        Destroy(ps.gameObject, 2f);
                    }
                }
            }
        }

        // optional: line renderer buat visual beam
        if (laserRenderer != null)
        {
            laserRenderer.positionCount = 2;
            laserRenderer.SetPosition(0, transform.position);

            Vector3 end = hits.Length > 0
                ? hits[hits.Length - 1].point
                : transform.position + _dir * maxDistance;

            laserRenderer.SetPosition(1, end);
        }

        Destroy(gameObject, laserDuration);

        // kalau cuma flash 1 frame, bisa langsung destroy:
        // Destroy(gameObject);
        // kalau mau line fade, biarin hidup sampai lifeTime habis.
    }
}
