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

    [Header("Hitlag")]
    public bool enableHitlag = true;
    [Range(0.01f, 0.5f)]
    public float hitlagDuration = 0.05f;
    [Range(0f, 1f)]
    public float hitlagTimeScale = 0.1f;

    [Header("Attack Settings")]
    private bool _isAttacking = false;
    public bool IsAttacking => _isAttacking;

    public static PlayerCombat Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
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

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
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
        // if (!_isAttacking) return;
        if (loadout.current == null) return;
        loadout.current.PerformAttack(this);
        if (loadout.current.attackEffectPrefab != null)
        {
            Vector3 effectPos = (attackOrigin != null ? attackOrigin.position : transform.position) + loadout.current.attackEffectOffset;
            Quaternion effectRot = Quaternion.Euler(loadout.current.attackEffectRotation);
            GameObject effect = Instantiate(loadout.current.attackEffectPrefab, effectPos, transform.rotation * effectRot);
            Destroy(effect, loadout.current.effectDuration); // Hapus efek setelah 1 detik (sesuaikan dengan durasi partikel)
        }
        if (loadout.current.attackSound != null)
        {
            AudioManager.Instance.PlayWithVaryingPitch(loadout.current.attackSound);
        }
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
    public void MeleeConeHit(
        float damage, 
        float range, 
        float angle, 
        bool stagger = false, 
        float staggerDuration = 0.5f, 
        bool causesKnockback = false, 
        float knockbackDistance = 1f
    )
    {
        bool hasHit = false;
        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position + transform.forward * 0.5f;
        Vector3 forward = (attackOrigin != null ? attackOrigin.forward : transform.forward).normalized;

        Collider[] hits = Physics.OverlapSphere(origin, range, enemyMask);
        foreach (var h in hits)
        {
            var targetStats = h.GetComponent<EnemyStats>();
            bool didCrit;
            float finalDamage = Helpers.CalculateFinalDamage(
                damage,
                targetStats != null ? targetStats.defense : 0f,
                stats?.critRate ?? 0f,
                stats?.critMultiplier ?? 1f,
                stats != null ? stats.level - (targetStats != null ? targetStats.level : 0) : 0,
                stats != null ? stats.GetCurrentDamageBuffMultiplier() : 1f,
                out didCrit
            );
            Vector3 dir = (h.transform.position - origin);
            dir.y = 0f;
            float ang = Vector3.Angle(forward, dir);
            if (ang <= angle * 0.5f)
            {
                GameObject particle = Instantiate(hitEffect.gameObject, h.ClosestPoint(origin), Quaternion.identity);
                var health = h.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(
                        finalDamage,
                        didCrit,
                        DamageSource.Melee,
                        applyStagger: stagger,
                        staggerDuration: staggerDuration,
                        causesKnockback: causesKnockback,
                        knockbackDistance: knockbackDistance,
                        hitInstigator: transform
                    );
                }
                hasHit = true;
            }
        }
        if (hasHit && enableHitlag)
        {
            HitlagManager.Instance.Trigger(hitlagDuration, hitlagTimeScale);
        }
        PlayerActionTracker.Instance.RegisterMelee();
    }

    // Projectile fire
    public void FireProjectile(
        GameObject projPrefab,
        float speed,
        float lifeTime,
        float damage,
        float maxRange,
        bool useCameraAim = false,
        bool causesStagger = false,
        float staggerDuration = 0.5f,
        bool causesKnockback = false,
        float knockbackDistance = 1f
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
        proj.Init(
            dir, 
            speed, 
            lifeTime, 
            damage, 
            this.transform,
            causesStagger,
            staggerDuration,
            causesKnockback,
            knockbackDistance
        );
        PlayerActionTracker.Instance.RegisterRanged();

        // Debug.Log($"Fired projectile {go.name} with dir {dir}");
    }
    
    public void SetIsAttacking(bool val)
    {
        _isAttacking = val;
    }
}
