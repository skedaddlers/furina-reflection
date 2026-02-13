using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class ShopUI : MonoBehaviour
{
    [Header("Main UI")]
    public GameObject shopPanel;
    public GameObject detailPanel;
    public Button closeButton;
    public Button buyButton;
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

    private void Start()
    {
        closeButton.onClick.AddListener(CloseShop);
        shopPanel.SetActive(false);
        detailPanel.SetActive(false);
    }

    #region Shop Open

    public void OpenShopUI(List<WeaponBase> weapons, List<SkillBase> skills)
    {
        OpenPanel();
        SetupWeapons(weapons);
        SetupSkills(skills);
    }

    private void OpenPanel()
    {
        shopPanel.SetActive(true);
        shopPanel.transform.localScale = Vector3.zero;
        shopPanel.transform.DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);

        GameManager.Instance.ChangeState(GameState.InMenu);
        GameManager.Instance.SetCursorState(true);
        detailPanel.SetActive(false);
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

            weaponButtons[i].onClick.RemoveAllListeners();

            bool owned = Player.Instance.GetComponent<PlayerLoadout>().HasWeapon(weapon);
            SetItemState(weaponImages[i], weaponButtons[i], owned);

            if (!owned)
            {
                int index = i;
                weaponButtons[i].onClick.AddListener(() =>
                    ShowWeaponDetails(weapon, index));
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

        SetItemState(weaponImages[index], weaponButtons[index], true);
        detailPanel.SetActive(false);
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


            skillButtons[i].onClick.RemoveAllListeners();

            bool owned = Player.Instance.GetComponent<SkillManager>().HasSkill(skill);
            SetItemState(skillImages[i], skillButtons[i], owned);

            if (!owned)
            {
                int index = i;
                skillButtons[i].onClick.AddListener(() =>
                    ShowSkillDetails(skill, index));
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

        PlayerStats.Instance.SpendGold(skill.price);
        Player.Instance.GetComponent<SkillManager>().AddSkill(skill);

        SetItemState(skillImages[index], skillButtons[index], true);
        detailPanel.SetActive(false);
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

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(onBuy);
    }

    private void SetItemState(Image image, Button button, bool owned)
    {
        image.color = owned ? Color.gray : Color.white;
        button.interactable = !owned;
    }

    #endregion

    #region Close

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        GameManager.Instance.SetCursorState(false);
        GameManager.Instance.ChangeState(GameState.Playing);
    }

    #endregion
}
