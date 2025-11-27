using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

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
        shopPanel.transform.localScale = Vector3.zero;
        shopPanel.transform.DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
        GameManager.Instance.ChangeState(GameState.InMenu);
        GameManager.Instance.SetCursorState(true);
        // Setup weapon slots
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            if (i < weaponsForSale.Count)
            {
                int index = i; // Capture index for the listener
                weaponBuyButtons[i].onClick.RemoveAllListeners();
                WeaponBase weapon = weaponsForSale[i];
                weaponSlots[i].SetActive(true);
                weaponImages[i].sprite = weapon.icon;
                weaponNames[i].text = weapon.weaponName;
                weaponPrices[i].text = weapon.price.ToString() + " Gold";
                weaponDescriptions[i].text = weapon.description;
                if(Player.Instance.GetComponent<PlayerLoadout>().HasWeapon(weaponsForSale[i]))
                {
                    weaponImages[i].color = Color.gray; // Indicate already owned weapon
                    weaponBuyButtons[i].interactable = false;
                    continue;
                }
                weaponBuyButtons[i].onClick.AddListener(() => {
                    // Implement purchase logic here
                    if (PlayerStats.Instance.CanAfford(weapon.price))
                    {
                        Debug.Log("Purchased: " + weapon.weaponName);
                        PlayerStats.Instance.SpendGold(weapon.price);
                        Player.Instance.GetComponent<PlayerLoadout>().AddToLoadout(weapon);
                        weaponImages[index].color = Color.gray; // Indicate already owned weapon
                        weaponBuyButtons[index].interactable = false;
                    }
                    else
                    {
                        // Not enough gold feedback
                    }
                    // Debug.Log("Purchased: " + weapon.weaponName);
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
                int index = i; // Capture index for the listener
                skillBuyButtons[i].onClick.RemoveAllListeners();
                SkillBase skill = skillsForSale[i];
                skillSlots[i].SetActive(true);
                skillImages[i].sprite = skill.skillIcon;
                skillNames[i].text = skill.skillName;
                skillPrices[i].text = skill.price.ToString() + " Gold";
                skillDescriptions[i].text = skill.description;
                if(Player.Instance.GetComponent<SkillManager>().HasSkill(skillsForSale[i]))
                {
                    skillImages[i].color = Color.gray; // Indicate already owned skill
                    skillBuyButtons[i].interactable = false;
                    continue;
                }
                skillBuyButtons[i].onClick.AddListener(() => {
                    // Implement purchase logic here
                    if (PlayerStats.Instance.CanAfford(skill.price))
                    {
                        Debug.Log("Purchased: " + skill.skillName);
                        PlayerStats.Instance.SpendGold(skill.price);
                        Player.Instance.GetComponent<SkillManager>().PurchaseSkill(skill);
                        skillImages[index].color = Color.gray; // Indicate already owned skill
                        skillBuyButtons[index   ].interactable = false;
                    }
                    else
                    {
                        // Not enough gold feedback
                    }
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