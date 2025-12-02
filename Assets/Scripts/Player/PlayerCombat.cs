using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Refs")]
    public Transform attackOrigin; // arah serangan (mis: titik depan player / socket senjata)
    public LayerMask enemyMask;
    public PlayerStats stats; // NEW
    public Transform aimCamera;
    public LayerMask hitMask;

    [Header("Weapon Loadout")]
    public PlayerAnimationBinder animBinder;
    public PlayerLoadout loadout;
    public int currentIndex = 0;
    private float _lastUseTime = -999f;

    [Header("Visual Effects")]
    public ParticleSystem hitEffect;

    [Header("Attack Settings")]
    private bool _isAttacking = false;
    public bool IsAttacking => _isAttacking;

    public static PlayerCombat Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (animBinder == null) animBinder = GetComponent<PlayerAnimationBinder>();
        if (loadout == null) loadout = GetComponent<PlayerLoadout>();
        // if (loadout != null && loadout.loadout.Length > 0)
        // {
        //     loadout.Equip(0);
        // }
        if (aimCamera == null)
        {
            var cam = Camera.main;
            if (cam != null) aimCamera = cam.transform;
        }
    }

    public bool TryUseWeapon()
    {
        if (loadout.current == null || stats == null) return false;

        float cd = loadout.current.cooldown;
        if (Time.time < _lastUseTime + cd) return false;

        if (!stats.TrySpendMana(loadout.current.manaCost))
            return false;

        _lastUseTime = Time.time;

        _isAttacking = true;

        return true;
    }

    // Dipanggil oleh anim event atau input (lihat PlayerController patch)
    public void TriggerAttackHit()
    {
        if (loadout.current == null) return;
        loadout.current.PerformAttack(this);
    }
    public void EndAttack()
    {
        _isAttacking = false;
    }

    public void ForceCancelAttack()
    {
        _isAttacking = false;
    }

    // ==== UTIL UNTUK WEAPON ====

    // Melee cone hit: deteksi musuh dalam jarak & sudut
    public void MeleeConeHit(float damage, float range, float angle)
    {
        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position + transform.forward * 0.5f;
        Vector3 forward = (attackOrigin != null ? attackOrigin.forward : transform.forward).normalized;

        Collider[] hits = Physics.OverlapSphere(origin, range, enemyMask);
        foreach (var h in hits)
        {
            Vector3 dir = (h.transform.position - origin);
            dir.y = 0f;
            float ang = Vector3.Angle(forward, dir);
            if (ang <= angle * 0.5f)
            {
                GameObject particle = Instantiate(hitEffect.gameObject, h.ClosestPoint(origin), Quaternion.identity);
                var health = h.GetComponent<Health>();
                if (health != null) health.TakeDamage(damage);
            }
        }
    }

    // Projectile fire
    public void FireProjectile(
        GameObject projPrefab,
        float speed,
        float lifeTime,
        float damage,
        float maxRange,
        bool useCameraAim = false
    )
    {
        if (projPrefab == null) return;

        Transform muzzle = attackOrigin != null ? attackOrigin : transform;

        Vector3 dir;

        if (useCameraAim && aimCamera != null)
        {
            // Ray dari kamera lewat crosshair (tengah layar)
            Ray ray = new Ray(aimCamera.position, aimCamera.forward);
            RaycastHit hit;
            Vector3 targetPoint;

            // 1000f jarak max, layerMask bebas (bisa kamu ganti kalau mau)
            if (Physics.Raycast(ray, out hit, 1000f, hitMask))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = aimCamera.position + aimCamera.forward * 1000f;
            }

            dir = (targetPoint - muzzle.position).normalized;
        }
        else
        {
            // default: tembak lurus dari muzzle
            dir = muzzle.forward;
        }

        GameObject go = Instantiate(projPrefab, muzzle.position, Quaternion.LookRotation(dir));
        var proj = go.GetComponent<Projectile>();
        if (proj == null) proj = go.AddComponent<Projectile>();
        proj.Init(dir, speed, lifeTime, damage);

        Debug.Log($"Fired projectile {go.name} with dir {dir}");
    }

    public void FireLaser(float damage, float range, float radius, bool useCameraAim = false)
    {
        Transform origin = attackOrigin != null ? attackOrigin : transform;

        Vector3 dir;
        if (useCameraAim && aimCamera != null)
        {
            dir = aimCamera.forward;
        }
        else
        {
            dir = origin.forward;
        }

        Vector3 start = origin.position;

        // Kalau mau agak "tebal" pakai SphereCast, kalau mau line tipis pakai RaycastAll
        RaycastHit[] hits = Physics.SphereCastAll(
            start,
            radius,
            dir,
            range,
            enemyMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (var hit in hits)
        {
            var health = hit.collider.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }

            // optional: VFX di titik hit
            if (hitEffect != null)
            {
                Instantiate(hitEffect.gameObject, hit.point, Quaternion.LookRotation(-dir));
            }
        }

        Debug.Log($"Laser fired from {start} dir {dir}, hit {hits.Length} colliders");
    }
    

    public void SetIsAttacking(bool val)
    {
        _isAttacking = val;
    }
}
