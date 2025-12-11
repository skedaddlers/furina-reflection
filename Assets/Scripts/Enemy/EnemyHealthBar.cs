using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple world-space health bar for enemies. Attach to an enemy prefab that has a Health component.
/// Requires a child Canvas (World Space or Screen Space - Camera) with a Slider assigned.
/// Hides the bar when HP is full.
/// </summary>
[RequireComponent(typeof(Health))]
public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    public Slider healthSlider;
    public Canvas healthCanvas;

    [Header("Display")]
    public Vector3 worldOffset = new Vector3(0, 2f, 0);
    public bool hideWhenFull = true;

    private Health _health;
    private Transform _mainCamera;

    void Awake()
    {
        _health = GetComponent<Health>();
        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>();
        }
        if (healthCanvas == null && healthSlider != null)
        {
            healthCanvas = healthSlider.GetComponentInParent<Canvas>();
        }
    }

    void Start()
    {
        if (_health != null)
        {
            _health.onHealthChanged += HandleHealthChanged;
            HandleHealthChanged(_health.GetCurrentHealth(), _health.maxHealth);
        }

        if (Camera.main != null)
        {
            _mainCamera = Camera.main.transform;
        }
    }

    void OnDestroy()
    {
        if (_health != null)
        {
            _health.onHealthChanged -= HandleHealthChanged;
        }
    }

    void LateUpdate()
    {
        if (healthCanvas == null || _health == null) return;

        // Position above enemy
        healthCanvas.transform.position = transform.position + worldOffset;

        // Always face camera
        if (_mainCamera != null)
        {
            healthCanvas.transform.rotation = Quaternion.LookRotation(healthCanvas.transform.position - _mainCamera.position);
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (healthSlider == null) return;

        healthSlider.maxValue = max;
        healthSlider.value = current;

        if (hideWhenFull && healthCanvas != null)
        {
            bool full = Mathf.Approximately(current, max);
            healthCanvas.enabled = !full;
        }
    }
}
