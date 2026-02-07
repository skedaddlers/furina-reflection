using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel;
    public Button closeButton;
    public GameObject slotParent;
    public List<Button> itemSlots;
    public List<Image> itemImages;
    public GameObject itemDetailPanel;
    public Image itemIconImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public Button useItemButton;
    public Button dropItemButton;

    private Item selectedItem;

    private void Start()
    {
        closeButton.onClick.AddListener(CloseInventory);
        var allSlots = slotParent.GetComponentsInChildren<Button>();
        itemSlots = new List<Button>();
        itemImages = new List<Image>();
        for (int i = 0; i < allSlots.Length; i++)
        {
            itemSlots.Add(allSlots[i]);
            Image img = allSlots[i].GetComponentsInChildren<Image>()[1];
            itemImages.Add(img);
            int index = i; // Capture index for the listener
            itemSlots[i].onClick.AddListener(() => {
                OnItemSlotClicked(index);
            });
        }
        inventoryPanel.SetActive(false);
    }

    private void OnItemSlotClicked(int index)
    {
        Inventory inventory = Player.Instance.GetComponent<Inventory>();
        if (inventory == null || index >= inventory.Items.Count)
            return;

        selectedItem = inventory.Items[index];
        itemDetailPanel.SetActive(true);
        itemIconImage.sprite = selectedItem.itemIcon;
        itemNameText.text = selectedItem.itemName;
        itemDescriptionText.text = selectedItem.itemDescription;

        useItemButton.onClick.RemoveAllListeners();
        useItemButton.onClick.AddListener(() => {
            inventory.UseItem(selectedItem);
            CloseInventory();
        });

        dropItemButton.onClick.RemoveAllListeners();
        dropItemButton.onClick.AddListener(() => {
            inventory.RemoveItem(selectedItem);
            CloseInventory();
        });
    }

    public void OpenInventoryUI(List<Item> items)
    {
        inventoryPanel.SetActive(true);
        GameManager.Instance.ChangeState(GameState.InMenu);
        GameManager.Instance.SetCursorState(true);
        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (i < items.Count)
            {
                itemImages[i].sprite = items[i].itemIcon;
                itemImages[i].enabled = true;
            }
            else
            {
                itemImages[i].sprite = null;
                itemImages[i].enabled = false;
            }
        }
        itemDetailPanel.SetActive(false);
    }

    public void CloseInventory()
    {
        inventoryPanel.SetActive(false);
        GameManager.Instance.ChangeState(GameState.Playing);
        GameManager.Instance.SetCursorState(false);
    }
}
