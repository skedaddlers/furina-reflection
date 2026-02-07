using UnityEngine;
using System.Collections.Generic;

public class ItemDetector : MonoBehaviour
{
    public Item nearestItem;

    void Update()
    {
        if (nearestItem != null && Input.GetKeyDown(KeyCode.E))
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
        UIManager.Instance.ShowInterractionUI(false, "");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Item item = other.GetComponent<Item>();
        if (item != null)
        {
            nearestItem = item;
            UIManager.Instance.ShowInterractionUI(true, $"Press 'E' to pick up {item.itemName}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Item item = other.GetComponent<Item>();
        if (item != null && item == nearestItem)
        {
            ClearNearestItem();
        }
    }
}