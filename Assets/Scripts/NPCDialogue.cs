using Unity.VisualScripting.FullSerializer;
using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCDialogue", menuName = "NPC Dialogue")]
public class NPCDialogue : ScriptableObject
{
   public string npcName;
   public Sprite npcPortrait;
   public string[] dialogueLines;
//    public bool[] autoProgressLines;
//    public float autoProgressDelay = 1.5f;
    public bool autoStart = false;
    public float autoStartDelay = 3f;
   public float typingSpeed = 0.05f;
   
   
}
