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

    [Header("Attack Settings")]
    private bool _isAttacking = false;
    public bool IsAttacking => _isAttacking;

    void Awake()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (animBinder == null) animBinder = GetComponent<PlayerAnimationBinder>();
        if (loadout == null) loadout = GetComponent<PlayerLoadout>();
        if (loadout != null && loadout.loadout.Length > 0)
        {
            loadout.Equip(0);
        }
    }

    // Dipanggil oleh anim event atau input (lihat PlayerController patch)
    public void UseWeapon()
    {
        if (loadout.current == null || stats == null) return;
        if (Time.time < _lastUseTime + loadout.current.cooldown) return;
        if (!stats.TrySpendMana(loadout.current.manaCost)) return;

        loadout.current.PerformAttack(this);
        _lastUseTime = Time.time;
        StartCoroutine(ResetAttackAfterCooldown(loadout.current.cooldown));
        // Debug.Log($"Used weapon {loadout.current.name} at time {_lastUseTime}");
    }

    // ==== UTIL UNTUK WEAPON ====
    private IEnumerator ResetAttackAfterCooldown(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        SetIsAttacking(false);
        Debug.Log("Attack cooldown finished, IsAttacking reset to false.");
    }
    // Melee cone hit: deteksi musuh dalam jarak & sudut
    public void MeleeConeHit(float damage, float range, float angle)
    {
        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position + transform.forward * 0.5f;
        Vector3 forward = (attackOrigin != null ? attackOrigin.forward : transform.forward).normalized;

        Collider[] hits = Physics.OverlapSphere(origin, range, enemyMask);
        Debug.Log($"MeleeConeHit found {hits.Length} targets");
        foreach (var h in hits)
        {
            Debug.Log($"MeleeConeHit found: {h.name}");
            Vector3 dir = (h.transform.position - origin);
            dir.y = 0f;
            float ang = Vector3.Angle(forward, dir);
            if (ang <= angle * 0.5f)
            {
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

    public void SetIsAttacking(bool val)
    {
        _isAttacking = val;
    }
}
