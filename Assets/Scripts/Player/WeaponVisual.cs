using UnityEngine;
using System.Collections.Generic;

public class WeaponVisual : MonoBehaviour
{
    public WeaponBase weapon;
    public List<WeaponPair> weaponVisuals;

    private PlayerLoadout playerLoadout;

    void Awake()
    {
        GetComponent<PlayerLoadout>().onWeaponChanged += (newWeapon) => {
            weapon = newWeapon;
            UpdateVisuals();
        };
    }
    void Start()
    {
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        foreach (var pair in weaponVisuals)
        {
            if (pair.weapon == weapon)
            {
                pair.visualObject.SetActive(true);
            }
            else
            {
                pair.visualObject.SetActive(false);
            }
        }
    }

}

[System.Serializable]
public class WeaponPair
{
    public WeaponBase weapon;
    public GameObject visualObject;
}