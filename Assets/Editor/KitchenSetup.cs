#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Tools > Cheese Heist > Set Up Player & Collisions
// adds colliders to border/furniture, makes the player, hooks up the camera
public static class KitchenSetup
{
    const string RobberSpritePath = "Assets/Sprites/Characters/robber.png";
    const string PlaceholderPath = "Assets/Sprites/Characters/placeholder_player.png";

    [MenuItem("Tools/Cheese Heist/Set Up Player & Collisions")]
    public static void Setup()
    {
        int count = 0;
        foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            string n = tm.gameObject.name.ToLower();
            if (n.Contains("border") || n.Contains("furniture") || n.Contains("wall"))
            {
                AddCollider(tm.gameObject);
                count++;
            }
            if (n.Contains("furniture"))
            {
                var r = tm.GetComponent<TilemapRenderer>();
                if (r != null) r.sortingOrder = 1;
            }
        }

        var player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");
            player.tag = "Player";

            var sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = GetPlayerSprite();
            sr.sortingOrder = 5;

            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            // just the feet, so you can walk right up to counters
            var col = player.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 0.3f);
            col.offset = new Vector2(0f, -0.35f);

            player.AddComponent<PlayerController>();
        }

        // swap the grey square for the real robber once it exists
        var psr = player.GetComponentInChildren<SpriteRenderer>();
        var robber = AssetDatabase.LoadAssetAtPath<Sprite>(RobberSpritePath);
        if (psr != null && robber != null) psr.sprite = robber;

        // move the sprite onto a child so the bob doesn't fight the physics
        var rootSr = player.GetComponent<SpriteRenderer>();
        if (rootSr != null)
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            var vsr = visual.AddComponent<SpriteRenderer>();
            vsr.sprite = rootSr.sprite;
            vsr.sortingOrder = rootSr.sortingOrder;
            Object.DestroyImmediate(rootSr);
        }
        var bob = player.GetComponent<WalkBob>();
        if (bob == null) bob = player.AddComponent<WalkBob>();
        bob.visual = player.transform.Find("Visual");

        var cam = Camera.main;
        if (cam != null)
        {
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = player;
        Debug.Log("player + colliders set up (" + count + " tilemaps). save the scene!");
    }

    static void AddCollider(GameObject go)
    {
        var tc = go.GetComponent<TilemapCollider2D>();
        if (tc == null) tc = go.AddComponent<TilemapCollider2D>();

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        if (go.GetComponent<CompositeCollider2D>() == null) go.AddComponent<CompositeCollider2D>();
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;
    }

    static Sprite GetPlayerSprite()
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(RobberSpritePath);
        if (s != null) return s;

        // no robber art yet, make a grey square for now
        if (AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderPath) == null)
        {
            var tex = new Texture2D(64, 64);
            var px = new Color32[64 * 64];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(40, 40, 40, 255);
            tex.SetPixels32(px);
            tex.Apply();
            System.IO.File.WriteAllBytes(PlaceholderPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(PlaceholderPath);

            var imp = (TextureImporter)AssetImporter.GetAtPath(PlaceholderPath);
            imp.textureType = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = 64;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        Debug.LogWarning("no robber.png in Sprites/Characters yet, using a placeholder");
        return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderPath);
    }
}
#endif
