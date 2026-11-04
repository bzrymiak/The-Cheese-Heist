#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using G = GusFightBuilder;

// Tools > Cheese Heist > Build Laser Dodge Scene
// side view vault corridor run. rebuilds the scene every time you run it
public static class LaserDodgeBuilder
{
    const string ScenePath = "Assets/Scenes/LaserDodge.unity";
    const string Art = "Assets/Sprites/Minigame/";

    [MenuItem("Tools/Cheese Heist/Build Laser Dodge Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        G.MakeLightTextures();
        MakeVaultTextures();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        G.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PixelifySans-VariableFont_wght.ttf");
        if (G.font == null) G.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(12, 18, 14, 255);
        camGo.transform.position = new Vector3(0, 0, -10);

        new GameObject("WindowManager").AddComponent<WindowManager>();

        var canvasGo = new GameObject("LaserCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1440);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var stage = new GameObject("Stage", typeof(RectTransform)).GetComponent<RectTransform>();
        stage.SetParent(canvasGo.transform, false);
        stage.anchorMin = Vector2.zero; stage.anchorMax = Vector2.one;
        stage.offsetMin = Vector2.zero; stage.offsetMax = Vector2.zero;
        var root = stage.transform;

        // ---- the corridor (everything in World slides left as you run) ----
        var world = new GameObject("World", typeof(RectTransform)).GetComponent<RectTransform>();
        world.SetParent(root, false);
        const float length = 16000f;   // plenty, the run is ~20 seconds
        const float floorTop = -360f;

        // back wall, vault green tiles like the laser room in figma
        var wall = G.Img(world, "Wall", Color.white, new Vector2(length / 2f - 1200f, -60f), new Vector2(length, 600f));
        Tile(wall, "tile_vault_wall.png", 0.78f);
        // ceiling trim + skirting
        G.Img(world, "Ceiling", new Color32(20, 30, 24, 255), new Vector2(length / 2f - 1200f, 255f), new Vector2(length, 30f));
        G.Img(world, "Skirting", new Color32(16, 24, 19, 255), new Vector2(length / 2f - 1200f, floorTop + 6f), new Vector2(length, 12f));

        // floor + the caution stripe you see in the vault
        var floor = G.Img(world, "Floor", Color.white, new Vector2(length / 2f - 1200f, floorTop - 180f), new Vector2(length, 360f));
        Tile(floor, "tile_vault_floor.png", 0.78f);
        var caution = G.Img(world, "Caution", Color.white, new Vector2(length / 2f - 1200f, floorTop - 26f), new Vector2(length, 26f));
        Tile(caution, "tile_caution.png", 0.6f);

        // a few dim ceiling lamps so the wall isn't flat
        for (float x = -900f; x < length - 1200f; x += 900f)
        {
            var lamp = G.Img(world, "LampGlow", new Color(0.55f, 1f, 0.7f, 0.08f), new Vector2(x, 120f), new Vector2(520f, 380f));
            lamp.sprite = G.Load(Art + "light_pool.png");
            G.Img(world, "Lamp", new Color32(60, 72, 64, 255), new Vector2(x, 236f), new Vector2(90f, 14f));
        }

        // the safe at the end of the corridor
        var safe = G.Img(world, "Safe", new Color32(70, 76, 82, 255), new Vector2(0, floorTop + 150f), new Vector2(240, 300));
        G.Border(safe.gameObject, 6);
        var door = G.Img(safe.transform, "Door", new Color32(96, 104, 112, 255), new Vector2(0, 10), new Vector2(200, 240));
        G.Border(door.gameObject, 3);
        var dial = G.Img(door.transform, "Dial", new Color(1f, 0.75f, 0.2f), new Vector2(-30, 20), new Vector2(84, 84));
        dial.sprite = G.Load(Art + "hit_ring.png");
        G.Img(door.transform, "Handle", new Color32(40, 44, 48, 255), new Vector2(60, 20), new Vector2(16, 70));
        var keypad = G.Img(door.transform, "Keypad", new Color32(20, 40, 28, 255), new Vector2(0, -70), new Vector2(120, 50));
        G.Border(keypad.gameObject, 2);
        var safeGlow = G.Img(world, "SafeGlow", new Color(1f, 0.85f, 0.4f, 0.12f), new Vector2(0, floorTop + 150f), new Vector2(560, 520));
        safeGlow.sprite = G.Load(Art + "light_pool.png");
        safeGlow.transform.SetSiblingIndex(safe.transform.GetSiblingIndex());
        safeGlow.gameObject.AddComponent<FollowX>().target = safe.rectTransform;

        // ---- robber ----
        var shadow = G.Img(root, "RobberShadow", new Color(0, 0, 0, 0.45f), new Vector2(-420, floorTop), new Vector2(170, 40));
        shadow.sprite = G.Load(Art + "light_pool.png");
        var robber = G.Img(root, "Robber", Color.white, new Vector2(-420, floorTop), new Vector2(235, 302));
        robber.sprite = G.FindSprite("robber"); robber.preserveAspect = true;
        robber.rectTransform.pivot = new Vector2(0.5f, 0f);
        robber.rectTransform.anchoredPosition = new Vector2(-420, floorTop);
        shadow.gameObject.AddComponent<FollowX>().target = robber.rectTransform;

        // dark vignette so the lasers carry the lighting
        var dark = G.Img(root, "Darkness", new Color(0, 0, 0, 0.35f), Vector2.zero, Vector2.zero);
        Stretch(dark.rectTransform);

        // light the shots throw on the floor
        var floorGlow = G.Img(root, "FloorGlow", new Color(0.35f, 1f, 0.5f, 0), new Vector2(0, floorTop), new Vector2(520, 90));
        floorGlow.sprite = G.Load(Art + "light_pool.png");

        // turrets at the far end of the hall, same metal boxes + leds as the laser posts in the vault
        float lowY = -318f, highY = -165f;
        var tLow = Turret(root, "TurretLow", new Vector2(880, lowY), floorTop, true, out var ledLow, out var muzLow);
        var tHigh = Turret(root, "TurretHigh", new Vector2(880, highY), 250f, false, out var ledHigh, out var muzHigh);

        // shots draw over the darkness so they glow
        var bolts = new GameObject("Shots", typeof(RectTransform)).GetComponent<RectTransform>();
        bolts.SetParent(root, false);

        // ---- HUD ----
        var hudBack = G.Img(root, "HudBack", new Color32(18, 18, 18, 235), new Vector2(0, 530), new Vector2(1920, 380));

        var pFrame = G.Img(root, "ProgressBar", new Color32(235, 235, 235, 255), new Vector2(-560, 610), new Vector2(560, 64));
        G.Border(pFrame.gameObject, 5);
        G.Img(pFrame.transform, "Back", new Color32(60, 60, 60, 255), new Vector2(50, 0), new Vector2(430, 36));
        var pFill = G.Img(pFrame.transform, "Fill", new Color32(84, 214, 84, 255), new Vector2(50 - 215, 0), new Vector2(430, 36));
        pFill.rectTransform.pivot = new Vector2(0, 0.5f);
        G.Txt(pFrame.transform, "Name", "SAFE", 34, Color.black, new Vector2(-222, 0), new Vector2(100, 60));
        var pct = G.Txt(pFrame.transform, "Percent", "0%", 26, Color.white, new Vector2(50, 0), new Vector2(120, 40));

        var aFrame = G.Img(root, "AlarmBar", new Color32(235, 235, 235, 255), new Vector2(560, 610), new Vector2(560, 64));
        G.Border(aFrame.gameObject, 5);
        G.Img(aFrame.transform, "Back", new Color32(60, 60, 60, 255), new Vector2(50, 0), new Vector2(430, 36));
        var aFill = G.Img(aFrame.transform, "Fill", new Color32(226, 64, 64, 255), new Vector2(50 - 215, 0), new Vector2(430, 36));
        aFill.rectTransform.pivot = new Vector2(0, 0.5f);
        aFill.rectTransform.localScale = new Vector3(0, 1, 1);
        G.Txt(aFrame.transform, "Name", "ALARM", 30, Color.black, new Vector2(-222, 0), new Vector2(110, 60));

        var titleBox = G.Img(root, "TitleBox", new Color32(235, 235, 235, 255), new Vector2(0, 610), new Vector2(330, 110));
        G.Border(titleBox.gameObject, 6);
        G.Txt(titleBox.transform, "Label", "LASERS!", 66, Color.black, Vector2.zero, new Vector2(330, 110));

        var track = new GameObject("Track", typeof(RectTransform)).GetComponent<RectTransform>();
        track.SetParent(root, false);
        track.sizeDelta = new Vector2(1920, 220);
        track.anchoredPosition = new Vector2(0, 430);
        G.Img(track, "Line", new Color(1, 1, 1, 0.9f), Vector2.zero, new Vector2(1920, 8));

        var ring = G.Img(track, "HitCircle", Color.white, Vector2.zero, new Vector2(150, 150));
        ring.sprite = G.Load(Art + "hit_ring.png");
        var t1 = ring.gameObject.AddComponent<Outline>(); t1.effectColor = Color.white; t1.effectDistance = new Vector2(3, 3);
        var t2 = ring.gameObject.AddComponent<Outline>(); t2.effectColor = Color.white; t2.effectDistance = new Vector2(-3, -3);
        var guide = G.Img(ring.transform, "Guide", new Color(1, 1, 1, 0.55f), Vector2.zero, new Vector2(110, 110));
        guide.preserveAspect = true;

        var judge = G.Txt(root, "Judgement", "", 52, Color.white, new Vector2(0, 280), new Vector2(900, 70));
        var center = G.Txt(root, "Center", "", 100, Color.white, new Vector2(0, 20), new Vector2(1400, 420));
        center.supportRichText = true;
        var centerShadow = center.gameObject.AddComponent<Shadow>();
        centerShadow.effectColor = new Color(0, 0, 0, 0.8f); centerShadow.effectDistance = new Vector2(4, -4);

        // alarm effects, outside the stage so they don't shake
        var red = G.Img(canvasGo.transform, "RedPulse", new Color(1, 0, 0, 0), Vector2.zero, Vector2.zero);
        Stretch(red.rectTransform);
        var siren = G.Img(canvasGo.transform, "Siren", new Color(1, 0.15f, 0.15f, 0), new Vector2(-860, 330), new Vector2(520, 120));
        siren.sprite = G.Load(Art + "light_pool.png");

        // ---- how to play card, shows before the countdown ----
        var card = G.Img(canvasGo.transform, "IntroCard", new Color(0, 0, 0, 0.82f), Vector2.zero, Vector2.zero);
        Stretch(card.rectTransform);
        var panel = G.Img(card.transform, "Panel", new Color32(235, 235, 235, 255), new Vector2(0, 0), new Vector2(1100, 720));
        G.Border(panel.gameObject, 8);
        G.Txt(panel.transform, "Title", "DON'T TRIP THE LASERS", 64, Color.black, new Vector2(0, 280), new Vector2(1000, 90));
        Row(panel.transform, 130, G.Load(Art + "arrow_up.png"), false, "JUMP over the low shots");
        Row(panel.transform, -10, G.Load(Art + "arrow_down.png"), false, "DUCK under the high shots");
        Row(panel.transform, -150, G.Load(Art + "arrow_down.png"), true, "HOLD down to slide under long laser beams");
        G.Txt(panel.transform, "Go", "press enter to start", 36, new Color(0, 0, 0, 0.6f), new Vector2(0, -290), new Vector2(900, 60));

        track.SetAsLastSibling();
        judge.transform.SetAsLastSibling();
        center.transform.SetAsLastSibling();

        var game = canvasGo.AddComponent<LaserDodge>();
        game.stage = stage;
        game.world = world;
        game.boltLayer = bolts;
        game.turretLow = tLow; game.turretHigh = tHigh;
        game.ledLow = ledLow; game.ledHigh = ledHigh;
        game.muzzleLow = muzLow; game.muzzleHigh = muzHigh;
        game.floorGlow = floorGlow;
        game.lowBeamY = lowY; game.highBeamY = highY;
        game.safe = safe.rectTransform;
        game.track = track;
        game.hitCircle = ring.rectTransform;
        game.hitRing = ring;
        game.guide = guide;
        game.judgeText = judge;
        game.centerText = center;
        game.progressFill = pFill.rectTransform;
        game.progressText = pct;
        game.alarmFill = aFill.rectTransform;
        game.robber = robber.rectTransform;
        game.redPulse = red;
        game.siren = siren;
        game.introCard = card.gameObject;
        game.glowSprite = G.Load(Art + "light_pool.png");
        game.floorY = floorTop;
        game.arrowUp = G.Load(Art + "arrow_up.png");
        game.arrowDown = G.Load(Art + "arrow_down.png");
        game.arrowUpHit = G.Load(Art + "arrow_up_hit.png");
        game.arrowDownHit = G.Load(Art + "arrow_down_hit.png");
        game.arrowUpOff = G.Load(Art + "arrow_up_off.png");
        game.arrowDownOff = G.Load(Art + "arrow_down_off.png");

        EditorSceneManager.SaveScene(scene, ScenePath);

        var list = EditorBuildSettings.scenes.ToList();
        if (!list.Any(s => s.path == ScenePath)) list.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();

        Debug.Log("laser dodge built, press play");
    }

    // a laser turret: mount pole (floor or ceiling), metal housing, barrel pointing left, status led, muzzle glow
    static RectTransform Turret(Transform root, string name, Vector2 pos, float mountY, bool fromFloor,
        out Image led, out Image muzzle)
    {
        var t = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        t.SetParent(root, false);
        t.anchoredPosition = pos;

        float poleLen = Mathf.Abs(mountY - pos.y);
        G.Img(t, "Pole", new Color32(30, 38, 34, 255), new Vector2(20, fromFloor ? -poleLen / 2f : poleLen / 2f), new Vector2(18, poleLen));
        var body = G.Img(t, "Housing", new Color32(58, 66, 62, 255), new Vector2(20, 0), new Vector2(90, 70));
        G.Border(body.gameObject, 3);
        var barrel = G.Img(t, "Barrel", new Color32(40, 46, 44, 255), new Vector2(-48, 0), new Vector2(60, 26));
        G.Border(barrel.gameObject, 2);
        led = G.Img(t, "Led", new Color(1f, 0.25f, 0.25f, 1f), new Vector2(38, 18), new Vector2(14, 14));
        muzzle = G.Img(t, "Muzzle", new Color(0.4f, 1f, 0.55f, 0), new Vector2(-82, 0), new Vector2(110, 110));
        muzzle.sprite = G.Load(Art + "light_pool.png");
        return t;
    }

    static void Row(Transform parent, float y, Sprite arrow, bool hold, string text)
    {
        var icon = G.Img(parent, "Icon", Color.white, new Vector2(-380, y), new Vector2(90, 90));
        icon.sprite = arrow; icon.preserveAspect = true;
        if (hold)
        {
            var tail = G.Img(parent, "Tail", new Color(0.7f, 0.45f, 1f, 0.75f), new Vector2(-300, y), new Vector2(110, 34));
            tail.transform.SetSiblingIndex(icon.transform.GetSiblingIndex());
        }
        var t = G.Txt(parent, "Text", text, 40, Color.black, new Vector2(80, y), new Vector2(760, 60));
        t.alignment = TextAnchor.MiddleLeft;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void Tile(Image img, string file, float ppuMultiplier)
    {
        img.sprite = G.Load(Art + file);
        img.type = Image.Type.Tiled;
        img.pixelsPerUnitMultiplier = ppuMultiplier;
    }

    // little pixel textures for the corridor so we don't need extra art
    static void MakeVaultTextures()
    {
        Directory.CreateDirectory(Art);

        if (!File.Exists(Art + "tile_vault_wall.png"))
            Save("tile_vault_wall.png", Bricks(new Color32(30, 52, 38, 255), new Color32(18, 32, 24, 255), new Color32(40, 66, 48, 255)));
        if (!File.Exists(Art + "tile_vault_floor.png"))
            Save("tile_vault_floor.png", Bricks(new Color32(24, 40, 30, 255), new Color32(12, 22, 16, 255), new Color32(34, 54, 40, 255), false));
        if (!File.Exists(Art + "tile_caution.png"))
        {
            var tex = new Texture2D(32, 16);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 32; x++)
                    tex.SetPixel(x, y, ((x + y) / 8) % 2 == 0 ? new Color32(236, 190, 40, 255) : new Color32(24, 24, 24, 255));
            Save("tile_caution.png", tex);
        }
        AssetDatabase.Refresh();
    }

    // 64x64 tile with grout lines and a light top edge on each block
    static Texture2D Bricks(Color32 face, Color32 grout, Color32 light, bool offset = true)
    {
        var tex = new Texture2D(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                int row = y / 32;
                int xx = offset && row == 1 ? (x + 32) % 64 : x;
                bool g = y % 32 < 2 || xx % 64 < 2;
                bool hi = y % 32 == 29 || y % 32 == 30;
                tex.SetPixel(x, y, g ? grout : hi ? light : face);
            }
        return tex;
    }

    static void Save(string file, Texture2D tex)
    {
        File.WriteAllBytes(Art + file, tex.EncodeToPNG());
    }

    // puts a trigger on the caution line in the vault (step past it and the mini game starts)
    // and a spawn by the safe for when you come back. only touches those two things
    [MenuItem("Tools/Cheese Heist/Hook Up Laser Vault")]
    public static void HookUpVault()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LaserVault.unity");

        var old = GameObject.Find("LaserGate");
        if (old != null) Object.DestroyImmediate(old);
        foreach (var sp in Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
            if (sp.id == "past_lasers") Object.DestroyImmediate(sp.gameObject);

        // vault art is 738x526 at 64 px per unit, centred on 0,0
        Vector2 W(float x, float y) => new Vector2((x - 369f) / 64f, (263f - y) / 64f);

        var gate = new GameObject("LaserGate");
        gate.transform.position = W(369, 390);
        var box = gate.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(706f / 64f, 60f / 64f);
        gate.AddComponent<LaserGate>();

        var spawns = GameObject.Find("Spawns");
        var sp2 = new GameObject("Spawn past_lasers");
        if (spawns != null) sp2.transform.SetParent(spawns.transform);
        sp2.transform.position = W(320, 165);
        sp2.AddComponent<SpawnPoint>().id = "past_lasers";

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("laser vault hooked up");
    }
}
#endif
