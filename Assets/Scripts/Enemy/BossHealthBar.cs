using UnityEngine;

public class BossHealthBar : MonoBehaviour
{
    public string bossName = "Boss Name";
    private Health _health;
    public Health Health => _health;

    void Awake()
    {
        _health = GetComponent<Health>();
    }

    void Start()
    {
        if (_health != null)
        {
            _health.onHealthChanged += HandleHealthChanged;
        }
    }

    void OnDestroy()
    {
        if (_health != null)
            _health.onHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (UIManager.Instance == null || UIManager.Instance.bossHPBarUI == null)
            return;

        UIManager.Instance.bossHPBarUI.UpdateHealth(current, max);
    }
}
