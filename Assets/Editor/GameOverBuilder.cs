#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using G = GusFightBuilder;

// Tools > Cheese Heist > Build Game Over Scene
// the "you got KNOCKED OUT!" / "you got CAUGHT!" screens from joann's figma, rebuilt in unity.
// one scene, the script switches the art depending on what you lost.
public static class GameOverBuilder
{
    const string ScenePath = "Assets/Scenes/GameOver.unity";
    const string Art = "Assets/Sprites/Minigame/";

    static readonly Color Red = new Color32(232, 38, 38, 255);
    static readonly Color Laser = new Color32(120, 220, 140, 255);

    [MenuItem("Tools/Cheese Heist/Build Game Over Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        G.MakeLightTextures();
        MakeTextures();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        G.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PixelifySans-VariableFont_wght.ttf");
        if (G.font == null) G.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(62, 62, 62, 255);
        camGo.transform.position = new Vector3(0, 0, -10);

        new GameObject("WindowManager").AddComponent<WindowManager>();

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        var canvasGo = new GameObject("GameOverCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1440);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var root = canvasGo.transform;

        // soft grey background, a little lighter in the middle like the figma frame
        var bg = G.Img(root, "Background", Color.white, Vector2.zero, Vector2.zero);
        Stretch(bg.rectTransform);
        bg.sprite = G.Load(Art + "gameover_bg.png");

        // ---- art: knocked out (lost the gus fight) ----
        var ko = new GameObject("KnockedOutArt", typeof(RectTransform)).GetComponent<RectTransform>();
        ko.SetParent(root, false);
        var cone = G.Img(ko, "Spotlight", new Color(1, 1, 1, 0.38f), new Vector2(470, 40), new Vector2(760, 1320));
        cone.sprite = G.Load(Art + "spotlight_cone.png");
        var pool = G.Img(ko, "LightPool", new Color(0.88f, 0.88f, 0.88f, 1f), new Vector2(470, -440), new Vector2(760, 200));
        pool.sprite = G.Load(Art + "light_pool.png");
        var blood = G.Img(ko, "Blood", Color.white, new Vector2(440, -455), new Vector2(330, 90));
        blood.sprite = G.Load(Art + "blood_pool.png");
        var downed = G.Img(ko, "Robber", Color.white, new Vector2(470, -400), new Vector2(230, 296));
        downed.sprite = G.FindSprite("robber"); downed.preserveAspect = true;
        downed.rectTransform.localRotation = Quaternion.Euler(0, 0, 95f);

        // ---- art: caught by the lasers ----
        var lz = new GameObject("LaserArt", typeof(RectTransform)).GetComponent<RectTransform>();
        lz.SetParent(root, false);
        var shadow = G.Img(lz, "Shadow", new Color(0.75f, 0.75f, 0.75f, 0.6f), new Vector2(470, -330), new Vector2(560, 90));
        shadow.sprite = G.Load(Art + "light_pool.png");
        var standing = G.Img(lz, "Robber", Color.white, new Vector2(470, -100), new Vector2(330, 424));
        standing.sprite = G.FindSprite("robber"); standing.preserveAspect = true;
        // beams coming in from off screen, all pinned on him
        Beam(lz, new Vector2(150, -760), new Vector2(420, -10));
        Beam(lz, new Vector2(420, -760), new Vector2(450, -150));
        Beam(lz, new Vector2(620, -760), new Vector2(500, -150));
        Beam(lz, new Vector2(1000, -620), new Vector2(530, -60));
        Beam(lz, new Vector2(1000, 120), new Vector2(520, 30));
        // speech bubble
        var bubble = G.Img(lz, "Bubble", Color.white, new Vector2(500, 300), new Vector2(560, 90));
        G.Border(bubble.gameObject, 5);
        var tail = G.Img(bubble.transform, "Tail", Color.white, new Vector2(-180, -52), new Vector2(26, 26));
        G.Border(tail.gameObject, 3);
        tail.transform.SetAsFirstSibling();
        G.Txt(bubble.transform, "Line", "Don't shoot! I can explain...", 32, Color.black, Vector2.zero, new Vector2(540, 80));

        // ---- left column: heading + buttons ----
        var t1 = G.Txt(root, "TitleTop", "you got", 150, Red, new Vector2(-470, 330), new Vector2(900, 170));
        t1.alignment = TextAnchor.MiddleLeft;
        Outline(t1.gameObject);
        var t2 = G.Txt(root, "TitleBottom", "KNOCKED OUT!", 150, Red, new Vector2(-470, 160), new Vector2(900, 170));
        t2.alignment = TextAnchor.MiddleLeft;
        Outline(t2.gameObject);

        var retry = MakeButton(root, "Retry", "RETRY", new Color32(20, 20, 20, 255), new Vector2(-470, -60), out var retryOutline);
        var menu = MakeButton(root, "Menu", "RETURN TO MENU", new Color32(52, 52, 52, 255), new Vector2(-470, -210), out var menuOutline);

        var help = G.Txt(root, "Help", "arrow keys + enter, or click", 26, new Color(1, 1, 1, 0.45f), new Vector2(-470, -330), new Vector2(760, 40));

        // if joann's exported frames are there, use them as-is and lay invisible buttons over her drawn ones
        var koPng = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "gameover_knockedout.png");
        var caughtPng = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "gameover_caught.png");
        if (koPng != null && caughtPng != null)
        {
            UseFigmaFrames(root, ko, lz, koPng, caughtPng, t1, t2, help, retry, menu, ref retryOutline, ref menuOutline);
            bg.color = new Color32(62, 62, 62, 255);
            bg.sprite = null;
        }

        var go = canvasGo.AddComponent<GameOver>();
        go.knockedOutArt = ko.gameObject;
        go.laserArt = lz.gameObject;
        go.titleBottom = t2;
        go.retryButton = retry;
        go.menuButton = menu;
        go.retryOutline = retryOutline;
        go.menuOutline = menuOutline;
        lz.gameObject.SetActive(false);

        // hovering with the mouse moves the highlight too
        Hover(retry.gameObject, go, 0);
        Hover(menu.gameObject, go, 1);

        EditorSceneManager.SaveScene(scene, ScenePath);

        var list = EditorBuildSettings.scenes.ToList();
        if (!list.Any(s => s.path == ScenePath)) list.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();

        Debug.Log("game over screen built");
    }

