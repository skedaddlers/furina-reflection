using System.Collections.Generic;
using UnityEngine;

public class PlayerLoadout : MonoBehaviour
{
    public PlayerAnimationBinder animBinder;
    public PlayerCombat combat;   // punyamu
    public WeaponBase current;    // senjata terpakai sekarang
    public WeaponBase[] loadout = new WeaponBase[2];
    private int maxLoadoutSize = 2;

    // event to subscribe when weapon changes
    public delegate void OnWeaponChanged(WeaponBase newWeapon);
    public event OnWeaponChanged onWeaponChanged;

    void Awake()
    {
        if (!animBinder) animBinder = GetComponent<PlayerAnimationBinder>();
        if (!combat) combat = GetComponent<PlayerCombat>();
    }

    void Start()
    {
        if (loadout[0] != null)
        {
            current = loadout[0];
            if (animBinder) animBinder.ApplyAnimSet(current.animSet);
        }
    }

    public void Equip(int index)
    {
        if (index < 0 || index >= loadout.Length) return;

        current = loadout[index];
        onWeaponChanged?.Invoke(current);
        UIManager.Instance.weaponUI?.UpdateWeaponIcon(this);
        // sinkron ke combat & anim
        if (animBinder) animBinder.ApplyAnimSet(current.animSet);
    }

    public void Swap()
    {
        if (loadout[1] == null) return;
        int currentIndex = System.Array.IndexOf(loadout, current);
        int newIndex = (currentIndex + 1) % loadout.Length;
        Equip(newIndex);
    }

    public void AddToLoadout(WeaponBase w)
    {
        AddToLoadoutInternal(w, null);
    }

    public bool TryPickupDroppedWeapon(WeaponDrop droppedWeapon)
    {
        if (droppedWeapon == null || droppedWeapon.Weapon == null) return false;

        WeaponBase pickedWeapon = droppedWeapon.Weapon;
        Vector3 swapDropPosition = droppedWeapon.transform.position;

        Destroy(droppedWeapon.gameObject);
        AddToLoadoutInternal(pickedWeapon, swapDropPosition);
        return true;
    }

    private void AddToLoadoutInternal(WeaponBase w, Vector3? swapDropPosition)
    {
        if (w == null) return;

        int currentIndex = System.Array.IndexOf(loadout, current);

        // Case spesifik: slot 0 isi, slot 1 kosong
        if (loadout[0] != null && loadout[1] == null)
        {
            loadout[1] = loadout[0]; // geser yang lama ke 1
            loadout[0] = w;          // senjata baru ke 0
            Equip(0);
            return;
        }

        // Kalau ada slot kosong biasa
        int emptyIndex = System.Array.IndexOf(loadout, null);
        if (emptyIndex != -1)
        {
            loadout[emptyIndex] = w;
            Equip(emptyIndex);
            return;
        }

        // Kalau full, replace current
        if (currentIndex == -1) currentIndex = 0;
        WeaponBase replacedWeapon = loadout[currentIndex];
        loadout[currentIndex] = w;
        Equip(currentIndex);

        if (replacedWeapon != null)
        {
            DropWeapon(replacedWeapon, swapDropPosition);
        }
    }

    public bool HasWeapon(WeaponBase w)
    {
        foreach (var weapon in loadout)
        {
            if (weapon == w) return true;
        }
        return false;
    }

    public bool HasWeapons()
    {
        foreach (var weapon in loadout)
        {
            if (weapon != null) return true;
        }
        return false;
    }

    private void DropWeapon(WeaponBase weaponToDrop, Vector3? overrideDropPosition = null)
    {
        if (weaponToDrop == null) return;

        Vector3 dropPosition = overrideDropPosition ?? (transform.position + transform.forward * 1.5f);
        WeaponDrop.Spawn(weaponToDrop, dropPosition);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification($"{weaponToDrop.weaponName} dropped to make space.", 2f);
        }
    }
}
