using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class Shop : MonoBehaviour
{
    public List<WeaponBase> weaponsForSale;
    public List<SkillBase> skillsForSale;
    public Room parentRoom;
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
        // Example: Populate shop with random items from library
        var allWeapons = Helpers.GetRandomItems(Library.Instance.allWeapons, 3, parentRoom.roomIndex + (int)System.DateTime.Now.Ticks);
        var allSkills = Helpers.GetRandomItems(Library.Instance.allSkills, 3, parentRoom.roomIndex + (int)System.DateTime.Now.Ticks);

        // For simplicity, just add first 3 weapons and skills
        for (int i = 0; i < 3 && i < allWeapons.Count; i++)
        {
            weaponsForSale.Add(allWeapons[i]);
        }

        for (int i = 0; i < 3 && i < allSkills.Count; i++)
        {
            skillsForSale.Add(allSkills[i]);
        }
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
}