    // figma frame is 1004x639, we fit it to the full width of the 1920 wide canvas and centre it
    static void UseFigmaFrames(Transform root, RectTransform ko, RectTransform lz, Sprite koPng, Sprite caughtPng,
        Text t1, Text t2, Text help, Button retry, Button menu, ref Image retryOutline, ref Image menuOutline)
    {
        const float scale = 1920f / 1004f;
        Vector2 Frame(float x, float y) => new Vector2(x * scale - 960f, 639f * scale / 2f - y * scale);

        // swap the rebuilt art for the full exported frames
        foreach (Transform c in ko) c.gameObject.SetActive(false);
        foreach (Transform c in lz) c.gameObject.SetActive(false);
        var a = G.Img(ko, "Frame", Color.white, Vector2.zero, new Vector2(1920, 639 * scale));
        a.sprite = koPng;
        var b = G.Img(lz, "Frame", Color.white, Vector2.zero, new Vector2(1920, 639 * scale));
        b.sprite = caughtPng;

        // heading + button labels are already in the picture
        t1.gameObject.SetActive(false);
        t2.gameObject.SetActive(false);
        help.rectTransform.anchoredPosition = new Vector2(0, -660);

        // move the clickable buttons on top of the drawn ones and make them see-through
        Vector2 size = new Vector2(367 * scale, 56 * scale);
        Place(retry, retryOutline, Frame(33 + 183.5f, 354.5f + 28f), size);
        Place(menu, menuOutline, Frame(33 + 183.5f, 434f + 28f), size);

        // selection shows as a soft white glow over the drawn button instead of a frame behind it
        retryOutline.color = new Color(1, 1, 1, 0.18f);
        menuOutline.color = new Color(1, 1, 1, 0.18f);
        retryOutline.transform.SetAsLastSibling();
        menuOutline.transform.SetAsLastSibling();
        retryOutline.raycastTarget = false;
        menuOutline.raycastTarget = false;
    }

