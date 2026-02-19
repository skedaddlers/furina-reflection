using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public int upgradesToBeDone = 0;
    public int randomSkillUpgradeEveryXLevels = 5;

    public void OnLevelUp(int newLevel)
    {
        upgradesToBeDone++;
        UIManager.Instance.levelUpUI.ShowLevelUp(upgradesToBeDone);
        if (newLevel % randomSkillUpgradeEveryXLevels == 0)
        {
            GetComponent<SkillManager>()?.TryUpgradeRandomSkill();
        }
    }

    public void ResetUpgradeAmount()
    {
        upgradesToBeDone = 0;
    }
    
}