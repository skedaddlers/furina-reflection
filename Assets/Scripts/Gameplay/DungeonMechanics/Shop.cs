using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
public class Shop : MonoBehaviour
{
    public List<WeaponBase> weaponsForSale;
    public List<SkillBase> skillsForSale;
    public Room parentRoom;

    [Header("Shop Slots")]
    [SerializeField] private int weaponSlots = 3;
    [SerializeField] private int skillSlots = 3;

    [Header("Rarity Weights")]
    [SerializeField] private float commonBaseWeight = 1f;
    [SerializeField] private float rareBaseWeight = 0.45f;
    [SerializeField] private float epicBaseWeight = 0.18f;
    [SerializeField] private float legendaryBaseWeight = 0.05f;
    [SerializeField] private float commonWeightDecayPerDistance = 0.05f;
    [SerializeField] private float higherRarityBoostPerDistance = 0.12f;
    [SerializeField] private float minimumCommonWeightMultiplier = 0.35f;

    private Transform model;
    private Animator animator;
    private Player player;

    private bool isPlayerInRange = false;

    void Start()
    {
        parentRoom = GetComponentInParent<Room>();
        model = GetComponent<Transform>();
        animator = GetComponent<Animator>();
        player = GameManager.Instance.player;
        PopulateShopInventory();
    }

    void Update()
    {
        // if in range of collider and press interact key
        if (Input.GetKeyDown(KeyCode.E) && isPlayerInRange && !UIManager.Instance.shopUI.IsOpen)
        {
            player.GetComponent<PlayerController>().ResetAllStates();
            UIManager.Instance.shopUI.OpenShopUI(weaponsForSale, skillsForSale);
            UIManager.Instance.ShowInterractionUI(false, "");
        }
        if(animator != null)
        {
            FacePlayer();
        }
    }

    void FacePlayer()
    {
        Vector3 direction = (player.transform.position - model.position).normalized;
        direction.y = 0; 
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        float angle = Vector3.SignedAngle(model.forward, direction, Vector3.up);
        if(Mathf.Abs(angle) < 30f){
            animator.SetTrigger("Idle");
            return; // No need to rotate if already facing player
        }
        StopAllCoroutines();
        StartCoroutine(RotateTowards(lookRotation, angle));
    }

    private IEnumerator RotateTowards(Quaternion targetRotation, float angle)
    {
        while (Mathf.Abs(Vector3.SignedAngle(model.forward, targetRotation * Vector3.forward, Vector3.up)) > 30f)
        {
            if (angle > 1f) animator.SetTrigger("TurnRight");
            else if (angle < -1f) animator.SetTrigger("TurnLeft");
            else animator.SetTrigger("Idle");
            yield return null;
        }
        animator.SetTrigger("Idle");

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            UIManager.Instance.ShowInterractionUI(true, "Press E to open Shop");     
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            UIManager.Instance.ShowInterractionUI(false, "");     
            UIManager.Instance.shopUI.CloseShop();
        }
    }

    private void PopulateShopInventory()
    {
        if (weaponsForSale == null)
            weaponsForSale = new List<WeaponBase>();
        else
            weaponsForSale.Clear();

        if (skillsForSale == null)
            skillsForSale = new List<SkillBase>();
        else
            skillsForSale.Clear();

        var library = Library.Instance;
        if (library == null)
        {
            Debug.LogWarning("[Shop] Library.Instance is null. Shop inventory could not be generated.");
            return;
        }

        int baseSeed = unchecked((parentRoom != null ? parentRoom.roomIndex : GetInstanceID()) * 397 ^ Environment.TickCount);
        var selectedWeapons = Helpers.GetWeightedRandomItems(library.allWeapons, weaponSlots, GetWeaponWeight, baseSeed + 17);
        var selectedSkills = Helpers.GetWeightedRandomItems(library.allSkills, skillSlots, GetSkillWeight, baseSeed + 53);

        weaponsForSale.AddRange(selectedWeapons);
        skillsForSale.AddRange(selectedSkills);
    }

    private float GetWeaponWeight(WeaponBase weapon)
    {
        return weapon == null ? 0f : GetRarityWeight(weapon.rarity);
    }

    private float GetSkillWeight(SkillBase skill)
    {
        return skill == null ? 0f : GetRarityWeight(skill.rarity);
    }

    private float GetRarityWeight(Rarity rarity)
    {
        int shopDistance = parentRoom != null ? Mathf.Max(0, parentRoom.distanceFromStart) : 0;

        return rarity switch
        {
            Rarity.Common => commonBaseWeight * Mathf.Max(minimumCommonWeightMultiplier, 1f - shopDistance * commonWeightDecayPerDistance),
            Rarity.Rare => rareBaseWeight * (1f + shopDistance * higherRarityBoostPerDistance),
            Rarity.Epic => epicBaseWeight * (1f + shopDistance * higherRarityBoostPerDistance * 2f),
            Rarity.Legendary => legendaryBaseWeight * (1f + shopDistance * higherRarityBoostPerDistance * 3f),
            _ => 1f
        };
    }
}
