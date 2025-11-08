using UnityEngine;
using UnityEngine.UI;

public class WeaponUI : MonoBehaviour
{
    public Image currentWeaponIcon;
    public Image otherWeaponIcon;
    
    public void UpdateWeaponIcon(PlayerLoadout loadout)
    {
        if (loadout == null) return;

        if (loadout.current != null)
        {
            currentWeaponIcon.sprite = loadout.current.icon;
            currentWeaponIcon.color = Color.white;
        }
        else
        {
            currentWeaponIcon.sprite = null;
            currentWeaponIcon.color = new Color(1, 1, 1, 0); // transparan
        }

        // Cari senjata lain di loadout
        WeaponBase otherWeapon = null;
        foreach (var w in loadout.loadout)
        {
            if (w != loadout.current)
            {
                otherWeapon = w;
                break;
            }
        }

        if (otherWeapon != null)
        {
            otherWeaponIcon.sprite = otherWeapon.icon;
            otherWeaponIcon.color = Color.white;
        }
        else
        {
            otherWeaponIcon.sprite = null;
            otherWeaponIcon.color = new Color(1, 1, 1, 0); // transparan
        }
    }
}