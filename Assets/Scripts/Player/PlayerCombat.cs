using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Refs")]
    public Transform attackOrigin; // arah serangan (mis: titik depan player / socket senjata)
    public LayerMask enemyMask;
    public PlayerStats stats; // NEW

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
    public void FireProjectile(GameObject projPrefab, float speed, float lifeTime, float damage, float maxRange)
    {
        if (projPrefab == null) return;
        Transform muzzle = attackOrigin != null ? attackOrigin : transform;
        Vector3 dir = muzzle.forward;

        GameObject go = Instantiate(projPrefab, muzzle.position, Quaternion.LookRotation(dir));
        var proj = go.GetComponent<Projectile>();
        if (proj == null) proj = go.AddComponent<Projectile>();
        proj.Init(dir, speed, lifeTime, damage);
        Debug.Log($"Fired projectile {go.name}");

        // // Optional: auto-destroy by distance (fallback)
        // if (maxRange > 0f)
        // {
        //     Destroy(go, Mathf.Max(lifeTime, maxRange / Mathf.Max(0.1f, speed)));
        // }
    }

    // Called through animation event
    

    public void SetIsAttacking(bool val)
    {
        _isAttacking = val;
    }
}
