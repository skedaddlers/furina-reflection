using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSFX : MonoBehaviour
{
    public AudioClip clickSFX;
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {
        button.onClick.AddListener(PlaySound);
    }

    void OnEnable()
    {
        button.onClick.AddListener(PlaySound);
    }

    void OnDisable()
    {
        button.onClick.RemoveListener(PlaySound);
    }

    void PlaySound()
    {
        AudioManager.Instance?.PlaySFX(clickSFX);
    }
}