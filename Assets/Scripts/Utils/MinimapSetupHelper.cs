using UnityEngine;

public class MinimapSetupHelper : MonoBehaviour
{
    [ContextMenu("Setup Minimap UI")]
    public void SetupMinimapInScene()
    {
        // Find or create canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Create Minimap UI
        GameObject minimapObj = new GameObject("MinimapUI");
        minimapObj.transform.SetParent(canvas.transform);
        MinimapUI minimapUI = minimapObj.AddComponent<MinimapUI>();

        // Debug.Log("Minimap UI setup complete! Configure settings in the Inspector.");
    }
}