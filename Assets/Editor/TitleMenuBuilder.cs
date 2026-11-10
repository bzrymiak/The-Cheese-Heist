#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using G = GusFightBuilder;

// Tools > Cheese Heist > Build Title Menu
// swaps the placeholder menu for joann's title + instructions screens (exported frames),
// with invisible buttons laid over her drawn ones
public static class TitleMenuBuilder
{
    const string ScenePath = "Assets/Scenes/Menu.unity";
    const string Art = "Assets/Sprites/Minigame/";
    const float Scale = 1920f / 1004f;

    static Vector2 F(float x, float y) => new Vector2(x * Scale - 960f, 639f * Scale / 2f - y * Scale);

    [MenuItem("Tools/Cheese Heist/Build Title Menu")]
    public static void Build()
    {
        var title = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "title_screen.png");
        var howTo = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "instructions_screen.png");
        if (title == null || howTo == null) { Debug.LogError("need title_screen.png and instructions_screen.png in " + Art); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(176, 140, 50, 255); // matches the edges of her gold frame
        camGo.transform.position = new Vector3(0, 0, -10);
        new GameObject("WindowManager").AddComponent<WindowManager>();

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
        var root = canvasGo.transform;

        var menu = root.gameObject.AddComponent<TitleMenu>();

        var bg = G.Img(root, "Title", Color.white, Vector2.zero, new Vector2(1920, 639 * Scale));
        bg.sprite = title;

        Vector2 btn = new Vector2(367 * Scale, 56 * Scale);
        menu.playButton = Invisible(root, "Play", F(62 + 183.5f, 319.9f + 28f), btn, out menu.playGlow, menu, 0);
        menu.instructionsButton = Invisible(root, "Instructions", F(62 + 183.5f, 391.5f + 28f), btn, out menu.instructionsGlow, menu, 1);

        // instructions card over the top
        var card = G.Img(root, "InstructionsScreen", Color.white, Vector2.zero, new Vector2(1920, 639 * Scale));
        card.sprite = howTo;
        card.raycastTarget = true; // eat clicks so you can't press PLAY through it
        menu.instructions = card.gameObject;
        menu.closeButton = Invisible(card.transform, "Close", F(923.4f + 20f, 35f + 34f), new Vector2(60 * Scale, 70 * Scale), out _, null, -1);
        var hint = G.Txt(card.transform, "Hint", "esc / enter to close", 28, new Color(0, 0, 0, 0.45f), new Vector2(0, -560), new Vector2(800, 40));

        EditorSceneManager.SaveScene(scene, ScenePath);
        var list = EditorBuildSettings.scenes.ToList();
        // menu has to stay first in the build
        list.RemoveAll(s => s.path == ScenePath);
        list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("title menu built");
    }

    static Button Invisible(Transform parent, string name, Vector2 pos, Vector2 size, out Image glow, TitleMenu menu, int index)
    {
        var img = G.Img(parent, name, new Color(1, 1, 1, 0), pos, size);
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        glow = null;
        if (menu != null)
        {
            glow = G.Img(parent, name + "Glow", new Color(1, 1, 1, 0.22f), pos, size);
            var trig = img.gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            trig.triggers.Add(entry);
            UnityEventTools.AddIntPersistentListener(entry.callback, menu.Select, index);
        }
        return b;
    }
}
#endif
