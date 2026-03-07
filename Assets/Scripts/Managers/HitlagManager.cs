using UnityEngine;
using System.Collections;

public class HitlagManager : MonoBehaviour
{
    private static HitlagManager _instance;
    public static HitlagManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("HitlagManager");
                _instance = go.AddComponent<HitlagManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public static void DestroyInstanceForRestart()
    {
        if (_instance == null) return;
        var go = _instance.gameObject;
        _instance = null;
        Object.Destroy(go);
    }

    private Coroutine _hitlagRoutine;
    private float _endTimeUnscaled;
    private float _targetTimeScale = 1f;
    private float _previousTimeScale = 1f;

    public void Trigger(float duration, float timeScale = 0.1f)
    {
        if (duration <= 0f) return;
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        timeScale = Mathf.Clamp01(timeScale);
        _targetTimeScale = Mathf.Min(_targetTimeScale, timeScale);
        _endTimeUnscaled = Mathf.Max(_endTimeUnscaled, Time.unscaledTime + duration);

        if (_hitlagRoutine == null)
        {
            _previousTimeScale = Time.timeScale;
            _hitlagRoutine = StartCoroutine(HitlagRoutine());
        }
    }

    private IEnumerator HitlagRoutine()
    {
        Time.timeScale = _targetTimeScale;

        while (Time.unscaledTime < _endTimeUnscaled)
        {
            yield return null;
        }

        _targetTimeScale = 1f;
        _endTimeUnscaled = 0f;
        _hitlagRoutine = null;

        if (GameManager.Instance == null || !GameManager.Instance.IsPaused)
        {
            Time.timeScale = _previousTimeScale;
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        Time.timeScale = 1f;
    }
}
