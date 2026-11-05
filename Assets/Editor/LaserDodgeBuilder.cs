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
            G.Img(world, "LampLight", new Color(0.55f, 1f, 0.7f, 0.05f), new Vector2(x, 60f), new Vector2(260f, 340f));
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
        var siren = G.Img(canvasGo.transform, "Siren", new Color(1, 0.15f, 0.15f, 0), new Vector2(-860, 330), new Vector2(420, 40));

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
        game.beamSprite = G.Load(Art + "laser_beam_thick.png");
        game.sparkSprite = G.Load(Art + "laser_spark.png");
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
        G.Img(t, "Pole", new Color32(22, 28, 25, 255), new Vector2(30, fromFloor ? -poleLen / 2f : poleLen / 2f), new Vector2(16, poleLen));
        // same black rounded emitter block that holds the beams in the vault, just bigger
        var body = G.Img(t, "Emitter", Color.white, new Vector2(10, 0), new Vector2(66, 88));
        body.sprite = G.Load(Art + "laser_emitter.png");
        led = G.Img(t, "Led", new Color(1f, 0.25f, 0.25f, 1f), new Vector2(22, 26), new Vector2(12, 12));
        muzzle = G.Img(t, "Muzzle", new Color(1f, 1f, 1f, 0), new Vector2(-30, 0), new Vector2(64, 64));
        muzzle.sprite = G.Load(Art + "laser_spark.png");
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
        if (!File.Exists(Art + "laser_beam_thick.png"))
        {
            // vertical slice of the figma beam, beefed up: dark edge, deep green, green, hot core, back out
            Color32 edge = new Color32(10, 70, 20, 255), deep = new Color32(30, 150, 40, 255),
                    green = new Color32(76, 230, 80, 255), light = new Color32(170, 255, 160, 255),
                    core = new Color32(245, 255, 240, 255);
            Color32[] rows = { edge, deep, green, green, light, core, core, light, green, green, deep, edge };
            var tex = new Texture2D(4, rows.Length);
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < 4; x++) tex.SetPixel(x, y, rows[y]);
            Save("laser_beam_thick.png", tex);
        }
        if (!File.Exists(Art + "laser_emitter.png"))
        {
            // rounded black block like the beam mounts on the vault walls
            int w = 11, h = 15;
            var tex = new Texture2D(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool corner = (x == 0 || x == w - 1) && (y == 0 || y == h - 1);
                    bool hi = x == 1 || y == h - 2;
                    tex.SetPixel(x, y, corner ? new Color32(0, 0, 0, 0) : hi ? new Color32(48, 52, 50, 255) : new Color32(10, 10, 10, 255));
                }
            Save("laser_emitter.png", tex);
        }
        if (!File.Exists(Art + "laser_spark.png"))
        {
            // little 4 point pixel star
            int n = 11, c = 5;
            var tex = new Texture2D(n, n);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int dx = Mathf.Abs(x - c), dy = Mathf.Abs(y - c);
                    Color32 col = new Color32(0, 0, 0, 0);
                    if ((dx == 0 && dy <= 5) || (dy == 0 && dx <= 5)) col = new Color32(170, 255, 170, 255);
                    if (dx == 1 && dy == 1) col = new Color32(110, 235, 110, 255);
                    if (dx + dy <= 1) col = new Color32(255, 255, 255, 255);
                    tex.SetPixel(x, y, col);
                }
            Save("laser_spark.png", tex);
        }
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

    // brooke rebuilt the vault + safe room with tilemaps, so everything here is placed off the tilemaps
    // instead of hardcoded pixels. safe to run again, it clears what it made last time.
    //   vault: laser gate just in front of the laser, door up into the safe room, spawns for coming back
    //   safe room: door at the bottom back down to the vault, spawn for arriving
    [MenuItem("Tools/Cheese Heist/Hook Up Laser Vault")]
    public static void HookUpVault()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // ---- laser vault ----
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LaserVault.unity");
        if (!FloorBounds("Ground", out var v)) { Debug.LogError("no Ground tilemap in LaserVault"); return; }

        Clear("LaserGate");
        Clear("Door to SafeRoom");
        ClearSpawn("past_lasers");
        ClearSpawn("from_saferoom");

        // the door sits where brooke put the LockedDoor sprite on the top wall
        var lockedDoor = GameObject.Find("LockedDoor");
        float doorX = lockedDoor != null ? lockedDoor.transform.position.x : v.center.x;

        // the laser runs across the middle of the room. gate goes right in front of it
        var laser = GameObject.Find("Laser");
        float laserY = laser != null ? laser.transform.position.y : v.center.y;

        var gate = new GameObject("LaserGate");
        gate.transform.position = new Vector2(v.center.x, laserY - 0.7f);
        var gbox = gate.AddComponent<BoxCollider2D>();
        gbox.isTrigger = true;
        gbox.size = new Vector2(v.size.x, 0.5f);
        gate.AddComponent<LaserGate>();

        var doors = Parent("Doors");
        var spawns = Parent("Spawns");
        MakeDoor(doors, "Door to SafeRoom", new Vector2(doorX, v.yMax - 0.3f), new Vector2(1.2f, 0.6f), "SafeRoom", "from_vault");
        MakeSpawn(spawns, "from_saferoom", new Vector2(doorX, v.yMax - 1.3f));
        MakeSpawn(spawns, "past_lasers", new Vector2(doorX, laserY + 1.1f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // ---- safe room ----
        scene = EditorSceneManager.OpenScene("Assets/Scenes/SafeRoom.unity");
        if (!FloorBounds("Floor", out var r)) { Debug.LogError("no Floor tilemap in SafeRoom"); return; }

        Clear("Door to LaserVault");
        ClearSpawn("from_vault");

        // doorway is the gap in the middle of the bottom wall (same as the figma map)
        doors = Parent("Doors");
        spawns = Parent("Spawns");
        MakeDoor(doors, "Door to LaserVault", new Vector2(r.center.x, r.yMin + 0.3f), new Vector2(1.4f, 0.6f), "LaserVault", "from_saferoom");
        MakeSpawn(spawns, "from_vault", new Vector2(r.center.x, r.yMin + 1.3f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("laser vault + safe room hooked up");
    }

    // world space rect covering every painted tile on that tilemap
    static bool FloorBounds(string mapName, out Rect rect)
    {
        rect = default;
        var map = Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsSortMode.None)
            .FirstOrDefault(t => t.gameObject.name == mapName);
        if (map == null) return false;
        map.CompressBounds();
        var b = map.cellBounds;
        Vector2 min = map.CellToWorld(b.min);
        Vector2 max = map.CellToWorld(b.max);
        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    static Transform Parent(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go.transform;
    }

    static void Clear(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    static void ClearSpawn(string id)
    {
        foreach (var sp in Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
            if (sp.id == id) Object.DestroyImmediate(sp.gameObject);
    }

    static void MakeDoor(Transform parent, string name, Vector2 pos, Vector2 size, string scene, string spawn)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = size;
        var door = go.AddComponent<RoomDoor>();
        door.targetScene = scene;
        door.targetSpawn = spawn;
    }

    static void MakeSpawn(Transform parent, string id, Vector2 pos)
    {
        var go = new GameObject("Spawn " + id);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.AddComponent<SpawnPoint>().id = id;
    }
}
#endif
