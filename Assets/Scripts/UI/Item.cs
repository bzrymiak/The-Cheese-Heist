using UnityEngine;

public class Item : MonoBehaviour
{
    public int ID;
    public string title;
    [TextArea] public string description;
    public Sprite detailedView; // optional 
}
