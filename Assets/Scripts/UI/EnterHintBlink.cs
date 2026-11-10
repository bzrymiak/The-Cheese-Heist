using TMPro;
using UnityEngine;

// slow blink for the dialogue box's ENTER hint. hidden while choice buttons are up (you click those instead)
public class EnterHintBlink : MonoBehaviour
{
    TMP_Text text;
    void Awake() { text = GetComponent<TMP_Text>(); }

    void Update()
    {
        var dc = DialogueController.Instance;
        bool choosing = dc != null && dc.choiceContainer != null && dc.choiceContainer.childCount > 0;
        float a = choosing ? 0f : 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f));
        text.color = new Color(text.color.r, text.color.g, text.color.b, a);
    }
}
