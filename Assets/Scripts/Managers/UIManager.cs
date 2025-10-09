using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public GameObject goToNextRoomButton;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        goToNextRoomButton.SetActive(false);
    }

    public void ShowGoToNextRoomButton()
    {
        goToNextRoomButton.SetActive(true);
    }

}