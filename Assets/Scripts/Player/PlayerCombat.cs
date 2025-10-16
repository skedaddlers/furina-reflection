using UnityEngine;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [Header("Refs")]
    public Transform attackOrigin; // arah serangan (mis: titik depan player / socket senjata)
    public LayerMask enemyMask;
    public PlayerStats stats; // NEW

    [Header("Weapon Loadout")]
    public WeaponBase currentWeapon;         // NEW
    public List<WeaponBase> loadout = new List<WeaponBase>(); // optional, untuk switch
    public int currentIndex = 0;
    private float _lastUseTime = -999f;

    void Awake()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (currentWeapon == null && loadout.Count > 0)
        {
            currentWeapon = loadout[0];
            currentIndex = 0;
        }
    }

    void Update()
    {
        // Optional: switch dengan tombol nomor
        if (loadout.Count > 0)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) EquipIndex(0);
            if (Input.GetKeyDown(KeyCode.Alpha2) && loadout.Count > 1) EquipIndex(1);
            if (Input.GetKeyDown(KeyCode.Alpha3) && loadout.Count > 2) EquipIndex(2);
        }
    }

    public void EquipIndex(int idx)
    {
        idx = Mathf.Clamp(idx, 0, loadout.Count - 1);
        currentIndex = idx;
        currentWeapon = loadout[idx];
        // TODO: update UI nama senjata bila perlu
    }

    // Dipanggil oleh anim event atau input (lihat PlayerController patch)
    public void UseWeapon()
    {
        if (currentWeapon == null || stats == null) return;
        if (Time.time < _lastUseTime + currentWeapon.cooldown) return;
        if (!stats.TrySpendMana(currentWeapon.manaCost)) return;

        currentWeapon.PerformAttack(this);
        _lastUseTime = Time.time;
    }

    // ==== UTIL UNTUK WEAPON ====

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
}
