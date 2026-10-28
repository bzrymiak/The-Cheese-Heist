using UnityEngine.UI;
using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System;
using UnityEngine.SceneManagement;

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
    public ChoiceAction[] actions; // function calls


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
            ChoiceAction action = (choice.actions != null && i < choice.actions.Length) 
                ? choice.actions[i]
                : null;

            dialogueUI.CreateChoiceButton(choice.choices[i], () => ChooseOption(nextIndex, action));
        }
    }

    void ChooseOption(int nextIndex, ChoiceAction action)
    {
        dialogueIndex = nextIndex;

         if (action != null && action.type != ChoiceActionType.None)
        {
            RunAction(action);

            // end dialogue
            if (action.type == ChoiceActionType.LoadScene) return;
        }
        
        dialogueUI.ClearChoices();
        DisplayCurrentLine();
    }

    void RunAction(ChoiceAction action)
    {
        switch (action.type)
        {
            case ChoiceActionType.LoadScene:
                EndDialogue();   
                SceneManager.LoadScene(action.parameter);
                break;

            case ChoiceActionType.SetFlag:
                // e.g. GameState.SetFlag(action.parameter, true);
                // where we can make note of interactions that shift narrative
                break;
        }
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
