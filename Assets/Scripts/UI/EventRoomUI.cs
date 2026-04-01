using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

    private void Awake()
    {
        if (eventPanel != null)
            eventPanel.SetActive(false);
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
                widget.button.onClick.RemoveAllListeners();
                widget.button.onClick.AddListener(() =>
                {
                    currentManager.OnChoiceSelected(capturedIndex);
                });
            }
            else
            {
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
}
