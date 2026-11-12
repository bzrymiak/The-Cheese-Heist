using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// once django hits the alarm, every room pulses red until you're out.
// sits under the dialogue box so conversations still read fine. made in code, nothing to set up in scenes.
public class AlarmOverlay : MonoBehaviour
{
    static readonly string[] skipScenes = { "Menu", "Intro", "TransitionScreen", "GusFight", "ReggieFight", "LaserDodge", "GameOver", "Ending", "VentMaze" };

    Canvas canvas;
    Image wash, siren, edgeTop, edgeBottom;
    Text label;

    void Start()
    {
        var go = new GameObject("AlarmCanvas");
        go.transform.SetParent(transform, false);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -10; // below the dialogue / menu canvases
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1440);
        scaler.matchWidthOrHeight = 0.5f;

        wash = Make(go.transform, "Wash", Vector2.zero, Vector2.zero, true);
        edgeTop = Make(go.transform, "EdgeTop", new Vector2(0, 690), new Vector2(1920, 60), false);
        edgeBottom = Make(go.transform, "EdgeBottom", new Vector2(0, -690), new Vector2(1920, 60), false);
        siren = Make(go.transform, "Siren", new Vector2(-820, 600), new Vector2(240, 36), false);

        var t = new GameObject("Label", typeof(RectTransform)).AddComponent<Text>();
        t.transform.SetParent(go.transform, false);
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = "ALARM";
        t.fontSize = 44;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.rectTransform.anchoredPosition = new Vector2(-820, 600);
        t.rectTransform.sizeDelta = new Vector2(300, 60);
        t.raycastTarget = false;
        label = t;

        canvas.enabled = false;
    }

    Image Make(Transform parent, string name, Vector2 pos, Vector2 size, bool stretch)
    {
        var img = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(parent, false);
        img.raycastTarget = false;
        var rt = img.rectTransform;
        if (stretch) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
        else { rt.anchoredPosition = pos; rt.sizeDelta = size; }
        return img;
    }

    void Update()
    {
        if (canvas == null) return;
        bool on = GameFlags.Has(Story.AlarmOn) && System.Array.IndexOf(skipScenes, SceneManager.GetActiveScene().name) < 0;
        canvas.enabled = on;
        if (!on) return;

        // slow red breathing over the whole screen, harder flashes on the edges
        float beat = Mathf.PingPong(Time.unscaledTime * 1.4f, 1f);
        wash.color = new Color(1f, 0f, 0f, Mathf.Lerp(0.04f, 0.2f, beat * beat));
        var edge = new Color(1f, 0.1f, 0.1f, Mathf.Lerp(0.15f, 0.6f, beat));
        // just the red breathing now, no bars, siren or label
        edgeTop.enabled = edgeBottom.enabled = siren.enabled = label.enabled = false;

        siren.rectTransform.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * -540f);
        siren.color = new Color(1f, 0.15f, 0.15f, 0.8f);
        label.color = new Color(1f, 1f, 1f, beat > 0.5f ? 1f : 0.35f);
    }
}
