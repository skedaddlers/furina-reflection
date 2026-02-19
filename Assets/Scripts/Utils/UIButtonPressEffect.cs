using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonPressEffect : MonoBehaviour, IPointerDownHandler
{
    public enum PressEffectMode
    {
        ButtonPress,
        PunchScale
    }

    [Header("Effect")]
    [SerializeField] private PressEffectMode effectMode = PressEffectMode.ButtonPress;

    [Header("ButtonPress Settings")]
    [SerializeField] private float buttonPressScale = 0.95f;
    [SerializeField] private float buttonPressDuration = 0.1f;

    [Header("PunchScale Settings")]
    [SerializeField] private Vector3 punchStrength = new Vector3(0.08f, 0.08f, 0f);
    [SerializeField] private float punchDuration = 0.2f;
    [SerializeField] private int punchVibrato = 6;
    [SerializeField] private float punchElasticity = 0.6f;

    private Button button;
    private bool triggeredByPointer;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        button.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }

        triggeredByPointer = false;
    }

    private void LateUpdate()
    {
        triggeredByPointer = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanPlayEffect())
        {
            return;
        }

        triggeredByPointer = true;
        PlayEffect();
    }

    private void HandleClick()
    {
        if (!CanPlayEffect())
        {
            return;
        }

        // Prevent double trigger for normal mouse/touch click
        // because pointer down already played the animation.
        if (triggeredByPointer)
        {
            return;
        }

        PlayEffect();
    }

    private bool CanPlayEffect()
    {
        return button != null && button.interactable && gameObject.activeInHierarchy;
    }

    public void SetEffectMode(PressEffectMode mode)
    {
        effectMode = mode;
    }

    private void PlayEffect()
    {
        if (effectMode == PressEffectMode.PunchScale)
        {
            transform.PunchScale(punchStrength, punchDuration, punchVibrato, punchElasticity);
            return;
        }

        transform.ButtonPress(buttonPressScale, buttonPressDuration);
    }
}
