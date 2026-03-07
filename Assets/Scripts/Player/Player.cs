using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool HasNoWeaponsOrSkills()
    {
        PlayerLoadout loadout = GetComponent<PlayerLoadout>();
        if (loadout != null && loadout.HasWeapons())
        {
            return false; // Player has weapons
        }
        SkillManager skillManager = GetComponent<SkillManager>();
        if (skillManager != null && skillManager.HasSkills())
        {
            return false; // Player has skills
        }
        return true;
    }
}
