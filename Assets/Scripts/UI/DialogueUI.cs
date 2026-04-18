using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class DialogueUI : MonoBehaviour
{

    [Header("Dialogue UI Elements")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI speakerNameText;
    public float typingSpeed = 0.05f;
    public float dialogueBufferTime = 1f;

    [Header("Skip Dialogue")]
    public KeyCode skipKey = KeyCode.B;
    public float skipHoldDuration = 1.25f;
    public string skipPromptMessage = "Hold B to skip";
    public GameObject skipPromptRoot;
    public TextMeshProUGUI skipPromptText;
    public Slider skipProgressBar;

    private Coroutine activeSequenceCoroutine;
    private Coroutine activeTypingCoroutine;
    private float skipHoldTimer;
    private System.Action activeSequenceCompletedCallback;

    private void Update()
    {
        if (activeSequenceCoroutine == null)
            return;

        if (Input.GetKey(skipKey))
        {
            skipProgressBar.gameObject.SetActive(true);
            float requiredHoldDuration = Mathf.Max(skipHoldDuration, 0.01f);
            skipHoldTimer += Time.deltaTime;
            UpdateSkipProgress(skipHoldTimer / requiredHoldDuration);

            if (skipHoldTimer >= requiredHoldDuration)
                CompleteDialogueSequence();
        }
        else if (skipHoldTimer > 0f)
        {
            ResetSkipHoldState();
        }
    }

    public void StartDialogueSequence(List<Dialogue> dialogues, System.Action onSequenceCompleted = null)
    {
        StopDialogueSequence();

        activeSequenceCompletedCallback = onSequenceCompleted;
        ResetSkipHoldState();
        ShowSkipUI();

        if (dialogues == null || dialogues.Count == 0)
        {
            FinishDialogueSequence(true);
            return;
        }

        activeSequenceCoroutine = StartCoroutine(PlayDialogueSequence(dialogues));
    }

    public void StopDialogueSequence()
    {
        CancelRunningDialogueCoroutines();
        FinishDialogueSequence(false);
    }

    private void CompleteDialogueSequence()
    {
        CancelRunningDialogueCoroutines();
        FinishDialogueSequence(true);
    }

    private void CancelRunningDialogueCoroutines()
    {
        if (activeSequenceCoroutine != null)
        {
            StopCoroutine(activeSequenceCoroutine);
            activeSequenceCoroutine = null;
        }

        if (activeTypingCoroutine != null)
        {
            StopCoroutine(activeTypingCoroutine);
            activeTypingCoroutine = null;
        }
    }

    private IEnumerator PlayDialogueSequence(List<Dialogue> dialogues)
    {
        foreach (Dialogue dialogue in dialogues)
        {
            ShowDialogue(dialogue.dialogueText, dialogue.speakerName);
            if (dialogue.voiceLine != null)
            {
                AudioManager.Instance?.StopVoiceLine(); // Stop any currently playing voice line before starting a new one
                AudioManager.Instance?.PlayDialogueLine(dialogue.voiceLine);
            }
            yield return new WaitForSeconds(dialogue.GetDialogueDuration() + dialogueBufferTime); // wait for dialogue duration + a small buffer
        }

        activeSequenceCoroutine = null;
        FinishDialogueSequence(true);
    }
    

    public float GetTotalDialogueSequenceDuration(List<Dialogue> dialogues)
    {
        if (dialogues == null)
            return 0f;

        float totalDuration = 0f;
        foreach (Dialogue dialogue in dialogues)
        {
            totalDuration += dialogue.GetDialogueDuration() + dialogueBufferTime; // dialogue duration + buffer
        }
        return totalDuration;
    }

    public void ShowDialogue(string dialogue, string speakerName)
    {
        if (speakerNameText != null)
            speakerNameText.text = speakerName;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        // Stop current typing before starting new typing for this specific line
        if (activeTypingCoroutine != null)
        {
            StopCoroutine(activeTypingCoroutine);
        }
        
        activeTypingCoroutine = StartCoroutine(TypewriteEffect(dialogue));
    }

    public void HideDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    public IEnumerator TypewriteEffect(string dialogue)
    {
        if (dialogueText == null)
        {
            activeTypingCoroutine = null;
            yield break;
        }

        dialogueText.text = dialogue; // Set the full text immediately
        dialogueText.maxVisibleCharacters = 0;
        
        int totalCharacters = dialogue.Length;
        for (int i = 0; i <= totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }
        
        activeTypingCoroutine = null;
    }

    private void FinishDialogueSequence(bool invokeCompletionCallback)
    {
        AudioManager.Instance?.StopVoiceLine();

        HideDialogue();
        HideSkipUI();
        ResetSkipHoldState();

        System.Action callback = activeSequenceCompletedCallback;
        activeSequenceCompletedCallback = null;

        if (invokeCompletionCallback)
            callback?.Invoke();
    }

    private void ShowSkipUI()
    {
        if (skipPromptRoot != null)
            skipPromptRoot.SetActive(true);

        if (skipPromptText != null)
        {
            skipPromptText.text = skipPromptMessage;
            skipPromptText.gameObject.SetActive(true);
        }
    }

    private void HideSkipUI()
    {
        if (skipPromptRoot != null)
            skipPromptRoot.SetActive(false);

        if (skipPromptRoot == null && skipPromptText != null)
            skipPromptText.gameObject.SetActive(false);

        if (skipPromptRoot == null && skipProgressBar != null)
            skipProgressBar.gameObject.SetActive(false);
    }

    private void ResetSkipHoldState()
    {
        skipProgressBar.gameObject.SetActive(false);
        skipHoldTimer = 0f;
        UpdateSkipProgress(0f);
    }

    private void UpdateSkipProgress(float normalizedProgress)
    {
        if (skipProgressBar != null)
            skipProgressBar.normalizedValue = Mathf.Clamp01(normalizedProgress);
    }
}

[System.Serializable]
public class Dialogue
{
    public string speakerName;
    [TextArea(3, 10)]
    public string dialogueText;
    public AudioClip voiceLine;

    public float GetDialogueDuration()
    {
        float textDuration = dialogueText.Length * 0.05f; // Assuming 0.05 seconds per character
        float voiceLineDuration = voiceLine != null ? voiceLine.length : 0f;
        return Mathf.Max(textDuration, voiceLineDuration);
    }
}
