using UnityEngine;

public enum WeaponAnimType { Melee, OneHandGun, TwoHandGun, Bow, ChannelLaser, BombThrow }

[CreateAssetMenu(menuName="DDA/Weapons/Animation Set")]
public class WeaponAnimationSet : ScriptableObject
{
    [Header("Animator Routing")]
    public WeaponAnimType type = WeaponAnimType.Melee;
    public string attackTrigger = "Attack";   // melee / bow release / bomb throw
    public string shootTrigger  = "Shoot";    // pistol/AR style
    public string channelBool   = "Channel";  // laser on/off
    public string equipTrigger  = "Equip";    // optional

    [Header("Override Controller (optional)")]
    public AnimatorOverrideController overrideController;

    [Header("Timings (seconds)")]
    public float hitEventTime = 0.2f;     // untuk melee (frame impact)
    public float fireEventTime = 0.0f;    // untuk ranged (boleh 0, event dari anim)
    public float channelWindup = 0.15f;   // laser mulai menyala
    public float channelCooldown = 0.1f;  // laser padam

    [Header("Upper Body Layer?")]
    public bool useUpperBodyLayerForAttacks = true;
}
