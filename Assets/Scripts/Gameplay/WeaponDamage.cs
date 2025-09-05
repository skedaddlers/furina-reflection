using UnityEngine;

public class WeaponDamage : MonoBehaviour
{
    public int damage = 20;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<Health>()?.TakeDamage(damage);
        }
    }
}
