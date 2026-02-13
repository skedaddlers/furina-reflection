using UnityEngine;
using TMPro;

public class DamageNumberUI : MonoBehaviour
{
    // 3D text object prefab for damage numbers
    public GameObject damageNumberPrefab;

    [Header("Spawn Jitter")]
    [Tooltip("Random horizontal offset radius range (world XZ).")]
    public Vector2 horizontalOffsetRange = new Vector2(0.0f, 0.35f);
    [Tooltip("Random height offset range above the target.")]
    public Vector2 heightOffsetRange = new Vector2(1.3f, 1.8f);

    [Header("Rotation Jitter")]
    [Tooltip("Random roll around camera-facing axis (degrees).")]
    public Vector2 rollRange = new Vector2(-12f, 12f);

    [Header("Scale & Lifetime")]
    [Tooltip("Random scale multiplier range.")]
    public Vector2 scaleRange = new Vector2(0.95f, 1.1f);
    [Tooltip("Random lifetime range (seconds).")]
    public Vector2 lifetimeRange = new Vector2(0.9f, 1.3f);

    [Header("Crit Styling")]
    public bool boldOnCrit = true;
    public float critScaleMultiplier = 1.25f;
    public bool tintCritColor = true;
    public Color critColor = new Color(1f, 0.85f, 0.2f, 1f);

    // Method to show damage popup
    public void ShowDamagePopup(float damageAmount, Vector3 position, bool isCrit = false)
    {
        if (damageNumberPrefab == null)
        {
            Debug.LogWarning("DamageNumberUI: damageNumberPrefab is not assigned.");
            return;
        }

        float minH = Mathf.Min(heightOffsetRange.x, heightOffsetRange.y);
        float maxH = Mathf.Max(heightOffsetRange.x, heightOffsetRange.y);
        float minR = Mathf.Min(horizontalOffsetRange.x, horizontalOffsetRange.y);
        float maxR = Mathf.Max(horizontalOffsetRange.x, horizontalOffsetRange.y);

        Vector2 circle = Random.insideUnitCircle;
        if (circle == Vector2.zero) circle = Vector2.right;
        float radius = Random.Range(minR, maxR);
        Vector3 offset = new Vector3(circle.x, 0f, circle.y) * radius;
        offset += Vector3.up * Random.Range(minH, maxH);

        // Instantiate the damage number prefab at the specified position with jitter
        GameObject damagePopup = Instantiate(damageNumberPrefab, position + offset, Quaternion.identity);
        
        // Set the text to show the damage amount
        TextMeshPro textMesh = damagePopup.GetComponentInChildren<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.text = Mathf.RoundToInt(damageAmount).ToString();
            if (isCrit)
            {
                if (boldOnCrit)
                {
                    textMesh.fontStyle |= FontStyles.Bold;
                }
                if (tintCritColor)
                {
                    textMesh.color = critColor;
                }
            }
        }

        float minS = Mathf.Min(scaleRange.x, scaleRange.y);
        float maxS = Mathf.Max(scaleRange.x, scaleRange.y);
        float scale = Random.Range(minS, maxS);
        if (isCrit)
        {
            scale *= Mathf.Max(0.01f, critScaleMultiplier);
        }
        damagePopup.transform.localScale *= scale;

        float minRoll = Mathf.Min(rollRange.x, rollRange.y);
        float maxRoll = Mathf.Max(rollRange.x, rollRange.y);
        float roll = Random.Range(minRoll, maxRoll);
        var billboard = damagePopup.GetComponent<DamageNumberBillboard>();
        if (billboard == null) billboard = damagePopup.AddComponent<DamageNumberBillboard>();
        billboard.rollDegrees = roll;

        float minLife = Mathf.Min(lifetimeRange.x, lifetimeRange.y);
        float maxLife = Mathf.Max(lifetimeRange.x, lifetimeRange.y);
        Destroy(damagePopup, Random.Range(minLife, maxLife));
    }
}
