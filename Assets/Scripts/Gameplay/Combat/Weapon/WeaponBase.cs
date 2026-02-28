using UnityEngine;

public abstract class WeaponBase : ScriptableObject
{
    [Header("Info")]
    public string id = "weapon_id";
    public string weaponName = "Weapon";
    public Rarity rarity = Rarity.Common;
    [TextArea] public string description;
    [TextArea] public string goodPropertyText;
    [TextArea] public string badPropertyText;
    public Sprite icon;
    [Tooltip("3D world model used when this weapon is dropped on the ground.")]
    public GameObject worldDropPrefab;
    public int price = 100;

    [Header("Stats")]
    public int baseDamage = 5;
    public float range = 2.0f;       // melee / ray range
    public float angle = 60f;        // untuk melee cone
    public float cooldown = 0.4f;    // detik
    public int manaCost = 0;

    [Header("Optional Projectile")]
    public GameObject projectilePrefab; // null kalau melee
    public float projectileSpeed = 20f;
    public float projectileLifeTime = 4f;

    [Header("Optional Animation")]
    public WeaponAnimationSet animSet; // optional, untuk override animasi player

    public abstract void PerformAttack(PlayerCombat ctx);
}

