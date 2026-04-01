using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TutorialUI : MonoBehaviour
{
    public List<GameObject> tutorialSteps;
    public Button nextButton;
    public Button backButton;
    private int currentStepIndex = 0;

    void Start()
    {
        backButton.interactable = false;
        nextButton.interactable = tutorialSteps.Count > 1;
        UpdateTutorialUI();
        nextButton.onClick.AddListener(OnNextButton);
        backButton.onClick.AddListener(OnBackButton);
    }

    void OnEnable()
    {
        currentStepIndex = 0;
        UpdateTutorialUI();
    }

    void OnNextButton()
    {
        if (currentStepIndex < tutorialSteps.Count - 1)
        {
            currentStepIndex++;
            UpdateTutorialUI();
        }
    }

    void OnBackButton()
    {
        if (currentStepIndex > 0)
        {
            currentStepIndex--;
            UpdateTutorialUI();
        }
    }

    void UpdateTutorialUI()
    {
        for (int i = 0; i < tutorialSteps.Count; i++)
        {
            tutorialSteps[i].SetActive(i == currentStepIndex);
        }
        backButton.interactable = currentStepIndex > 0;
        nextButton.interactable = currentStepIndex < tutorialSteps.Count - 1;
    }


}