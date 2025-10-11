using UnityEngine;
using UnityEngine.UI;
[System.Serializable]
public class MinimapRoomIconPrefab : MonoBehaviour
{
    // This is a template for creating room icon prefabs
    public Image iconImage;
    public Image borderImage;
    public TMPro.TextMeshProUGUI roomNumberText; // Optional
    
    void Awake()
    {
        if (iconImage == null)
            iconImage = GetComponent<UnityEngine.UI.Image>();
    }
}