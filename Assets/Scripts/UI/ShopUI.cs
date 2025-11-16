using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    public GameObject shopPanel;
    public Button closeButton;
    public List <GameObject> weaponSlots;
    public List <GameObject> skillSlots;
    public List<Image> weaponImages;
    public List<Image> skillImages;
    public List<TextMeshProUGUI> weaponNames;
    public List<TextMeshProUGUI> skillNames;
    public List<TextMeshProUGUI> weaponPrices;
    public List<TextMeshProUGUI> skillPrices;
    public List<TextMeshProUGUI> weaponDescriptions;
    public List<TextMeshProUGUI> skillDescriptions;
    public List<Button> weaponBuyButtons;
    public List<Button> skillBuyButtons;

    private void Start()
    {
        closeButton.onClick.AddListener(CloseShop);
        shopPanel.SetActive(false);
    }

    public void OpenShopUI(List<WeaponBase> weaponsForSale, List<SkillBase> skillsForSale)
    {
        shopPanel.SetActive(true);
        GameManager.Instance.ChangeState(GameState.Paused);
        GameManager.Instance.SetCursorState(true);
        // Setup weapon slots
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            if (i < weaponsForSale.Count)
            {
                WeaponBase weapon = weaponsForSale[i];
                weaponSlots[i].SetActive(true);
                weaponImages[i].sprite = weapon.icon;
                weaponNames[i].text = weapon.weaponName;
                weaponPrices[i].text = weapon.price.ToString() + " Gold";
                weaponDescriptions[i].text = weapon.description;
                weaponBuyButtons[i].onClick.RemoveAllListeners();
                weaponBuyButtons[i].onClick.AddListener(() => {
                    // Implement purchase logic here
                    Debug.Log("Purchased: " + weapon.weaponName);
                });
            }
            else
            {
                weaponSlots[i].SetActive(false);
            }
        }

        // Setup skill slots
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (i < skillsForSale.Count)
            {
                SkillBase skill = skillsForSale[i];
                skillSlots[i].SetActive(true);
                skillImages[i].sprite = skill.skillIcon;
                skillNames[i].text = skill.skillName;
                skillPrices[i].text = skill.price.ToString() + " Gold";
                skillDescriptions[i].text = skill.description;
                skillBuyButtons[i].onClick.RemoveAllListeners();
                skillBuyButtons[i].onClick.AddListener(() => {
                    // Implement purchase logic here
                    Debug.Log("Purchased: " + skill.skillName);
                });
            }
            else
            {
                skillSlots[i].SetActive(false);
            }
        }
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        GameManager.Instance.ChangeState(GameState.Playing);
        GameManager.Instance.SetCursorState(false);
    }
}