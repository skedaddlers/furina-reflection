using UnityEngine;
using DDAMAPEKitFramework;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Example UI display for DDA status
/// </summary>
public class DDAStatusUI : MonoBehaviour
{
    private TextMeshProUGUI statusText;
    private PlayerModel playerModel;

    void Start()
    {
        statusText = GetComponent<TextMeshProUGUI>();
        playerModel = DDAMAPEKit.Instance.GetPlayerModel();
    }

    void Update()
    {
        if (playerModel != null && statusText != null)
        {
            var profile = playerModel.GetCurrentProfile();
            statusText.text = $"Player Type: {profile?.name ?? "Analyzing..."}";
        }
    }
}