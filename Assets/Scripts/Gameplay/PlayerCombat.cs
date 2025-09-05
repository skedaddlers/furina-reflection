using UnityEngine;

[System.Serializable]
public class WeaponStats
{
    public string name;
    public int damage;
    public float range;
    public float angle;
}

public class PlayerCombat : MonoBehaviour
{
    public WeaponStats currentWeapon;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {

    }

    public void CheckHit()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, currentWeapon.range);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Vector3 dirToEnemy = (hit.transform.position - transform.position).normalized;
                float angleToEnemy = Vector3.Angle(transform.forward, dirToEnemy);

                if (angleToEnemy < currentWeapon.angle / 2f)
                {
                    hit.GetComponent<Health>()?.TakeDamage(currentWeapon.damage);
                }
            }
        }
    }

    // Gambar gizmo attack cone di Scene View
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, currentWeapon.range);

        Vector3 rightBoundary = Quaternion.Euler(0, currentWeapon.angle / 2f, 0) * transform.forward * currentWeapon.range;
        Vector3 leftBoundary = Quaternion.Euler(0, -currentWeapon.angle / 2f, 0) * transform.forward * currentWeapon.range;

        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
    }
}
