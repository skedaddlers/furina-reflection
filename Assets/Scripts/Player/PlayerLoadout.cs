using System.Collections.Generic;
using UnityEngine;

public class PlayerLoadout : MonoBehaviour
{
    public PlayerAnimationBinder animBinder;
    public PlayerCombat combat;   // punyamu
    public WeaponBase current;    // senjata terpakai sekarang
    public WeaponBase[] loadout = new WeaponBase[2];
    public int maxLoadoutSize = 2;

    void Awake()
    {
        if (!animBinder) animBinder = GetComponent<PlayerAnimationBinder>();
        if (!combat) combat = GetComponent<PlayerCombat>();
        if (loadout.Length > 0)
        {
            Equip(0);
        }
    }

    public void Equip(int index)
    {
        if (index < 0 || index >= loadout.Length) return;
    
        current = loadout[index];
        // sinkron ke combat & anim
        if (animBinder) animBinder.ApplyAnimSet(current.animSet);
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
