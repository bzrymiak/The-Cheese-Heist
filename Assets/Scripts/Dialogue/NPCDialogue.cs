using Unity.VisualScripting.FullSerializer;
using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCDialogue", menuName = "NPC Dialogue")]
public class NPCDialogue : ScriptableObject
{
   public string npcName;
   public Sprite npcPortrait;
   public string[] dialogueLines;
   public bool[] endDialogueLines;
    public bool autoStart = false;
    public float autoStartDelay = 3f;
   public float typingSpeed = 0.05f;
   
   public DialogueChoice[] choices;
}

[System.Serializable]

public class DialogueChoice
{
   public int dialogueIndex;  // where choices appear
   public string[] choices;   // player choices
   public int[] nextDialogueIndexes;   // line after player decision
   public ChoiceAction[] actions;   // function calls
}

public enum ChoiceActionType { None, LoadScene, SetFlag }
[System.Serializable]

public class ChoiceAction
{
   public ChoiceActionType type;
   public string parameter;   // scene name, flag name, etc.
}
