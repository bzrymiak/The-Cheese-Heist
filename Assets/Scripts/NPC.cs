using UnityEngine.UI;
using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System;

public class NPC : MonoBehaviour
{
    public NPCDialogue dialogueData;
    private DialogueController dialogueUI;
    // public GameObject dialoguePanel;
    // public TMP_Text dialogueText, nameText;
    // public Image portraitImage;

    private int dialogueIndex;
    private bool isTyping, isDialogueActive;
    public bool autoStart;
    public float autoStartDelay;
    private bool hasSpoken;


    void Start()
    {
        dialogueUI = DialogueController.Instance;

        autoStart = dialogueData.autoStart;
        autoStartDelay = dialogueData.autoStartDelay;

        if (autoStart)
        {
            StartCoroutine(AutoStartRoutine());
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (PauseController.IsGamePaused && !isDialogueActive) return;

        if (kb.enterKey.wasPressedThisFrame)
        {
            if (isDialogueActive)
            {
                NextLine();
            }
            else
            {
                StartDialogue();
            }
        }
    }

    IEnumerator AutoStartRoutine()
    {
        yield return new WaitForSeconds(autoStartDelay);
        StartDialogue();
    }

    void StartDialogue()
    {
        if (isDialogueActive) return;
        if (hasSpoken) return;

        isDialogueActive = true;
        dialogueIndex = 0;
        
        dialogueUI.SetNPCInfo(dialogueData.npcName, dialogueData.npcPortrait);
        dialogueUI.ShowDialogueUI(true);
        PauseController.SetPause(true);

        DisplayCurrentLine();
        // StartCoroutine(TypeLine());
    }

    void NextLine()
    {
        if (isTyping)
        {
            // skip typing animation
            StopAllCoroutines();
            dialogueUI.SetDialogueText(dialogueData.dialogueLines[dialogueIndex]);
            isTyping = false;
            return;
        }

        // clear choices
        dialogueUI.ClearChoices();

        // check if its the end of the branch
        if (dialogueData.endDialogueLines.Length > dialogueIndex 
            && dialogueData.endDialogueLines[dialogueIndex])
        {
            EndDialogue();
            return;
        }

        // check for choices
        foreach (DialogueChoice dialogueChoice in dialogueData.choices)
        {
            if (dialogueChoice.dialogueIndex == dialogueIndex)
            {
                DisplayChoices(dialogueChoice);
                return;
            }
        }

        if (++dialogueIndex < dialogueData.dialogueLines.Length)
        {
            DisplayCurrentLine();
            // StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueUI.SetDialogueText("");

        foreach(char letter in dialogueData.dialogueLines[dialogueIndex])
        {
            // dialogueText.text += letter;
            dialogueUI.SetDialogueText(dialogueUI.dialogueText.text += letter); 
            yield return new WaitForSeconds(dialogueData.typingSpeed);
        }

        isTyping = false;
    }

    void DisplayChoices (DialogueChoice choice) {
        for (int i = 0; i < choice.choices.Length; i++)
        {
            int nextIndex = choice.nextDialogueIndexes[i];
            dialogueUI.CreateChoiceButton(choice.choices[i], () => ChooseOption(nextIndex));
        }
    }

    void ChooseOption(int nextIndex)
    {
        dialogueIndex = nextIndex;
        dialogueUI.ClearChoices();
        DisplayCurrentLine();
    }

    void DisplayCurrentLine()
    {
        StopAllCoroutines();
        StartCoroutine(TypeLine());
    }

    public void EndDialogue()
    {
        StopAllCoroutines();
        isDialogueActive = false;
        hasSpoken = true;
        dialogueUI.SetDialogueText("");
        dialogueUI.ShowDialogueUI(false);
        PauseController.SetPause(false);
    }
}
