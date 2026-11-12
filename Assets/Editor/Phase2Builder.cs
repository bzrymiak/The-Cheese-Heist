#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using G = GusFightBuilder;

// Tools > Cheese Heist > Build Phase 2 (Menu, Vent Maze)
public static class Phase2Builder
{
    const string VentPath = "Assets/Scenes/VentMaze.unity";

    [MenuItem("Tools/Cheese Heist/Build Phase 2 (Menu, Vent Maze)")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildVent();
        TitleMenuBuilder.Build();
        Debug.Log("phase 2 built");
    }

    // puts joann's plaque close-up into the dining room plaque's inspect popup (was the sticky note placeholder)
    [MenuItem("Tools/Cheese Heist/Use Plaque Close-up")]
    public static void UsePlaque()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/plaque_closeup.png");
        if (sprite == null)
        {
            // items folder isn't on the pixel-art importer, make sure it comes in as a sprite
            var imp = AssetImporter.GetAtPath("Assets/Sprites/Items/plaque_closeup.png") as TextureImporter;
            if (imp == null) { Debug.LogError("export the plaque as Assets/Sprites/Items/plaque_closeup.png first"); return; }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/plaque_closeup.png");
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/DiningRoom2.unity");
        int n = 0;
        foreach (var item in Object.FindObjectsByType<InspectableItem>(FindObjectsInactive.Include))
        {
            if (item.gameObject.name != "Plaque") continue;
            var so = new SerializedObject(item);
            so.FindProperty("inspectImage").objectReferenceValue = sprite;
            so.ApplyModifiedProperties();
            n++;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("plaque close-up set on " + n + " plaque(s)");
    }

    // makes the recipe book item (a copy of brooke's item prefab with the book art) and hooks it into StoryContent
    [MenuItem("Tools/Cheese Heist/Build Recipe Book + Story Extras")]
    public static void BuildRecipeBook()
    {
        const string art = "Assets/Sprites/Items/recipe_book.png";
        var imp = AssetImporter.GetAtPath(art) as TextureImporter;
        if (imp == null) { Debug.LogError("missing " + art); return; }
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100; // same as the desk art it was cut from
        imp.filterMode = FilterMode.Bilinear;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(art);

        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item.prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        go.name = "RecipeBook";
        var item = go.GetComponent<Item>();
        item.title = "Reggie's Secret Cheese Recipe";
        item.description = "The famous recipe, straight out of Reggie's safe. Somebody drew a tiny mouse in the margins.";
        item.detailedView = sprite;
        var img = go.GetComponent<Image>();
        if (img != null) img.sprite = sprite;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = sprite;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/RecipeBook Variant.prefab");
        Object.DestroyImmediate(go);

        // same desk art with the book painted out, lined up with the original (centre pivot, 100 ppu)
        const string desk = "Assets/Sprites/Items/desk_nobook.png";
        var dimp = AssetImporter.GetAtPath(desk) as TextureImporter;
        if (dimp != null)
        {
            dimp.textureType = TextureImporterType.Sprite;
            dimp.spriteImportMode = SpriteImportMode.Single;
            dimp.spritePixelsPerUnit = 100;
            var ds = new TextureImporterSettings();
            dimp.ReadTextureSettings(ds);
            ds.spriteAlignment = (int)SpriteAlignment.Center; // the original desk sprite is centred too
            dimp.SetTextureSettings(ds);
            dimp.filterMode = FilterMode.Bilinear;
            dimp.SaveAndReimport();
        }

        var content = AssetDatabase.LoadAssetAtPath<StoryContent>("Assets/Resources/StoryContent.asset");
        if (content == null) { Debug.LogError("run Build Story Content first"); return; }
        content.recipeBook = prefab;
        content.deskNoBook = AssetDatabase.LoadAssetAtPath<Sprite>(desk);
        // borrow the "E to pickup" tooltip from the rolling pin so the book matches every other pickup
        var pin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RollingPinItem Variant.prefab");
        var tip = pin != null ? pin.transform.Find("tooltip") : null;
        content.pickupTooltip = tip != null ? tip.gameObject : null;
        if (tip == null) Debug.LogWarning("couldn't find the rolling pin's tooltip");

        // joann's clue art
        foreach (var name in new[] { "painting", "key_on_wall", "trophy_frame", "sticky_poop", "newspaper_reggie", "sticky_riddle" })
        {
            var path = "Assets/Sprites/Items/Clues/" + name + ".png";
            var cimp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (cimp == null) { Debug.LogWarning("missing " + path); continue; }
            cimp.textureType = TextureImporterType.Sprite;
            cimp.spriteImportMode = SpriteImportMode.Single;
            cimp.spritePixelsPerUnit = 100;
            cimp.filterMode = FilterMode.Point;
            cimp.textureCompression = TextureImporterCompression.Uncompressed;
            cimp.mipmapEnabled = false;
            cimp.SaveAndReimport();
        }
        content.clueKeyWall = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/Clues/key_on_wall.png");
        content.clueSticky = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/Clues/sticky_poop.png");
        content.clueRiddle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/Clues/sticky_riddle.png");
        content.clueNewspaper = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/Clues/newspaper_reggie.png");

        content.pixelFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PixelifySans-VariableFont_wght.ttf");

        // fight / maze music and the ouch
        content.fightMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/fighting.mp3");
        content.mazeMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/maze_audio.mp3");
        content.ouch = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ouch.mp3");

        // portraits and the inspect bubble the story code needs
        content.tonyRadio = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Characters/Tony/walkietalkie.png");
        content.inspectTooltip = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/inspectTooltip.png").OfType<Sprite>().FirstOrDefault();
        const string reggieHead = "Assets/Sprites/Characters/Reggie/reggiehead.png";
        var rimp = AssetImporter.GetAtPath(reggieHead) as TextureImporter;
        if (rimp != null)
        {
            rimp.textureType = TextureImporterType.Sprite;
            rimp.spriteImportMode = SpriteImportMode.Single;
            rimp.filterMode = FilterMode.Point;
            rimp.textureCompression = TextureImporterCompression.Uncompressed;
            rimp.SaveAndReimport();
            content.reggiePortrait = AssetDatabase.LoadAssetAtPath<Sprite>(reggieHead);
        }
        EditorUtility.SetDirty(content);
        AssetDatabase.SaveAssets();
        Debug.Log("recipe book built");
    }

    static void BuildVent()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        G.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PixelifySans-VariableFont_wght.ttf");
        if (G.font == null) G.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(52, 64, 74, 255);
        camGo.transform.position = new Vector3(0, 0, -10);
        new GameObject("WindowManager").AddComponent<WindowManager>();

        var canvasGo = new GameObject("VentCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1440);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        var title = G.Txt(root, "Title", "VENT ESCAPE", 72, Color.white, new Vector2(0, 600), new Vector2(1200, 100));
        var frame = G.Img(root, "Frame", new Color32(30, 36, 42, 255), new Vector2(0, 0), new Vector2(23 * 64 + 40, 13 * 64 + 40));
        var board = new GameObject("Board", typeof(RectTransform)).GetComponent<RectTransform>();
        board.SetParent(root, false);
        var status = G.Txt(root, "Status", "", 40, new Color32(255, 210, 90, 255), new Vector2(0, -500), new Vector2(1600, 60));
        G.Txt(root, "Help", "WASD / arrows to crawl.   Stand on a red button + E to flip the vent gates.   Reach EXIT before Django gets you.", 28,
            new Color(1, 1, 1, 0.6f), new Vector2(0, -600), new Vector2(1800, 50));
        var flash = G.Img(root, "Flash", new Color(1, 0, 0, 0), Vector2.zero, Vector2.zero);
        flash.rectTransform.anchorMin = Vector2.zero; flash.rectTransform.anchorMax = Vector2.one;
        flash.rectTransform.offsetMin = flash.rectTransform.offsetMax = Vector2.zero;

        var maze = canvasGo.AddComponent<VentMaze>();
        maze.board = board;
        maze.statusText = status;
        maze.titleText = title;
        maze.flash = flash;
        maze.robberSprite = G.FindSpriteQuiet("robber");
        maze.djangoSprite = G.FindSpriteQuiet("django");
        if (maze.djangoSprite == null) maze.djangoSprite = G.FindSpriteQuiet("image 340");

        EditorSceneManager.SaveScene(scene, VentPath);
        var list = EditorBuildSettings.scenes.ToList();
        if (!list.Any(s => s.path == VentPath)) list.Add(new EditorBuildSettingsScene(VentPath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
