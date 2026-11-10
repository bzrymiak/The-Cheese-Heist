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

// Tools > Cheese Heist > Build Phase 1 (Story, Reggie, Endings)
// one button for everything the new story needs:
//   Resources/StoryContent.asset  portraits + sprites the story code uses at runtime
//   ReggieFight.unity             the final boss fight
//   Ending.unity                  time cards, newspaper, end screens
public static class Phase1Builder
{
    const string Art = "Assets/Sprites/Minigame/";
    const string EndingPath = "Assets/Scenes/Ending.unity";

    [MenuItem("Tools/Cheese Heist/Build Phase 1 (Story, Reggie, Endings)")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildContent();
        G.BuildReggie();
        BuildEnding();
        Debug.Log("phase 1 built: story content, reggie fight, endings");
    }

    [MenuItem("Tools/Cheese Heist/Build Story Content (art + audio)")]
    internal static void BuildContent()
    {
        Directory.CreateDirectory("Assets/Resources");
        const string path = "Assets/Resources/StoryContent.asset";
        var c = AssetDatabase.LoadAssetAtPath<StoryContent>(path);
        if (c == null)
        {
            c = ScriptableObject.CreateInstance<StoryContent>();
            AssetDatabase.CreateAsset(c, path);
        }
        c.playerPortrait = Quiet("robber");
        c.robberSprite = Quiet("robber");
        c.tonyPortrait = Quiet("tonyhead");
        c.tonySprite = Quiet("tony");
        c.gusPortrait = Quiet("gushead");
        c.djangoPortrait = Quiet("image 340");
        c.reggieSprite = Quiet("reggie");
        c.reggiePortrait = Quiet("reggiehead");
        c.menuMusic = Clip("menu_music");
        c.laserMusic = Clip("lasergame_backgroud_music");
        if (c.laserMusic == null) c.laserMusic = Clip("lasergame_background_music");
        c.alarm = Clip("alarm");
        c.siren = Clip("police_siren_distan");
        if (c.siren == null) c.siren = Clip("police_siren_distant");
        c.punch = Clip("punch");
        c.pickup = Clip("picking_up_item");
        c.click = Clip("clicks");
        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        if (c.reggieSprite == null) Debug.Log("no reggie.png yet, reggie will show as a tinted gus until it's added to Sprites/Characters/Reggie");
    }

