using UnityEngine;

[CreateAssetMenu(menuName = "DDA/Weapons/Melee")]
public class MeleeWeapon : WeaponBase
{
    public override void PerformAttack(PlayerCombat ctx)
    {
        Debug.Log($"Melee attack with {displayName}");
        float dmg = ctx.stats.RollDamage(baseDamage);
        ctx.MeleeConeHit(dmg, range, angle);
    }
}