    static void Place(Button btn, Image outline, Vector2 pos, Vector2 size)
    {
        var rt = (RectTransform)btn.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        btn.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        btn.transition = Selectable.Transition.None;
        foreach (Transform c in btn.transform) c.gameObject.SetActive(false);
        rt.SetAsLastSibling();
        outline.rectTransform.anchoredPosition = pos;
        outline.rectTransform.sizeDelta = size;
    }

    static Button MakeButton(Transform root, string name, string label, Color fill, Vector2 pos, out Image outline)
    {
        // white frame behind the button, only shown on the selected one
        outline = G.Img(root, name + "Highlight", Color.white, pos, new Vector2(772, 122));
        var img = G.Img(root, name + "Button", fill, pos, new Vector2(760, 110));
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        var cols = b.colors;
        cols.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
        cols.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        b.colors = cols;
        G.Txt(img.transform, "Label", label, 40, Color.white, Vector2.zero, new Vector2(740, 100));
        return b;
    }

    static void Hover(GameObject target, GameOver go, int index)
    {
        var trig = target.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        trig.triggers.Add(entry);
        UnityEventTools.AddIntPersistentListener(entry.callback, go.Select, index);
    }

    static void Beam(Transform parent, Vector2 from, Vector2 to)
    {
        Vector2 d = to - from;
        var b = G.Img(parent, "Beam", Laser, (from + to) / 2f, new Vector2(d.magnitude, 6));
        b.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        var dot = G.Img(parent, "Dot", Laser, to, new Vector2(14, 14));
    }

    static void Outline(GameObject go)
    {
        var o = go.AddComponent<Outline>(); o.effectColor = Color.black; o.effectDistance = new Vector2(6, -6);
        var o2 = go.AddComponent<Outline>(); o2.effectColor = Color.black; o2.effectDistance = new Vector2(-6, 6);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void MakeTextures()
    {
        Directory.CreateDirectory(Art);
        if (!File.Exists(Art + "gameover_bg.png"))
        {
            // radial-ish grey, lighter towards the middle
            int w = 64, h = 48;
            var tex = new Texture2D(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - w * 0.5f) / w, dy = (y - h * 0.55f) / h;
                    float k = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 1.6f);
                    float v = Mathf.Lerp(52f, 98f, k) / 255f;
                    tex.SetPixel(x, y, new Color(v, v, v));
                }
            File.WriteAllBytes(Art + "gameover_bg.png", tex.EncodeToPNG());
        }
        if (!File.Exists(Art + "blood_pool.png"))
        {
            // lumpy pixel puddle
            int w = 36, h = 10;
            var tex = new Texture2D(w, h);
            var rng = new System.Random(3);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f - w / 2f) / (w / 2f), dy = (y + 0.5f - h / 2f) / (h / 2f);
                    float wobble = 0.15f * Mathf.Sin(x * 1.3f) + (float)rng.NextDouble() * 0.08f;
                    bool inside = dx * dx + dy * dy < 0.85f + wobble;
                    Color c = new Color(0, 0, 0, 0);
                    if (inside) c = y >= h - 3 && x % 5 != 0 ? new Color32(196, 30, 30, 255) : new Color32(160, 16, 20, 255);
                    tex.SetPixel(x, y, c);
                }
            File.WriteAllBytes(Art + "blood_pool.png", tex.EncodeToPNG());
        }
        AssetDatabase.Refresh();
    }
}
#endif
