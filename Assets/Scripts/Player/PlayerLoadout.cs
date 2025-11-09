using System.Collections.Generic;
using UnityEngine;

public class PlayerLoadout : MonoBehaviour
{
    public PlayerAnimationBinder animBinder;
    public PlayerCombat combat;   // punyamu
    public WeaponBase current;    // senjata terpakai sekarang
    public WeaponBase[] loadout = new WeaponBase[2];
    private int maxLoadoutSize = 2;

    void Awake()
    {
        if (!animBinder) animBinder = GetComponent<PlayerAnimationBinder>();
        if (!combat) combat = GetComponent<PlayerCombat>();
    }

    void Start()
    {
        current = loadout[0];
        if (animBinder) animBinder.ApplyAnimSet(current.animSet);
    }

    public void Equip(int index)
    {
        if (index < 0 || index >= loadout.Length) return;

        current = loadout[index];
        UIManager.Instance.weaponUI?.UpdateWeaponIcon(this);
        // sinkron ke combat & anim
        if (animBinder) animBinder.ApplyAnimSet(current.animSet);
    }

    public void Swap()
    {
        if (loadout.Length < 2) return;
        int currentIndex = System.Array.IndexOf(loadout, current);
        int newIndex = (currentIndex + 1) % loadout.Length;
        Equip(newIndex);
    }

    public void AddToLoadout(WeaponBase w)
    {
        if (loadout.Length >= maxLoadoutSize)
        {
            loadout[System.Array.IndexOf(loadout, current)] = null;
        }
        loadout[System.Array.IndexOf(loadout, null)] = w;
        Equip(System.Array.IndexOf(loadout, w));
    }
}
