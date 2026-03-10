using UnityEngine;

public interface IStaggerable
{
    bool IsStaggered { get; }
    void ApplyStagger(StaggerInfo info);
}

public struct StaggerInfo
{
    public float duration;
    public bool causesKnockback;
    public float knockbackDistance;
    public Vector3 hitOrigin;
    public bool hasHitOrigin;

    public StaggerInfo(
        float duration,
        bool causesKnockback,
        float knockbackDistance,
        Vector3 hitOrigin,
        bool hasHitOrigin
    )
    {
        this.duration = duration;
        this.causesKnockback = causesKnockback;
        this.knockbackDistance = knockbackDistance;
        this.hitOrigin = hitOrigin;
        this.hasHitOrigin = hasHitOrigin;
    }

    public Vector3 ResolveKnockbackDirection(Transform target)
    {
        if (target == null)
            return Vector3.zero;

        if (hasHitOrigin)
        {
            Vector3 dir = target.position - hitOrigin;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                return dir.normalized;
        }

        Vector3 fallback = -target.forward;
        fallback.y = 0f;
        if (fallback.sqrMagnitude <= 0.0001f)
            fallback = Vector3.back;

        return fallback.normalized;
    }
}
