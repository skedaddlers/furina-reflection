using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSFX : MonoBehaviour
{
    public AudioClip clickSFX;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(PlaySound);
    }

    void OnEnable()
    {
        GetComponent<Button>().onClick.AddListener(PlaySound);
    }

    void OnDisable()
    {
        GetComponent<Button>().onClick.RemoveListener(PlaySound);
    }

    void PlaySound()
    {
        AudioManager.Instance?.PlaySFX(clickSFX);
    }
}