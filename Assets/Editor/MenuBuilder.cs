#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Tools > Cheese Heist > Build Placeholder Menu
// rebuilds the Menu scene UI from scratch and sets up build settings
public static class MenuBuilder
{
    static Font font;

    [MenuItem("Tools/Cheese Heist/Build Placeholder Menu")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // clear out old menu stuff so this can be re-run
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == "MenuCanvas" || go.name == "EventSystem") Object.DestroyImmediate(go);

        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
        }

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        var canvasGo = new GameObject("MenuCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1440);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var menu = canvasGo.AddComponent<MainMenu>();
        var root = canvasGo.transform;

        // title screen
        MakeText(root, "Tag", "[ PLACEHOLDER TITLE SCREEN ]", 32, new Color(1, 0.8f, 0.3f), new Vector2(0, 450), new Vector2(1200, 60));
        MakeText(root, "Title", "The Cheese Heist", 110, Color.white, new Vector2(0, 330), new Vector2(1400, 150));

        var start = MakeButton(root, "StartButton", "Start", new Vector2(0, 60), new Vector2(420, 90));
        var instr = MakeButton(root, "InstructionsButton", "How to Play", new Vector2(0, -60), new Vector2(420, 90));
        var quit = MakeButton(root, "QuitButton", "Quit", new Vector2(0, -180), new Vector2(420, 90));
        UnityEventTools.AddPersistentListener(start.onClick, menu.StartGame);
        UnityEventTools.AddPersistentListener(instr.onClick, menu.OpenInstructions);
        UnityEventTools.AddPersistentListener(quit.onClick, menu.QuitGame);

        // instructions popup
        var panel = MakeImage(root, "InstructionsPanel", new Color(0, 0, 0, 0.6f), Vector2.zero, Vector2.zero);
        Stretch(panel.rectTransform);
        var win = MakeImage(panel.transform, "Window", new Color(0.3f, 0.3f, 0.3f), Vector2.zero, new Vector2(1100, 760));

        MakeText(win.transform, "Tag", "[ PLACEHOLDER INSTRUCTIONS ]", 28, new Color(1, 0.8f, 0.3f), new Vector2(0, 320), new Vector2(900, 50));
        var title = MakeText(win.transform, "PageTitle", "Page title", 64, Color.white, new Vector2(0, 220), new Vector2(950, 90));
        var body = MakeText(win.transform, "PageBody", "Page text", 38, Color.white, new Vector2(0, 0), new Vector2(950, 320));
        body.alignment = TextAnchor.UpperCenter;
        var counter = MakeText(win.transform, "PageCounter", "1 / 5", 32, Color.white, new Vector2(0, -300), new Vector2(200, 50));

        var back = MakeButton(win.transform, "BackButton", "< Back", new Vector2(-330, -300), new Vector2(260, 80));
        var next = MakeButton(win.transform, "NextButton", "Next >", new Vector2(330, -300), new Vector2(260, 80));
        var close = MakeButton(win.transform, "CloseButton", "X", new Vector2(500, 330), new Vector2(70, 70));
        UnityEventTools.AddPersistentListener(back.onClick, menu.PrevPage);
        UnityEventTools.AddPersistentListener(next.onClick, menu.NextPage);
        UnityEventTools.AddPersistentListener(close.onClick, menu.CloseInstructions);

        menu.instructionsPanel = panel.gameObject;
        menu.pageTitle = title;
        menu.pageBody = body;
        menu.pageCounter = counter;
        menu.backButton = back;
        menu.nextButton = next;
        panel.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Menu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Intro.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Kitchen.unity", true),
        };

        Debug.Log("placeholder menu built + saved. Menu, Intro, Kitchen are in build settings now");
    }

    static Image MakeImage(Transform parent, string name, Color color, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = img.rectTransform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return img;
    }

    static Text MakeText(Transform parent, string name, string text, int size, Color color, Vector2 pos, Vector2 box)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        return t;
    }

    static Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        var img = MakeImage(parent, name, new Color(0.85f, 0.85f, 0.85f), pos, size);
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var lbl = MakeText(img.transform, "Label", label, 40, Color.black, Vector2.zero, size);
        Stretch(lbl.rectTransform);
        return btn;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
