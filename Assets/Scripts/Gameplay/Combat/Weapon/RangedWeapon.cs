using UnityEngine;

[CreateAssetMenu(menuName = "DDA/Weapons/Ranged")]
public class RangedWeapon : WeaponBase
{
    public override void PerformAttack(PlayerCombat ctx)
    {
        if (projectilePrefab == null) return;
        float dmg = ctx.stats.RollDamage(baseDamage + ctx.stats.baseAttack);
        ctx.FireProjectile(projectilePrefab, projectileSpeed, projectileLifeTime, dmg, range);
    }
}