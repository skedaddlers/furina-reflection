using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SalonSolitaire", menuName = "Furina/Skills/Salon Solitaire")]
public class SalonSolitaire : SkillBase
{
    [Header("Salon Member Settings")]
    public GameObject[] salonMemberPrefabs;
    public int memberCount = 3;
    public float spawnRadius = 2f;
    public float attackRange = 10f;
    public float attackInterval = 1.5f;
    public float projectileSpeed = 15f;
    public string enemyTag = "Enemy";
    public string playerTag = "Player";
    public string targetTag = "Enemy";

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;

    private List<GameObject> activeSalonMembers = new List<GameObject>();
    private bool isActive = false;

    private void OnEnable()
    {
        isActive = false;
        activeSalonMembers.Clear();
    }

    public override void OnSkillActivate(GameObject caster)
    {
        base.OnSkillActivate(caster);

        if (isActive) return;

        isActive = true;

        // Debug.Log($"{skillName} activated by {caster.name}");

        // Play cast sound
        if (castSound != null)
        {
            AudioManager.Instance?.PlayVoiceLine(castSound);
        }

        if(caster.CompareTag(playerTag))
        {
            targetTag = enemyTag;
        }
        else
        {
            targetTag = playerTag;
        }

        // Spawn salon members around the caster
        SpawnSalonMembers(caster);

        // Start the skill duration coroutine
        MonoBehaviour casterMono = caster.GetComponent<MonoBehaviour>();
        if (casterMono != null)
        {
            casterMono.StartCoroutine(SalonMembersDuration(caster));
        }
    }

    private void SpawnSalonMembers(GameObject caster)
    {
        if (isUpgraded)
        {
            memberCount = salonMemberPrefabs.Length;
        }
        else
        {
            memberCount = salonMemberPrefabs.Length - 1;
        }
        for (int i = 0; i < memberCount; i++)
        {
            // Calculate spawn position around the caster
            float angle = i * (360f / memberCount) * Mathf.Deg2Rad;
            Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * spawnRadius;
            Vector3 spawnPosition = caster.transform.position + spawnOffset;

            GameObject member;

            // Spawn prefab or create dummy if no prefab assigned
            if (salonMemberPrefabs[i] != null)
            {
                GameObject salonMemberPrefab = salonMemberPrefabs[i];
                member = Object.Instantiate(salonMemberPrefab, spawnPosition, Quaternion.identity);
                // set rotatation the same as the prefab
                member.transform.rotation = salonMemberPrefab.transform.rotation;
            }
            else
            {
                member = CreateDummySalonMember(spawnPosition, i);
            }

            // Add SalonMemberBehaviour component
            SalonMemberBehaviour behaviour = member.GetComponent<SalonMemberBehaviour>();
            if (behaviour == null)
            {
                behaviour = member.AddComponent<SalonMemberBehaviour>();
            }

            // Initialize the salon member
            behaviour.Initialize(caster, this);

            activeSalonMembers.Add(member);

            // Debug.Log($"Spawned Salon Member {i + 1} at {spawnPosition}");
        }
    }

    private GameObject CreateDummySalonMember(Vector3 position, int index)
    {
        // Create a dummy object (capsule) as placeholder
        GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        dummy.name = $"SalonMember_{index + 1}";
        dummy.transform.position = position;
        dummy.transform.localScale = new Vector3(0.5f, 0.75f, 0.5f);

        // Set color to distinguish members
        Renderer renderer = dummy.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            // Different colors for each member
            Color[] colors = { Color.cyan, Color.magenta, Color.yellow };
            mat.color = colors[index % colors.Length];
            renderer.material = mat;
        }

        // Remove collider so it doesn't block player
        Collider col = dummy.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        return dummy;
    }

    private IEnumerator SalonMembersDuration(GameObject caster)
    {
        float elapsed = 0f;

        while (elapsed < duration && isActive)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        OnSkillEnd(caster);
    }

    public override void OnSkillEnd(GameObject caster)
    {
        if (!isActive) return;

        base.OnSkillEnd(caster);

        // Destroy all salon members
        foreach (GameObject member in activeSalonMembers)
        {
            if (member != null)
            {
                Object.Destroy(member);
            }
        }
        activeSalonMembers.Clear();

        isActive = false;

        // Debug.Log($"{skillName} ended - Salon members dismissed");
    }

    public override bool CanUseSkill(GameObject caster)
    {
        if (isActive) return false;

        PlayerStats playerStats = caster.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            return playerStats.CurrentMana >= manaCost;
        }
        return base.CanUseSkill(caster);
    }
}