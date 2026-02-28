using UnityEngine;

public class WeaponDrop : MonoBehaviour
{
    [SerializeField] private WeaponBase weapon;
    [SerializeField] private float triggerRadius = 0.8f;
    [SerializeField] private Vector3 fallbackVisualScale = new Vector3(0.4f, 0.35f, 0.4f);
    [SerializeField] private Vector3 fallbackVisualOffset = new Vector3(0f, 0.2f, 0f);

    public WeaponBase Weapon => weapon;

    public static WeaponDrop Spawn(WeaponBase weaponToDrop, Vector3 position)
    {
        if (weaponToDrop == null) return null;

        GameObject dropObject = new GameObject($"DroppedWeapon_{weaponToDrop.weaponName}");
        dropObject.transform.position = position;

        WeaponDrop drop = dropObject.AddComponent<WeaponDrop>();
        drop.Initialize(weaponToDrop);

        return drop;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, 50f * Time.deltaTime);
    }

    public void Initialize(WeaponBase droppedWeapon)
    {
        weapon = droppedWeapon;
        EnsureTrigger();
        CreateVisual();
    }

    public string GetPickupPrompt()
    {
        if (weapon == null || string.IsNullOrEmpty(weapon.weaponName))
            return "Press E to pick up weapon";

        return $"Press E to pick up {weapon.weaponName}";
    }

    private void EnsureTrigger()
    {
        SphereCollider trigger = GetComponent<SphereCollider>();
        if (trigger == null)
            trigger = gameObject.AddComponent<SphereCollider>();

        trigger.isTrigger = true;
        trigger.radius = triggerRadius;
    }

    private void CreateVisual()
    {
        if (weapon != null && weapon.worldDropPrefab != null)
        {
            GameObject visual = Instantiate(weapon.worldDropPrefab, transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            return;
        }

        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fallback.name = "FallbackVisual";
        fallback.transform.SetParent(transform, false);
        fallback.transform.localPosition = fallbackVisualOffset;
        fallback.transform.localScale = fallbackVisualScale;

        Collider fallbackCollider = fallback.GetComponent<Collider>();
        if (fallbackCollider != null)
            Destroy(fallbackCollider);
    }
}