    static AudioClip Clip(string name)
    {
        foreach (var ext in new[] { ".mp3", ".wav", ".ogg" })
        {
            var a = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + name + ext);
            if (a != null) return a;
        }
        return null;
    }

    // like FindSprite but doesn't complain, reggie's art might not be in yet
    static Sprite Quiet(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Sprites/Characters" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p).ToLower() == name) return AssetDatabase.LoadAssetAtPath<Sprite>(p);
        }
        return null;
    }

    // ---------------- ending scene ----------------

    static void BuildEnding()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        G.MakeLightTextures();
        G.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PixelifySans-VariableFont_wght.ttf");
        if (G.font == null) G.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(24, 24, 24, 255);
        camGo.transform.position = new Vector3(0, 0, -10);
        new GameObject("WindowManager").AddComponent<WindowManager>();

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        var canvasGo = new GameObject("EndingCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1440);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var root = canvasGo.transform;
        var robberSprite = Quiet("robber");
        var tonySprite = Quiet("tony");

        // time cards: plain dark screen like the wireframes
        var timePanel = Panel(root, "TimePanel", new Color32(24, 24, 24, 255));
        var timeText = G.Txt(timePanel.transform, "Time", "3:00 AM", 120, Color.white, new Vector2(0, 200), new Vector2(1400, 160));
        var timeLine = G.Txt(timePanel.transform, "Line", "", 44, new Color(1, 1, 1, 0.85f), new Vector2(0, 40), new Vector2(1300, 160));
        Wrap(timeLine);
        G.Img(timePanel.transform, "Ground", new Color(1, 1, 1, 0.25f), new Vector2(0, -172), new Vector2(760, 4));
        var runner = G.Img(timePanel.transform, "Runner", Color.white, new Vector2(-300, -170), new Vector2(100, 128));
        runner.sprite = robberSprite; runner.preserveAspect = true;
        runner.rectTransform.pivot = new Vector2(0.5f, 0f);
        var car = G.Img(timePanel.transform, "Car", Color.white, new Vector2(300, -172), new Vector2(260, 110));
        car.preserveAspect = true; car.rectTransform.pivot = new Vector2(0.5f, 0f);
        var prop = G.Img(timePanel.transform, "Prop", Color.white, new Vector2(0, -300), new Vector2(520, 360));
        prop.preserveAspect = true;

        // newspaper
        var paperPanel = Panel(root, "PaperPanel", new Color(0, 0, 0, 0));
        var paper = G.Img(paperPanel.transform, "Paper", new Color32(232, 230, 224, 255), new Vector2(0, -20), new Vector2(1100, 760));
        G.Border(paper.gameObject, 6);
        var p = paper.transform;
        G.Txt(p, "Masthead", "THE CHEESY TIMES", 36, new Color32(40, 40, 40, 255), new Vector2(0, 330), new Vector2(1000, 50));
        G.Img(p, "Rule", new Color32(40, 40, 40, 255), new Vector2(0, 300), new Vector2(1000, 4));
        var photoBack = G.Img(p, "PhotoBack", new Color32(150, 150, 150, 255), new Vector2(-330, 150), new Vector2(280, 280));
        G.Border(photoBack.gameObject, 3);
        var photo = G.Img(photoBack.transform, "Photo", Color.white, Vector2.zero, new Vector2(220, 250));
        photo.preserveAspect = true; photo.sprite = robberSprite;
        var headline = G.Txt(p, "Headline", "", 50, new Color32(30, 30, 30, 255), new Vector2(180, 210), new Vector2(580, 160));
        Wrap(headline); headline.alignment = TextAnchor.UpperLeft;
        var sub = G.Txt(p, "Sub", "", 26, new Color32(60, 60, 60, 255), new Vector2(180, 30), new Vector2(580, 170));
        Wrap(sub); sub.alignment = TextAnchor.UpperLeft;
        for (int i = 0; i < 6; i++)
            G.Img(p, "Text" + i, new Color32(120, 120, 120, 255), new Vector2(-330 + (i % 2) * 0, -60 - i * 34), new Vector2(i % 3 == 2 ? 200 : 280, 8));
        var sideBack = G.Img(p, "SideBack", new Color32(215, 212, 205, 255), new Vector2(250, -220), new Vector2(480, 220));
        var side = G.Txt(sideBack.transform, "Side", "", 30, new Color32(30, 30, 30, 255), Vector2.zero, new Vector2(440, 200));
        Wrap(side);
        paper.rectTransform.localRotation = Quaternion.Euler(0, 0, -4f);
        var finish = G.Txt(paperPanel.transform, "Finish", "finish the game  >   (enter)", 36, Color.white, new Vector2(600, -640), new Vector2(700, 60));

        // end screens: joann's exported frames if they're there, otherwise rebuilt to match
        var tonyPng = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "ending_tony.png");
        var soloPng = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "ending_solo.png");

        var game = canvasGo.AddComponent<Ending>();
        var tonyEnd = EndScreen(root, "TonyEnd", tonyPng, new Color32(70, 176, 92, 255), new Color32(40, 120, 55, 255),
            "You and Tony", "ESCAPED!", new Color32(60, 230, 90, 255), robberSprite, tonySprite, game, out game.tonyButtons, out game.tonyHighlights);
        var soloEnd = EndScreen(root, "SoloEnd", soloPng, new Color32(226, 186, 80, 255), new Color32(160, 128, 40, 255),
            "You", "ESCAPED!", new Color32(255, 214, 70, 255), robberSprite, null, game, out game.soloButtons, out game.soloHighlights);

        game.timePanel = timePanel;
        game.timeText = timeText;
        game.timeLine = timeLine;
        game.runner = runner.rectTransform;
        game.car = car;
        game.prop = prop;
        game.carSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "ending_car.png");
        game.deskSoloSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "ending_desk_solo.png");
        game.deskTonySprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "ending_desk_tony.png");
        game.paperPanel = paperPanel;
        game.paper = paper.rectTransform;
        game.paperHeadline = headline;
        game.paperSub = sub;
        game.paperSide = side;
        game.paperPhoto = photo;
        game.robberSprite = robberSprite;
        game.tonySprite = tonySprite;
        game.finishPrompt = finish;
        game.tonyEnd = tonyEnd;
        game.soloEnd = soloEnd;

        EditorSceneManager.SaveScene(scene, EndingPath);
        var list = EditorBuildSettings.scenes.ToList();
        if (!list.Any(s => s.path == EndingPath)) list.Add(new EditorBuildSettingsScene(EndingPath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    static GameObject EndScreen(Transform root, string name, Sprite png, Color bg, Color bg2, string top, string bottom, Color titleCol,
        Sprite robber, Sprite tony, Ending game, out Button[] buttons, out Image[] highlights)
    {
        var panel = Panel(root, name, bg);
        var t = panel.transform;
        buttons = new Button[2];
        highlights = new Image[2];

        if (png != null)
        {
            // full figma frame, fit to width, invisible buttons over the drawn ones
            const float scale = 1920f / 1004f;
            panel.GetComponent<Image>().color = bg2;
            var frame = G.Img(t, "Frame", Color.white, Vector2.zero, new Vector2(1920, 639 * scale));
            frame.sprite = png;
            Vector2 F(float x, float y) => new Vector2(x * scale - 960f, 639f * scale / 2f - y * scale);
            Vector2 size = new Vector2(367 * scale, 56 * scale);
            for (int i = 0; i < 2; i++)
            {
                var pos = F(33 + 183.5f, (i == 0 ? 354.5f : 434f) + 28f);
                var b = G.Img(t, i == 0 ? "StartOver" : "Menu", new Color(1, 1, 1, 0), pos, size);
                b.raycastTarget = true;
                buttons[i] = b.gameObject.AddComponent<Button>();
                buttons[i].transition = Selectable.Transition.None;
                highlights[i] = G.Img(t, "Glow" + i, new Color(1, 1, 1, 0.18f), pos, size);
                Hover(b.gameObject, game, i);
            }
        }
        else
        {
            var t1 = G.Txt(t, "Top", top, 130, titleCol, new Vector2(-470, 330), new Vector2(900, 160));
            t1.alignment = TextAnchor.MiddleLeft; Outline(t1.gameObject);
            var t2 = G.Txt(t, "Bottom", bottom, 130, titleCol, new Vector2(-470, 170), new Vector2(900, 160));
            t2.alignment = TextAnchor.MiddleLeft; Outline(t2.gameObject);

            var r = G.Img(t, "Robber", Color.white, new Vector2(420, -120), new Vector2(380, 490));
            r.sprite = robber; r.preserveAspect = true;
            if (tony != null)
            {
                var tn = G.Img(t, "Tony", Color.white, new Vector2(700, 260), new Vector2(220, 280));
                tn.sprite = tony; tn.preserveAspect = true;
                tn.rectTransform.localRotation = Quaternion.Euler(0, 0, 18f);
            }

            string[] labels = { "START OVER", "RETURN TO MENU" };
            for (int i = 0; i < 2; i++)
            {
                var pos = new Vector2(-470, i == 0 ? -60 : -210);
                highlights[i] = G.Img(t, "Highlight" + i, Color.white, pos, new Vector2(772, 122));
                var img = G.Img(t, labels[i], i == 0 ? new Color32(30, 30, 30, 255) : new Color(0, 0, 0, 0.35f), pos, new Vector2(760, 110));
                img.raycastTarget = true;
                buttons[i] = img.gameObject.AddComponent<Button>();
                G.Txt(img.transform, "Label", labels[i], 40, Color.white, Vector2.zero, new Vector2(740, 100));
                Hover(img.gameObject, game, i);
            }
        }
        panel.SetActive(false);
        return panel;
    }

    static GameObject Panel(Transform root, string name, Color c)
    {
        var img = G.Img(root, name, c, Vector2.zero, Vector2.zero);
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return img.gameObject;
    }

    static void Wrap(Text t)
    {
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
    }

    static void Outline(GameObject go)
    {
        var o = go.AddComponent<Outline>(); o.effectColor = Color.black; o.effectDistance = new Vector2(6, -6);
        var o2 = go.AddComponent<Outline>(); o2.effectColor = Color.black; o2.effectDistance = new Vector2(-6, 6);
    }

    static void Hover(GameObject target, Ending game, int index)
    {
        var trig = target.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        trig.triggers.Add(entry);
        UnityEventTools.AddIntPersistentListener(entry.callback, game.Select, index);
    }
}
#endif
