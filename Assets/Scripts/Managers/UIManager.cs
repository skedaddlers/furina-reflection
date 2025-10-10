using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public CursorController cursorController;
    [Header("Door Interaction UI")]
    public CanvasGroup doorPromptCanvas;
    public TextMeshProUGUI doorPromptText; // atau pakai Text biasa kalau belum pakai TMP

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

        ShowInterractionUI(false, "");        
    }

    void Start()
    {
        if (cursorController == null)
            cursorController = GetComponent<CursorController>();
        cursorController.LockCursor();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (cursorController != null)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                    cursorController.UnlockCursor();
                else
                    cursorController.LockCursor();
            }
        }   
    }


    public void ShowInterractionUI(bool show, string promptText = "")
    {
        doorPromptCanvas.alpha = show ? 1 : 0;
        doorPromptCanvas.blocksRaycasts = show;
        doorPromptCanvas.interactable = show;
        doorPromptText.text = promptText;
    }

}