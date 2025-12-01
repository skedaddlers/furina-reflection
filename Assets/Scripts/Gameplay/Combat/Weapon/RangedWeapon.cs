using UnityEngine;

[CreateAssetMenu(menuName = "DDA/Weapons/Ranged")]
public class RangedWeapon : WeaponBase
{
    public bool useCameraAim = false;   // centang untuk bow

    public override void PerformAttack(PlayerCombat ctx)
    {
        if (projectilePrefab == null) return;
        float dmg = ctx.stats.RollDamage(baseDamage);

        bool aimWithCam = useCameraAim;

        // kalau mau auto aktif khusus Bow:
        if (animSet != null && animSet.type == WeaponAnimType.Bow)
            aimWithCam = true;

        ctx.FireProjectile(
            projectilePrefab,
            projectileSpeed,
            projectileLifeTime,
            dmg,
            range,
            aimWithCam
        );
    }
}
