using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// "how to play" card for the minigames, same look as the laser game's intro card.
// the game checks Done and starts once you press enter.
public class HowToPlay : MonoBehaviour
{
    public bool Done { get; private set; }

    public static HowToPlay Show(Transform canvas, string title, string[] lines)
    {
        // borrow whatever pixel font the scene's text already uses
        var any = canvas.GetComponentInChildren<Text>(true);
        Font font = any != null ? any.font : null;
        if (font == null && StoryContent.Get() != null) font = StoryContent.Get().pixelFont;
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var card = new GameObject("HowToPlay", typeof(RectTransform));
        card.transform.SetParent(canvas, false);
        var dim = card.AddComponent<Image>();
        dim.color = new Color(0, 0, 0, 0.82f);
        var rt = dim.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(card.transform, false);
        var pimg = panel.AddComponent<Image>();
        pimg.color = new Color32(235, 235, 235, 255);
        float h = 330f + lines.Length * 120f;
        pimg.rectTransform.sizeDelta = new Vector2(1100, h);
        foreach (var d in new[] { new Vector2(8, -8), new Vector2(-8, 8) })
        {
            var o = panel.AddComponent<Outline>();
            o.effectColor = Color.black;
            o.effectDistance = d;
        }

        float top = h / 2f;
        Label(panel.transform, font, title, 64, Color.black, top - 90);
        for (int i = 0; i < lines.Length; i++)
            Label(panel.transform, font, lines[i], 38, new Color(0.1f, 0.1f, 0.1f), top - 210 - i * 120);
        Label(panel.transform, font, "press enter to start", 36, new Color(0, 0, 0, 0.6f), -top + 70);

        card.transform.SetAsLastSibling();
        return card.AddComponent<HowToPlay>();
    }

    static void Label(Transform parent, Font font, string text, int size, Color c, float y)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = c;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.rectTransform.sizeDelta = new Vector2(980, 110);
        t.rectTransform.anchoredPosition = new Vector2(0, y);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || Done) return;
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
        {
            Done = true;
            gameObject.SetActive(false);
        }
    }
}
