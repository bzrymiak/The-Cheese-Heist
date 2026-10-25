#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Tools > Cheese Heist > Build Rooms + Doors
// makes the two dining room scenes from the figma layout and hooks up doors between them and the kitchen.
// all the numbers below are pixel positions straight from the figma "map" frames (64px = 1 tile)
public static class RoomBuilder
{
    class Box
    {
        public float x, y, w, h;
        public Box(float x, float y, float w, float h) { this.x = x; this.y = y; this.w = w; this.h = h; }
    }

    class Door
    {
        public Box box; public string scene; public string spawn;
        public Door(Box b, string scene, string spawn) { box = b; this.scene = scene; this.spawn = spawn; }
    }

    class Room
    {
        public string name;
        public float w, h;
        public List<Box> walls = new List<Box>();
        public List<Box> furniture = new List<Box>();
        public List<Door> doors = new List<Door>();
        public Dictionary<string, Vector2> spawns = new Dictionary<string, Vector2>();
        public string firstSpawn;
    }

    const string KitchenScene = "Kitchen";
    const string KitchenW = "673";

    [MenuItem("Tools/Cheese Heist/Build Rooms + Doors")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        foreach (var room in Rooms())
            BuildRoom(room);

        SetUpKitchenDoors();
        AddToBuildSettings("Assets/Scenes/DiningRoom.unity", "Assets/Scenes/DiningRoom2.unity");

        Debug.Log("rooms built. play from the Kitchen and walk through the right or bottom door");
    }

    static List<Room> Rooms()
    {
        // dining room to the right of the kitchen
        var d1 = new Room { name = "DiningRoom", w = 738, h = 526 };
        AddWalls(d1);
        d1.furniture.AddRange(new[] {
            new Box(475,332,127,132), new Box(148,397,67,64), new Box(535,205,67,64),
            new Box(605,318,68,83), new Box(218,381,68,83), new Box(605,189,68,83), new Box(77,381,68,83),
            new Box(464,189,68,83), new Box(404,318,68,83), new Box(605,381,68,83), new Box(404,381,68,83),
            new Box(660,88,47,111), new Box(24,82,47,111) // plants
        });
        d1.doors.Add(new Door(new Box(16, 271, 40, 64), KitchenScene, "from_dining"));
        d1.spawns["from_kitchen"] = new Vector2(95, 303);
        d1.firstSpawn = "from_kitchen";

        // dining room below the kitchen (the one with the arch)
        var d2 = new Room { name = "DiningRoom2", w = 738, h = 526 };
        AddWalls(d2);
        d2.furniture.AddRange(new[] {
            new Box(148,332,127,132), new Box(535,397,67,64), new Box(148,205,67,64),
            new Box(77,318,68,83), new Box(464,381,68,83), new Box(77,189,68,83), new Box(605,381,68,83),
            new Box(218,189,68,83), new Box(278,318,68,83), new Box(77,381,68,83), new Box(278,381,68,83),
            new Box(605,88,47,111), new Box(403,88,47,111) // plants
        });
        d2.doors.Add(new Door(new Box(463, 144, 128, 36), KitchenScene, "from_dining2"));
        d2.spawns["from_kitchen"] = new Vector2(527, 225);
        d2.firstSpawn = "from_kitchen";

        return new List<Room> { d1, d2 };
    }

    static void AddWalls(Room r)
    {
        r.walls.Add(new Box(0, 0, r.w, 150));        // top wall (includes the wall face)
        r.walls.Add(new Box(0, r.h - 16, r.w, 16));  // bottom
        r.walls.Add(new Box(0, 0, 16, r.h));         // left
        r.walls.Add(new Box(r.w - 16, 0, 16, r.h));  // right
    }

    // figma pixels (top-left origin) -> world units, room centered on 0,0
    static Vector2 ToWorld(Room r, float x, float y)
    {
        return new Vector2((x - r.w / 2f) / 64f, (r.h / 2f - y) / 64f);
    }

    static void BuildRoom(Room r)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.transform.position = new Vector3(0, 0, -10);

        new GameObject("WindowManager").AddComponent<WindowManager>();

        // background art
        var bg = new GameObject("Background");
        var sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = GetRoomSprite(r);
        sr.sortingOrder = -10;

        var walls = new GameObject("Walls");
        foreach (var b in r.walls) AddSolid(walls.transform, r, b, 1f);

        // only block the bottom chunk of furniture so you can walk "behind" the tops
        var furn = new GameObject("Furniture");
        foreach (var b in r.furniture) AddSolid(furn.transform, r, b, 0.45f);

        var doors = new GameObject("Doors");
        foreach (var d in r.doors) AddDoor(doors.transform, r, d);

        var spawns = new GameObject("Spawns");
        foreach (var kv in r.spawns) AddSpawn(spawns.transform, kv.Key, ToWorld(r, kv.Value.x, kv.Value.y));

        // reuse the kitchen setup to make the player + camera follow
        KitchenSetup.Setup();
        var player = GameObject.Find("Player");
        if (player != null) player.transform.position = ToWorld(r, r.spawns[r.firstSpawn].x, r.spawns[r.firstSpawn].y);

