using UnityEngine;
using UnityEngine.UI;

public class CursorTexture : MonoBehaviour
{
    public Texture2D defaultCursor;

    void Start()
    {
        SetDefaultCursor();
    }

    public void SetDefaultCursor()
    {
        Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
    }
}