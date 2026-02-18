using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Item : MonoBehaviour
{
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public GameObject itemPrefab;

    public void SetVisibleInWorld(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }
    
    public virtual bool TryUse(GameObject user)
    {
        Debug.Log($"Using item: {itemName}");
        return true;
    }
}