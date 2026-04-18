using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
public class EventRoomUI : MonoBehaviour
{
    [System.Serializable]
    public class EventChoiceWidget
    {
        public Button button;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
    }

    public GameObject eventPanel;
    public EventChoiceWidget[] choiceWidgets; // isi 3 di inspector

    private EventRoomManager currentManager;
    private readonly Dictionary<Button, UnityAction> dynamicButtonActions = new Dictionary<Button, UnityAction>();

    private void Awake()
    {
        if (eventPanel != null)
            eventPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        foreach (var kvp in dynamicButtonActions)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                kvp.Key.onClick.RemoveListener(kvp.Value);
            }
        }

        dynamicButtonActions.Clear();
    }

    public void ShowChoices(List<GameEventOption> choices, EventRoomManager manager)
    {
        if (UIManager.Instance != null && !UIManager.Instance.TryOpenMenu(this))
        {
            return;
        }

        GameManager.Instance.player.GetComponent<PlayerController>().ResetAllStates();
        currentManager = manager;
        if (eventPanel != null)
            eventPanel.OpenPanel();

        for (int i = 0; i < choiceWidgets.Length; i++)
        {
            var widget = choiceWidgets[i];

            if (i < choices.Count)
            {
                var evt = choices[i];
                widget.button.gameObject.SetActive(true);
                widget.titleText.text = evt.displayName;
                widget.descriptionText.text = evt.description;

                int capturedIndex = i;
                SetDynamicButtonAction(widget.button, () =>
                {
                    currentManager.OnChoiceSelected(capturedIndex);
                });
            }
            else
            {
                ClearDynamicButtonAction(widget.button);
                widget.button.gameObject.SetActive(false);
            }
        }
    }

    public void Hide()
    {
        if (eventPanel != null)
            eventPanel.SetActive(false);
        currentManager = null;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseMenu(this);
        }
    }

    private void SetDynamicButtonAction(Button button, UnityAction action)
    {
        if (button == null)
            return;

        ClearDynamicButtonAction(button);

        if (action == null)
            return;

        dynamicButtonActions[button] = action;
        button.onClick.AddListener(action);
    }

    private void ClearDynamicButtonAction(Button button)
    {
        if (button == null)
            return;

        if (dynamicButtonActions.TryGetValue(button, out UnityAction existingAction) && existingAction != null)
        {
            button.onClick.RemoveListener(existingAction);
        }

        dynamicButtonActions.Remove(button);
    }
}
