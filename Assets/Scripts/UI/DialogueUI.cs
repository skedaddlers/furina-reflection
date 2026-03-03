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

    public void StartDialogueSequence(List<Dialogue> dialogues)
    {
        StartCoroutine(PlayDialogueSequence(dialogues));
    }

    private IEnumerator PlayDialogueSequence(List<Dialogue> dialogues)
    {
        foreach (Dialogue dialogue in dialogues)
        {
            ShowDialogue(dialogue.dialogueText, dialogue.speakerName);
            if (dialogue.voiceLine != null)
            {
                AudioManager.Instance.PlayVoiceLine(dialogue.voiceLine);
            }
            yield return new WaitForSeconds(dialogue.GetDialogueDuration() + dialogueBufferTime); // wait for dialogue duration + a small buffer
        }
        HideDialogue();
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
        dialogueText.text = dialogue;
        speakerNameText.text = speakerName;
        dialoguePanel.SetActive(true);
        StartCoroutine(TypewriteEffect(dialogue));
    }

    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
    }

    public IEnumerator TypewriteEffect(string dialogue)
    {
        // set max visible characters to 0
        dialogueText.maxVisibleCharacters = 0;
        int totalCharacters = dialogue.Length;
        int currentCharacter = 0;
        while (currentCharacter <= totalCharacters)
        {
            dialogueText.maxVisibleCharacters = currentCharacter;
            currentCharacter++;
            yield return new WaitForSeconds(typingSpeed);
        }
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