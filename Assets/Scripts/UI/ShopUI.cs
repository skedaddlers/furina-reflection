using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("Main UI")]
    public GameObject shopPanel;
    public GameObject detailPanel;
    public Button closeButton;
    public Button buyButton;
    public TextMeshProUGUI currentGoldText;
    public float rarityColorAlpha = 0.5f;

    [Header("Weapon UI")]
    public List<GameObject> weaponSlots;
    public List<Button> weaponButtons;
    public List<Image> weaponImages;
    public List<Image> weaponRarityImageOverlay;
    public List<TextMeshProUGUI> weaponNames;
    public List<TextMeshProUGUI> weaponPrices;

    [Header("Skill UI")]
    public List<GameObject> skillSlots;
    public List<Button> skillButtons;
    public List<Image> skillImages;
    public List<Image> skillRarityImageOverlay;
    public List<TextMeshProUGUI> skillNames;
    public List<TextMeshProUGUI> skillPrices;

    [Header("Detail Panel")]
    public Image detailImage;
    public Image detailRarityOverlay;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailRarityText;
    public TextMeshProUGUI detailDescriptionText;
    public TextMeshProUGUI detailPriceText;
    public TextMeshProUGUI detailGoodPropertyText;
    public TextMeshProUGUI detailBadPropertyText;

    [Header("SFX")]
    public AudioClip buySound;

    private bool isOpen = false;
    public bool IsOpen => isOpen;

    private readonly Dictionary<Button, UnityAction> dynamicButtonActions = new Dictionary<Button, UnityAction>();

    private void Start()
    {
        closeButton.onClick.RemoveListener(CloseShop);
        closeButton.onClick.AddListener(CloseShop);
        shopPanel.SetActive(false);
        detailPanel.SetActive(false);
        UpdateGoldDisplay();
    }

    private void OnDestroy()
    {
        foreach (var kvp in dynamicButtonActions)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                kvp.Key.onClick.RemoveListener(kvp.Value);
            }
        }

        dynamicButtonActions.Clear();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseShop);
        }
    }

    #region Shop Open

    public void OpenShopUI(List<WeaponBase> weapons, List<SkillBase> skills)
    {
        if (!OpenPanel())
        {
            return;
        }

        SetupWeapons(weapons);
        SetupSkills(skills);
        UpdateGoldDisplay();
    }

    private bool OpenPanel()
    {
        if (UIManager.Instance != null && !UIManager.Instance.TryOpenMenu(this))
        {
            return false;
        }

        shopPanel.OpenPanel();
        GameManager.Instance.player.GetComponent<PlayerController>().ResetAllStates();
        detailPanel.SetActive(false);
        isOpen = true;
        return true;
    }

    private void UpdateGoldDisplay()
    {
        currentGoldText.text = PlayerStats.Instance.Gold.ToString();
    }

    #endregion

    #region Weapons

    private void SetupWeapons(List<WeaponBase> weapons)
    {
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            if (i >= weapons.Count)
            {
                weaponSlots[i].SetActive(false);
                continue;
            }

            WeaponBase weapon = weapons[i];
            weaponSlots[i].SetActive(true);

            weaponImages[i].sprite = weapon.icon;
            weaponNames[i].text = weapon.weaponName;
            weaponPrices[i].text = weapon.price.ToString();
            Color rarityColor = Helpers.GetColorForRarity(weapon.rarity);
            rarityColor.a = rarityColorAlpha;
            weaponRarityImageOverlay[i].color = rarityColor;

            ClearDynamicButtonAction(weaponButtons[i]);

            bool owned = Player.Instance.GetComponent<PlayerLoadout>().HasWeapon(weapon);
            SetItemState(weaponImages[i], weaponButtons[i], owned);

            if (!owned)
            {
                int index = i;
                SetDynamicButtonAction(weaponButtons[i], () => ShowWeaponDetails(weapon, index));
            }
        }
    }

    private void ShowWeaponDetails(WeaponBase weapon, int index)
    {
        ShowDetails(
            weapon.icon,
            Helpers.GetColorForRarity(weapon.rarity),
            weapon.weaponName,
            weapon.description,
            weapon.price,
            weapon.goodPropertyText,
            weapon.badPropertyText,
            () => BuyWeapon(weapon, index)
        );
    }

    private void BuyWeapon(WeaponBase weapon, int index)
    {
        if (!PlayerStats.Instance.CanAfford(weapon.price))
        {
            UIManager.Instance.ShowNotification("Not enough gold!", 2f);
            return;
        }

        PlayerStats.Instance.SpendGold(weapon.price);
        Player.Instance.GetComponent<PlayerLoadout>().AddToLoadout(weapon);
        UpdateGoldDisplay();
        SetItemState(weaponImages[index], weaponButtons[index], true);
        detailPanel.SetActive(false);
        if (buySound != null)
        {
            AudioManager.Instance?.PlaySFXNoOverlap(buySound);
        }
    }

    #endregion

    #region Skills

    private void SetupSkills(List<SkillBase> skills)
    {
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (i >= skills.Count)
            {
                skillSlots[i].SetActive(false);
                continue;
            }

            SkillBase skill = skills[i];
            skillSlots[i].SetActive(true);

            skillImages[i].sprite = skill.skillIcon;
            skillNames[i].text = skill.skillName;
            skillPrices[i].text = skill.price.ToString();
            Color rarityColor = Helpers.GetColorForRarity(skill.rarity);
            rarityColor.a = rarityColorAlpha;
            skillRarityImageOverlay[i].color = rarityColor;


            ClearDynamicButtonAction(skillButtons[i]);

            bool owned = Player.Instance.GetComponent<SkillManager>().HasSkill(skill);
            SetItemState(skillImages[i], skillButtons[i], owned);

            if (!owned)
            {
                int index = i;
                SetDynamicButtonAction(skillButtons[i], () => ShowSkillDetails(skill, index));
            }
        }
    }

    private void ShowSkillDetails(SkillBase skill, int index)
    {
        ShowDetails(
            skill.skillIcon,
            Helpers.GetColorForRarity(skill.rarity),
            skill.skillName,
            skill.description,
            skill.price,
            skill.goodPropertyText,
            skill.badPropertyText,
            () => BuySkill(skill, index)
        );
    }

    private void BuySkill(SkillBase skill, int index)
    {
        if (!PlayerStats.Instance.CanAfford(skill.price))
        {
            UIManager.Instance.ShowNotification("Not enough gold!", 2f);
            return;
        }

        if (Player.Instance.GetComponent<SkillManager>().HasSkill(skill))
        {
            UIManager.Instance.ShowNotification("You already own this skill!", 2f);
            return;
        }

        if (Player.Instance.GetComponent<SkillManager>().IsAtSkillLimit())
        {
            UIManager.Instance.ShowNotification("You can't carry more skills!", 2f);
            return;
        }

        PlayerStats.Instance.SpendGold(skill.price);
        Player.Instance.GetComponent<SkillManager>().AddSkill(skill);
        UpdateGoldDisplay();
        SetItemState(skillImages[index], skillButtons[index], true);
        detailPanel.SetActive(false);
        if (buySound != null)
        {
            AudioManager.Instance?.PlaySFXNoOverlap(buySound);
        }
    }

    #endregion

    #region Detail Panel

    private void ShowDetails(
        Sprite icon,
        Color rarityColor,
        string name,
        string description,
        int price,
        string good,
        string bad,
        UnityEngine.Events.UnityAction onBuy)
    {
        detailPanel.SetActive(true);

        detailImage.sprite = icon;
        detailRarityText.text = Helpers.GetRarityNameFromColor(rarityColor);
        rarityColor.a = rarityColorAlpha;
        detailRarityOverlay.color = rarityColor;
        detailNameText.text = name;
        detailDescriptionText.text = description;
        detailPriceText.text = price.ToString();
        detailGoodPropertyText.text = good;
        detailBadPropertyText.text = bad;

        SetDynamicButtonAction(buyButton, onBuy);
    }

    private void SetItemState(Image image, Button button, bool owned)
    {
        image.color = owned ? Color.gray : Color.white;
        button.interactable = !owned;
    }

    private void SetDynamicButtonAction(Button button, UnityAction action)
    {
        if (button == null)
            return;

        ClearDynamicButtonAction(button);

        if (action == null)
            return;

        dynamicButtonActions[button] = action;
        button.onClick.AddListener(action);
    }

    private void ClearDynamicButtonAction(Button button)
    {
        if (button == null)
            return;

        if (dynamicButtonActions.TryGetValue(button, out UnityAction existingAction) && existingAction != null)
        {
            button.onClick.RemoveListener(existingAction);
        }

        dynamicButtonActions.Remove(button);
    }

    #endregion

    #region Close

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseMenu(this);
        }
        isOpen = false;
    }

    #endregion
}
