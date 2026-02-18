using UnityEngine;
using System.Collections.Generic;
public class Inventory : MonoBehaviour
{
    public int maxCapacity = 20;
    public float itemUsageCooldown = 3f;
    private float lastItemUsageTime = -Mathf.Infinity;
    [SerializeField]
    private List<Item> items = new List<Item>();

    public List<Item> Items => items;

    void Start()
    {
        Debug.Log("Inventory initialized.");
    }
    public bool TryAddItem(Item item)
    {
        if (items.Count >= maxCapacity)
        {
            UIManager.Instance.ShowNotification("Inventory is full!");
            return false;
        }
        items.Add(item);
        Debug.Log($"Added item to inventory: {item.itemName}");
        return true;
    }

    public void RemoveItem(Item item)
    {
        if (items.Remove(item))
        {
            Debug.Log($"Removed item from inventory: {item.itemName}");
        }
        else
        {
            Debug.Log($"Item not found in inventory: {item.itemName}");
        }
        if (item != null)
        {
            item.SetVisibleInWorld(true);
            Vector3 dropPosition = transform.position + transform.forward * 1.5f;
            item.SetPosition(dropPosition);
        }
    }

    public void UseItem(Item item)
    {
        if (items.Contains(item))
        {
            if (Time.time - lastItemUsageTime >= itemUsageCooldown)
            {
                bool success = item.TryUse(gameObject);
                if (success)
                {
                    Destroy(item.gameObject);
                    items.Remove(item);
                    lastItemUsageTime = Time.time;
                }
            }
            else
            {
                UIManager.Instance.ShowNotification("Wait before using another item!");
            }
        }
        else
        {
            Debug.Log($"Item not in inventory: {item.itemName}");
        }
    }
}