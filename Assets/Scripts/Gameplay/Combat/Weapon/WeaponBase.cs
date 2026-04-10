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
    public AudioClip attackSound;
    public GameObject attackEffectPrefab;
    public Vector3 attackEffectOffset;
    public Vector3 attackEffectRotation;
    public float effectDuration = 1f;

    [Header("Stats")]
    public int baseDamage = 5;
    public float range = 2.0f;       // melee / ray range
    public float angle = 60f;        // untuk melee cone
    public float cooldown = 0.4f;    // detik
    public int manaCost = 0;
    public bool causesStagger = false;
    public float staggerDuration = 0.5f;
    public bool causesKnockback = false;
    public float knockbackDistance = 1f;

    [Header("Optional Projectile")]
    public GameObject projectilePrefab; // null kalau melee
    public float projectileSpeed = 20f;
    public float projectileLifeTime = 4f;

    [Header("Optional Animation")]
    public WeaponAnimationSet animSet; // optional, untuk override animasi player

    [Header("Optional Voice Effects")]
    public AudioClip[] voiceLines; // optional, untuk suara saat menyerang
    public float voiceLineChance = 0.3f; // peluang untuk memutar suara saat menyerang

    public abstract void PerformAttack(PlayerCombat ctx);

    public void PlayVoiceLineOnAttack()
    {
        if (Random.value < voiceLineChance && voiceLines != null && voiceLines.Length > 0)
        {
            AudioClip clip = voiceLines[Random.Range(0, voiceLines.Length)];
            AudioManager.Instance?.PlayVoiceLine(clip);
        }
    }
}

