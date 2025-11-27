using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class SkillsUI : MonoBehaviour
{
    [Header("UI References")]
    public Image[] skillIcons;
    public TextMeshProUGUI[] manaCosts;
    public TextMeshProUGUI[] cooldownTexts;
    public Image[] cooldownOverlays;
    public SkillManager skillManager;
    
    void Start()
    {
        if (skillManager == null)
        {
            skillManager = FindObjectOfType<SkillManager>();
        }
        UpdateSkillsUI(skillManager.ActiveSkillSlots);
    }

    void Update()
    {
        foreach (var slot in skillManager.ActiveSkillSlots)
        {
            if(slot.isOnCooldown)
            {
                UpdateCooldownUI(slot);
            }
        }
    }

    void UpdateCooldownUI(SkillSlot slot)
    {
        for (int i = 0; i < skillManager.ActiveSkillSlots.Length; i++)
        {
            if (skillManager.ActiveSkillSlots[i] == slot)
            {
                cooldownTexts[i].text = Mathf.CeilToInt(slot.currentCooldown).ToString();
                cooldownOverlays[i].fillAmount = slot.currentCooldown / slot.skill.cooldownTime;
            }
        }
    }
    public void UpdateSkillsUI(SkillSlot[] activeSkillSlots)
    {
        for (int i = 0; i < skillIcons.Length; i++)
        {
            if (i < activeSkillSlots.Length && activeSkillSlots[i].skill != null)
            {
                skillIcons[i].enabled = true;
                manaCosts[i].enabled = true;
                cooldownTexts[i].enabled = true;
                cooldownOverlays[i].enabled = true;
                SkillBase skill = activeSkillSlots[i].skill;
                skillIcons[i].sprite = skill.skillIcon;
                manaCosts[i].text = skill.manaCost.ToString();
                cooldownTexts[i].text = activeSkillSlots[i].isOnCooldown ? Mathf.CeilToInt(activeSkillSlots[i].currentCooldown).ToString() : "";
                cooldownOverlays[i].fillAmount = activeSkillSlots[i].currentCooldown / activeSkillSlots[i].skill.cooldownTime;
            }
            else
            {
                skillIcons[i].enabled = false;
                manaCosts[i].enabled = false;
                cooldownTexts[i].enabled = false;
                cooldownOverlays[i].enabled = false;
                skillIcons[i].sprite = null;
                manaCosts[i].text = "";
                cooldownTexts[i].text = "";
                cooldownOverlays[i].fillAmount = 0f;
            }
        }
    }



}