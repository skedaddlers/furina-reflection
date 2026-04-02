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

    private Coroutine activeSequenceCoroutine;
    private Coroutine activeTypingCoroutine;

    public void StartDialogueSequence(List<Dialogue> dialogues)
    {
        // 1. Stop the current sequence if one is already running
        if (activeSequenceCoroutine != null)
        {
            StopCoroutine(activeSequenceCoroutine);
        }

        // 2. Stop any active typing effect to prevent text flickering
        if (activeTypingCoroutine != null)
        {
            StopCoroutine(activeTypingCoroutine);
        }

        // 3. Start the new sequence and store the reference
        activeSequenceCoroutine = StartCoroutine(PlayDialogueSequence(dialogues));
    }

    public void StopDialogueSequence()
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

        HideDialogue();
    }

    private IEnumerator PlayDialogueSequence(List<Dialogue> dialogues)
    {
        foreach (Dialogue dialogue in dialogues)
        {
            ShowDialogue(dialogue.dialogueText, dialogue.speakerName);
            if (dialogue.voiceLine != null)
            {
                AudioManager.Instance.StopVoiceLine(); // Stop any currently playing voice line before starting a new one
                AudioManager.Instance.PlayDialogueLine(dialogue.voiceLine);
            }
            yield return new WaitForSeconds(dialogue.GetDialogueDuration() + dialogueBufferTime); // wait for dialogue duration + a small buffer
        }
        HideDialogue();
        activeSequenceCoroutine = null;
    }
    

    public float GetTotalDialogueSequenceDuration(List<Dialogue> dialogues)
    {
        float totalDuration = 0f;
        foreach (Dialogue dialogue in dialogues)
        {
            totalDuration += dialogue.GetDialogueDuration() + dialogueBufferTime; // dialogue duration + buffer
        }
        return totalDuration;
    }

    public void ShowDialogue(string dialogue, string speakerName)
    {
        speakerNameText.text = speakerName;
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
        dialoguePanel.SetActive(false);
        // Clean up coroutines when hiding
        if (activeTypingCoroutine != null) StopCoroutine(activeTypingCoroutine);
        activeSequenceCoroutine = null;
    }

    public IEnumerator TypewriteEffect(string dialogue)
    {
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