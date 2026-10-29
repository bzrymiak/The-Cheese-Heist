#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Cheese Heist > Build Gus Fight Scene
// lays out the fight like joann's figma mockup. safe to re-run, it rebuilds the scene
public static class GusFightBuilder
{
    const string ScenePath = "Assets/Scenes/GusFight.unity";
    const string Art = "Assets/Sprites/Minigame/";
    static Font font;

    [MenuItem("Tools/Cheese Heist/Build Gus Fight Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        MakeLightTextures();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PixelifySans-VariableFont_wght.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(43, 43, 43, 255);
        camGo.transform.position = new Vector3(0, 0, -10);

        new GameObject("WindowManager").AddComponent<WindowManager>();

        var canvasGo = new GameObject("FightCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1440);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        // everything sits under one stage object so the whole screen can shake together
        var stage = new GameObject("Stage", typeof(RectTransform)).GetComponent<RectTransform>();
        stage.SetParent(canvasGo.transform, false);
        stage.anchorMin = Vector2.zero; stage.anchorMax = Vector2.one;
        stage.offsetMin = Vector2.zero; stage.offsetMax = Vector2.zero;
        var root = stage.transform;

        // stage: darker floor strip along the bottom like the mockup
        Img(root, "Floor", new Color32(28, 28, 28, 255), new Vector2(0, -470), new Vector2(1920, 500));

        // spotlight cone coming down from the top, and the pool of light on the floor
        var cone = Img(root, "Spotlight", new Color(1, 1, 1, 0.22f), new Vector2(0, -60), new Vector2(1220, 1160));
        cone.sprite = Load(Art + "spotlight_cone.png");
        var pool = Img(root, "LightPool", new Color(1, 1, 1, 0.85f), new Vector2(0, -520), new Vector2(1000, 200));
        pool.sprite = Load(Art + "light_pool.png");

        // health bars + FIGHT! box
        var php = Bar(root, "You", new Vector2(-560, 610), new Color32(84, 214, 84, 255), true);
        var ghp = Bar(root, "Gus", new Vector2(560, 610), new Color32(226, 64, 64, 255), false);

        var fightBox = Img(root, "FightBox", new Color32(235, 235, 235, 255), new Vector2(0, 610), new Vector2(330, 110));
        Border(fightBox.gameObject, 6);
        Txt(fightBox.transform, "Label", "FIGHT!", 70, Color.black, Vector2.zero, new Vector2(330, 110));

        // the track
        var track = new GameObject("Track", typeof(RectTransform)).GetComponent<RectTransform>();
        track.SetParent(root, false);
        track.sizeDelta = new Vector2(1920, 220);
        track.anchoredPosition = new Vector2(0, 300);
        Img(track, "Line", new Color(1, 1, 1, 0.9f), Vector2.zero, new Vector2(1920, 8));

        var ring = Img(track, "HitCircle", Color.white, new Vector2(0, 0), new Vector2(150, 150));
        ring.sprite = Load(Art + "hit_ring.png");
        // thicken the ring with a couple of outlines instead of the arrow in the middle,
        // plus a dark edge so it pops off the white line
        var thick = ring.gameObject.AddComponent<Outline>();
        thick.effectColor = Color.white; thick.effectDistance = new Vector2(3, 3);
        var thick2 = ring.gameObject.AddComponent<Outline>();
        thick2.effectColor = Color.white; thick2.effectDistance = new Vector2(-3, -3);
        var edge = ring.gameObject.AddComponent<Shadow>();
        edge.effectColor = new Color(0, 0, 0, 0.6f); edge.effectDistance = new Vector2(0, -5);
        var inner = ring; // the ring itself flashes on hits now

        // arrow outline in the middle, same size as the real arrows so a perfect is when they overlap
        var guide = Img(ring.transform, "Guide", new Color(1, 1, 1, 0.55f), Vector2.zero, new Vector2(110, 110));
        guide.preserveAspect = true;

        var judge = Txt(root, "Judgement", "", 56, Color.white, new Vector2(260, 420), new Vector2(600, 80));
        judge.alignment = TextAnchor.MiddleLeft;
        var center = Txt(root, "Center", "", 110, Color.white, new Vector2(0, 40), new Vector2(1400, 420));
        center.supportRichText = true;

        Txt(root, "Help", "When an arrow reaches the circle, press that arrow key", 32,
            new Color(1, 1, 1, 0.55f), new Vector2(0, -665), new Vector2(1500, 50));

        // shadows + fighters
        var s1 = Img(root, "RobberShadow", new Color(0, 0, 0, 0.35f), new Vector2(-150, -470), new Vector2(190, 50));
        s1.sprite = Load(Art + "light_pool.png");
        var s2 = Img(root, "GusShadow", new Color(0, 0, 0, 0.35f), new Vector2(150, -470), new Vector2(190, 50));
        s2.sprite = Load(Art + "light_pool.png");

        var robber = Img(root, "Robber", Color.white, new Vector2(-150, -330), new Vector2(196, 252));
        robber.sprite = FindSprite("robber"); robber.preserveAspect = true;
        robber.rectTransform.pivot = new Vector2(0.5f, 0f);
        robber.rectTransform.anchoredPosition = new Vector2(-150, -470);
        var gus = Img(root, "Gus", Color.white, new Vector2(150, -330), new Vector2(196, 256));
        gus.sprite = FindSprite("gus"); gus.preserveAspect = true;
        gus.rectTransform.pivot = new Vector2(0.5f, 0f);
        gus.rectTransform.anchoredPosition = new Vector2(150, -470);

        // little "!" over gus's head when he's about to swing
        var alert = Txt(gus.transform, "Alert", "", 90, new Color32(255, 90, 90, 255), new Vector2(0, 300), new Vector2(120, 120));

        // red flash over the whole screen when you get hit (outside the stage so it doesn't shake)
        var hurt = Img(canvasGo.transform, "HurtFlash", new Color(1, 0, 0, 0), Vector2.zero, Vector2.zero);
        hurt.rectTransform.anchorMin = Vector2.zero; hurt.rectTransform.anchorMax = Vector2.one;
        hurt.rectTransform.offsetMin = Vector2.zero; hurt.rectTransform.offsetMax = Vector2.zero;
        hurt.raycastTarget = false;

        track.SetAsLastSibling();
        judge.transform.SetAsLastSibling();
        center.transform.SetAsLastSibling();

        var fight = canvasGo.AddComponent<RhythmFight>();
        fight.stage = stage;
        fight.guide = guide;
        fight.gusAlert = alert;
        fight.hurtFlash = hurt;
        fight.track = track;
        fight.hitCircle = ring.rectTransform;
        fight.hitInner = inner;
        fight.judgeText = judge;
        fight.centerText = center;
        fight.playerHealth = php;
        fight.gusHealth = ghp;
        fight.robber = robber.rectTransform;
        fight.gus = gus.rectTransform;

        // order is left, down, up, right (right reuses left, the script flips it)
        fight.arrows = new[] { Load(Art + "arrow_left.png"), Load(Art + "arrow_down.png"), Load(Art + "arrow_up.png"), Load(Art + "arrow_left.png") };
        fight.arrowsHit = new[] { Load(Art + "arrow_left_hit.png"), Load(Art + "arrow_down_hit.png"), Load(Art + "arrow_up_hit.png"), Load(Art + "arrow_left_hit.png") };
        fight.arrowsOff = new[] { Load(Art + "arrow_left_off.png"), Load(Art + "arrow_down_off.png"), Load(Art + "arrow_up_off.png"), Load(Art + "arrow_left_off.png") };

        EditorSceneManager.SaveScene(scene, ScenePath);

        var list = EditorBuildSettings.scenes.ToList();
        if (!list.Any(s => s.path == ScenePath)) list.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();

        Debug.Log("gus fight rebuilt, press play");
    }

    // health bar like the mockup: dark frame, name on the left, coloured fill
    static RectTransform Bar(Transform root, string who, Vector2 pos, Color fill, bool leftSide)
    {
        var frame = Img(root, who + "Bar", new Color32(235, 235, 235, 255), pos, new Vector2(560, 64));
        Border(frame.gameObject, 5);

        var back = Img(frame.transform, "Back", new Color32(60, 60, 60, 255), new Vector2(50, 0), new Vector2(430, 36));
        var bar = Img(frame.transform, "Fill", fill, new Vector2(50 - 215, 0), new Vector2(430, 36));
        bar.rectTransform.pivot = new Vector2(0, 0.5f);

        var label = Txt(frame.transform, "Name", who.ToUpper(), 34, Color.black, new Vector2(-222, 0), new Vector2(100, 60));
        return bar.rectTransform;
    }

    static void Border(GameObject go, float size)
    {
        var o = go.AddComponent<Outline>();
        o.effectColor = Color.black;
        o.effectDistance = new Vector2(size, -size);
        var o2 = go.AddComponent<Outline>();
        o2.effectColor = Color.black;
        o2.effectDistance = new Vector2(-size, size);
    }

    static Sprite Load(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogWarning("missing sprite " + path);
        return s;
    }

    static Sprite FindSprite(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:Sprite", new[] { "Assets/Sprites/Characters" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path).ToLower() == name)
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        Debug.LogWarning("couldn't find " + name + ".png in Sprites/Characters");
        return null;
    }

    // soft spotlight cone + floor pool, generated so we don't need extra art for them
    static void MakeLightTextures()
    {
        Directory.CreateDirectory(Art);
        if (!File.Exists(Art + "spotlight_cone.png"))
        {
            int w = 128, h = 128;
            var tex = new Texture2D(w, h);
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1);             // 0 bottom, 1 top
                float half = Mathf.Lerp(0.5f, 0.12f, v);  // wide at the bottom, narrow up top
                for (int x = 0; x < w; x++)
                {
                    float u = Mathf.Abs(x / (float)(w - 1) - 0.5f);
                    float a = u < half ? Mathf.Lerp(0.55f, 1f, v) * Mathf.Clamp01((half - u) * 12f) : 0f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            }
            File.WriteAllBytes(Art + "spotlight_cone.png", tex.EncodeToPNG());
        }
        if (!File.Exists(Art + "light_pool.png"))
        {
            int w = 128, h = 64;
            var tex = new Texture2D(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - w / 2f) / (w / 2f), dy = (y - h / 2f) / (h / 2f);
                    float d = dx * dx + dy * dy;
                    tex.SetPixel(x, y, new Color(1, 1, 1, d < 1f ? 1f : 0f));
                }
            File.WriteAllBytes(Art + "light_pool.png", tex.EncodeToPNG());
        }
        AssetDatabase.Refresh();
    }

    static Image Img(Transform parent, string name, Color c, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        img.rectTransform.anchoredPosition = pos;
        img.rectTransform.sizeDelta = size;
        return img;
    }

    static Text Txt(Transform parent, string name, string text, int size, Color c, Vector2 pos, Vector2 box)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = c;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.rectTransform.anchoredPosition = pos;
        t.rectTransform.sizeDelta = box;
        return t;
    }
}
#endif
