using UnityEngine;
using UnityEngine.UI;

// Component for individual room icons
public class MinimapRoomIcon : MonoBehaviour
{
    public int roomId;
    public Image iconImage;
    public Color baseColor;
    private bool isVisited = false;
    private bool isCurrent = false;

    public void SetVisited(bool visited)
    {
        isVisited = visited;
        UpdateVisual();
    }

    public void SetCurrent(bool current)
    {
        isCurrent = current;
        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (isCurrent)
        {
            iconImage.color = Color.white;
            transform.localScale = Vector3.one * 1.3f;
        }
        else if (isVisited)
        {
            iconImage.color = baseColor;
            transform.localScale = Vector3.one;
        }
        else
        {
            iconImage.color = Color.Lerp(baseColor, new Color(0.3f, 0.3f, 0.3f, 0.5f), 0.7f);
            transform.localScale = Vector3.one * 0.8f;
        }
    }
}