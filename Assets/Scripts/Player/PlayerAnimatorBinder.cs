using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationBinder : MonoBehaviour
{
    public Animator animator;
    [Tooltip("Layer index untuk UpperBody (buat nembak sambil jalan).")]
    public int upperBodyLayerIndex = 1; // buatkan layer ini di Animator
    [Range(0,1f)] public float upperBodyWeight = 1f;

    [Header("Runtime")]
    public WeaponAnimationSet currentAnimSet;

    void Reset(){ animator = GetComponent<Animator>(); }

    public void ApplyAnimSet(WeaponAnimationSet set)
    {
        currentAnimSet = set;
        if (set != null && set.overrideController != null)
            animator.runtimeAnimatorController = set.overrideController;

        // atur layer upper body
        float w = (set != null && set.useUpperBodyLayerForAttacks) ? upperBodyWeight : 0f;
        if (upperBodyLayerIndex >= 0 && upperBodyLayerIndex < animator.layerCount)
            animator.SetLayerWeight(upperBodyLayerIndex, w);

        // optional: equip pose
        if (set != null && !string.IsNullOrEmpty(set.equipTrigger))
            animator.SetTrigger(set.equipTrigger);
    }

    // ==== dipanggil dari input/weapon logic ====
    public void PlayAttack()
    {
        if (currentAnimSet == null) return;
        animator.ResetTrigger(currentAnimSet.shootTrigger);
        animator.SetTrigger(currentAnimSet.attackTrigger);
    }

    public void PlayShoot()
    {
        if (currentAnimSet == null) return;
        animator.ResetTrigger(currentAnimSet.attackTrigger);
        animator.SetTrigger(currentAnimSet.shootTrigger);
    }

    public void SetChannel(bool on)
    {
        if (currentAnimSet == null) return;
        if (!string.IsNullOrEmpty(currentAnimSet.channelBool))
            animator.SetBool(currentAnimSet.channelBool, on);
    }

    public void SetAim(bool isAiming)
    {
        if (animator == null) return;
        animator.SetBool("IsAiming", isAiming);
    }
}
