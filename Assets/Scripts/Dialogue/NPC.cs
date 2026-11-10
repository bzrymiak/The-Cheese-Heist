using UnityEngine.UI;
using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class NPC : MonoBehaviour
{
    public NPCDialogue dialogueData;
    private DialogueController dialogueUI;

    private int dialogueIndex;
    private bool isTyping, isDialogueActive;
    public bool autoStart;
    public float autoStartDelay;

    public bool manualOnly; // only runs when StartDialogue() is called
    public bool repeatable; // can play every time its triggered
    private bool hasSpoken;

    // conversations already finished this run. static so it survives leaving and coming
    // back into a room (the npc object itself gets rebuilt every time the scene loads)
    static readonly HashSet<string> finished = new HashSet<string>();
    // going back to the main menu counts as a fresh game, so forget everything
    [RuntimeInitializeOnLoadMethod]
    static void HookMenuReset()
    {
        finished.Clear();
        SceneManager.sceneLoaded += (scene, mode) => { if (scene.name == "Menu") finished.Clear(); };
    }

    public static void ForgetConversations() => finished.Clear();

    string Key => SceneManager.GetActiveScene().name + "/" + (dialogueData != null ? dialogueData.name : name);
    public ChoiceAction[] actions; // function calls

    // lets story code know when a conversation is over (and whether a choice loaded another scene)
    public event Action<NPC> Finished;
    public bool LeftScene { get; private set; }


    void Start()
    {
        dialogueUI = DialogueController.Instance;

        autoStart = dialogueData.autoStart;
        autoStartDelay = dialogueData.autoStartDelay;

        if (finished.Contains(Key)) hasSpoken = true;

        if (autoStart && !hasSpoken && !manualOnly)
        {
            StartCoroutine(AutoStartRoutine());
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (PauseController.IsGamePaused && !isDialogueActive) return;
        if (dialogueData.skip) EndDialogue(); // debug skip

        if (kb.enterKey.wasPressedThisFrame)
        {
            if (isDialogueActive)
            {
                NextLine();
            }
            else if (!manualOnly)
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

    public void StartDialogue()
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
                LeftScene = true;
                EndDialogue();   
                SceneManager.LoadScene(action.parameter);
                break;

            case ChoiceActionType.SetFlag:
                // "AlarmOn" sets a flag, "trust:-1" changes tony's trust. comma separate to do a few
                Story.Apply(action.parameter);
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
        if (!repeatable)
        {
            hasSpoken = true;
            finished.Add(Key);
        }

        // the dialogue box carries over between rooms, so any leftover choice buttons
        // (like gus's fight / misunderstanding) would show up in the next conversation
        dialogueUI.ClearChoices();
        dialogueUI.SetDialogueText("");
        dialogueUI.ShowDialogueUI(false);
        PauseController.SetPause(false);

        Finished?.Invoke(this);
    }
}
