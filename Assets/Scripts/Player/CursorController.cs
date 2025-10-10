using UnityEngine;

public class CursorController : MonoBehaviour
{
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                UnlockCursor();
            else
                LockCursor();
        }
    }

    public void UnlockCursor()
    {
        // Time.timeScale = 0f; // Pause the game
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LockCursor()
    {
        // Time.timeScale = 1f; // Resume the game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

}
