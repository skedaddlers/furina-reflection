using UnityEngine;
using System.Collections.Generic;

public class ItemDetector : MonoBehaviour
{
    public Item nearestItem;
    public WeaponDrop nearestWeaponDrop;

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E))
            return;

        if (nearestWeaponDrop != null)
        {
            PlayerLoadout loadout = GetComponent<PlayerLoadout>();
            if (loadout != null && loadout.TryPickupDroppedWeapon(nearestWeaponDrop))
            {
                nearestWeaponDrop = null;
                RefreshInteractionUI();
            }
            return;
        }

        if (nearestItem != null)
        {
            Inventory inventory = GetComponent<Inventory>();
            if (inventory != null)
            {
                bool added = inventory.TryAddItem(nearestItem);
                if (added)
                {
                    nearestItem.SetVisibleInWorld(false);
                    ClearNearestItem();
                }
            }
        }
    }

    public void ClearNearestItem()
    {
        nearestItem = null;
        RefreshInteractionUI();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        WeaponDrop weaponDrop = other.GetComponent<WeaponDrop>();
        if (weaponDrop != null)
        {
            nearestWeaponDrop = weaponDrop;
            RefreshInteractionUI();
            return;
        }

        Item item = other.GetComponent<Item>();
        if (item != null)
        {
            nearestItem = item;
            RefreshInteractionUI();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        WeaponDrop weaponDrop = other.GetComponent<WeaponDrop>();
        if (weaponDrop != null && weaponDrop == nearestWeaponDrop)
        {
            nearestWeaponDrop = null;
            RefreshInteractionUI();
            return;
        }

        Item item = other.GetComponent<Item>();
        if (item != null && item == nearestItem)
        {
            ClearNearestItem();
        }
    }

    private void RefreshInteractionUI()
    {
        if (nearestWeaponDrop != null)
        {
            UIManager.Instance.ShowInterractionUI(true, nearestWeaponDrop.GetPickupPrompt());
            return;
        }

        if (nearestItem != null)
        {
            UIManager.Instance.ShowInterractionUI(true, $"Press E to pick up {nearestItem.itemName}");
            return;
        }

        UIManager.Instance.ShowInterractionUI(false, "");
    }
}
