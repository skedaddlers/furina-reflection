using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UIButtonPressEffectInstaller
{
    private const UIButtonPressEffect.PressEffectMode DefaultPressEffectMode = UIButtonPressEffect.PressEffectMode.ButtonPress;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyToAllButtonsInLoadedScenes();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToAllButtonsInLoadedScenes();
    }

    private static void ApplyToAllButtonsInLoadedScenes()
    {
        Button[] buttons = Object.FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            if (!button.gameObject.scene.IsValid())
            {
                continue;
            }

            if (button.GetComponent<UIButtonPressEffect>() != null)
            {
                continue;
            }

            UIButtonPressEffect effect = button.gameObject.AddComponent<UIButtonPressEffect>();
            effect.SetEffectMode(DefaultPressEffectMode);
        }
    }
}