        // same follow camera as the kitchen
        var follow = cam.GetComponent<CameraFollow>();
        if (follow != null && player != null) camGo.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/" + r.name + ".unity");
    }

    static void AddSolid(Transform parent, Room r, Box b, float bottomPart)
    {
        float top = b.y + b.h * (1f - bottomPart);
        float h = b.h * bottomPart;
        var go = new GameObject("Solid");
        go.transform.SetParent(parent, false);
        var center = ToWorld(r, b.x + b.w / 2f, top + h / 2f);
        go.transform.position = center;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(b.w / 64f, h / 64f);
    }

    static void AddDoor(Transform parent, Room r, Door d)
    {
        var go = new GameObject("Door to " + d.scene);
        go.transform.SetParent(parent, false);
        go.transform.position = ToWorld(r, d.box.x + d.box.w / 2f, d.box.y + d.box.h / 2f);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(d.box.w / 64f, d.box.h / 64f);
        var door = go.AddComponent<RoomDoor>();
        door.targetScene = d.scene;
        door.targetSpawn = d.spawn;
    }

    static void AddSpawn(Transform parent, string id, Vector2 pos)
    {
        var go = new GameObject("Spawn " + id);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.AddComponent<SpawnPoint>().id = id;
    }

    // uses Sprites/Rooms/<RoomName>.png if someone exported it from figma, otherwise makes a rough placeholder
    static Sprite GetRoomSprite(Room r)
    {
        string real = "Assets/Sprites/Rooms/" + r.name + ".png";
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(real);
        if (s != null) return s;

        string ph = "Assets/Sprites/Rooms/" + r.name + "_placeholder.png";
        int w = Mathf.RoundToInt(r.w), h = Mathf.RoundToInt(r.h);
        var tex = new Texture2D(w, h);
        var px = new Color32[w * h];
        Color32 floor = new Color32(176, 84, 66, 255), wall = new Color32(170, 145, 130, 255),
                edge = new Color32(70, 40, 35, 255), furn = new Color32(110, 50, 40, 255), door = new Color32(235, 210, 160, 255);
        for (int i = 0; i < px.Length; i++) px[i] = floor;

        void Fill(Box b, Color32 c)
        {
            for (int y = (int)b.y; y < (int)(b.y + b.h); y++)
                for (int x = (int)b.x; x < (int)(b.x + b.w); x++)
                    if (x >= 0 && x < w && y >= 0 && y < h) px[(h - 1 - y) * w + x] = c;
        }

        Fill(new Box(0, 0, w, 144), wall);
        foreach (var b in r.walls.Skip(1)) Fill(b, edge);
        Fill(new Box(0, 0, w, 16), edge);
        foreach (var b in r.furniture) Fill(b, furn);
        foreach (var d in r.doors) Fill(d.box, door);

        tex.SetPixels32(px);
        tex.Apply();
        Directory.CreateDirectory("Assets/Sprites/Rooms");
        File.WriteAllBytes(ph, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(ph);
        return AssetDatabase.LoadAssetAtPath<Sprite>(ph);
    }

    static void SetUpKitchenDoors()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Kitchen.unity");

        var old = GameObject.Find("Doors");
        if (old != null) Object.DestroyImmediate(old);
        var oldSp = GameObject.Find("Spawns");
        if (oldSp != null) Object.DestroyImmediate(oldSp);

        // line the figma kitchen up with the painted border tilemap
        var border = Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None)
            .FirstOrDefault(t => t.gameObject.name.ToLower().Contains("border"));
        if (border == null) { Debug.LogError("couldn't find the Border tilemap in Kitchen"); return; }

        var b = border.bounds;
        float scale = b.size.x / float.Parse(KitchenW);
        Vector2 K(float x, float y) => new Vector2(b.min.x + x * scale, b.max.y - y * scale);

        // the right door has to start on the floor, the border tile's wall sits right where the old trigger was
        var ground = Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None)
            .FirstOrDefault(t => t.gameObject.name.ToLower().Contains("ground"));
        float floorRight = ground != null ? ground.bounds.max.x : K(657, 0).x;
        float doorTop = K(0, 272).y, doorBottom = K(0, 336).y;

        var doors = new GameObject("Doors");
        MakeKitchenDoor(doors.transform, new Vector2(floorRight - 0.7f, doorTop), new Vector2(floorRight + 0.6f, doorBottom), "DiningRoom", "from_kitchen");
        MakeKitchenDoor(doors.transform, K(463, 478), K(592, 510), "DiningRoom2", "from_kitchen");

        var spawns = new GameObject("Spawns");
        AddSpawn(spawns.transform, "from_dining", new Vector2(floorRight - 1.5f, (doorTop + doorBottom) / 2f));
        AddSpawn(spawns.transform, "from_dining2", K(527, 440));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void MakeKitchenDoor(Transform parent, Vector2 topLeft, Vector2 bottomRight, string scene, string spawn)
    {
        var go = new GameObject("Door to " + scene);
        go.transform.SetParent(parent, false);
        go.transform.position = (topLeft + bottomRight) / 2f;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(Mathf.Abs(bottomRight.x - topLeft.x), Mathf.Abs(bottomRight.y - topLeft.y));
        var door = go.AddComponent<RoomDoor>();
        door.targetScene = scene;
        door.targetSpawn = spawn;
    }

    static void AddToBuildSettings(params string[] paths)
    {
        var list = EditorBuildSettings.scenes.ToList();
        foreach (var p in paths)
            if (!list.Any(s => s.path == p)) list.Add(new EditorBuildSettingsScene(p, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
