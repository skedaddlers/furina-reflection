using UnityEngine;

[CreateAssetMenu(menuName = "DDA/Weapons/Melee")]
public class MeleeWeapon : WeaponBase
{
    public override void PerformAttack(PlayerCombat ctx)
    {
        // Debug.Log($"Melee attack with {weaponName}");
        float baseDmg = baseDamage + ctx.stats.baseAttack;
        ctx.MeleeConeHit(baseDmg, range, angle);
    }
}
