using UnityEngine;
using UnityEngine.SceneManagement;

// after you beat gus in the fight he's left lying on the kitchen floor, out cold, with stars going round his head.
// hooks itself in when the kitchen loads so nobody has to touch the kitchen scene.
public class GusAftermath : MonoBehaviour
{
    public const string KnockedOutFlag = "GusKnockedOut";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!GameFlags.Has(KnockedOutFlag)) return;

        foreach (var npc in Object.FindObjectsByType<NPC>(FindObjectsSortMode.None))
        {
            if (npc.gameObject.name != "Gus") continue;
            if (npc.GetComponent<GusAftermath>() == null) npc.gameObject.AddComponent<GusAftermath>();
        }
    }

    Transform[] stars;

    void Start()
    {
        // no more talking
        var npc = GetComponent<NPC>();
        if (npc != null) npc.enabled = false;

        // flat on his back
        transform.rotation = Quaternion.Euler(0, 0, 90f);
        transform.position += new Vector3(0, -0.3f, 0);

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(0.85f, 0.85f, 0.85f);
            sr.sortingOrder = Mathf.Min(sr.sortingOrder, 0);
        }

        // collider lies down with him so you can walk around him properly
        var box = GetComponent<BoxCollider2D>();
        if (box != null) box.size = new Vector2(box.size.x, box.size.y * 0.6f);

        // little shadow
        var shadow = new GameObject("KOShadow").AddComponent<SpriteRenderer>();
        shadow.sprite = Ellipse();
        shadow.color = new Color(0, 0, 0, 0.3f);
        shadow.transform.position = transform.position + new Vector3(0, -0.05f, 0.1f);
        shadow.transform.localScale = new Vector3(1.3f, 0.5f, 1);
        if (sr != null) { shadow.sortingLayerID = sr.sortingLayerID; shadow.sortingOrder = sr.sortingOrder - 1; }

        // dizzy stars
        var star = Star();
        stars = new Transform[3];
        for (int i = 0; i < stars.Length; i++)
        {
            var s = new GameObject("DizzyStar").AddComponent<SpriteRenderer>();
            s.sprite = star;
            if (sr != null) { s.sortingLayerID = sr.sortingLayerID; s.sortingOrder = sr.sortingOrder + 5; }
            stars[i] = s.transform;
        }
    }

    void Update()
    {
        if (stars == null) return;

        // head is at the left end once he's lying down (rotated 90)
        Vector3 head = transform.position + new Vector3(-0.45f, 0.15f, 0);
        for (int i = 0; i < stars.Length; i++)
        {
            float a = Time.time * 3f + i * Mathf.PI * 2f / stars.Length;
            stars[i].position = head + new Vector3(Mathf.Cos(a) * 0.3f, Mathf.Sin(a) * 0.1f + 0.25f, -0.1f);
            // stars behind his head go a bit smaller
            stars[i].localScale = Vector3.one * (Mathf.Sin(a) > 0 ? 0.8f : 1f);
        }

        // slow breathing so he doesn't look like a prop
        float b = 1f + Mathf.Sin(Time.time * 2f) * 0.03f;
        transform.localScale = new Vector3(1f, b, 1f);
    }

    void OnDestroy()
    {
        if (stars == null) return;
        foreach (var s in stars) if (s != null) Destroy(s.gameObject);
    }

    // tiny 5x5 pixel star, made here so we don't need another png
    static Sprite Star()
    {
        var tex = new Texture2D(5, 5) { filterMode = FilterMode.Point };
        Color y = new Color(1f, 0.85f, 0.2f), c = Color.clear;
        Color[] px =
        {
            c, c, y, c, c,
            c, y, y, y, c,
            y, y, Color.white, y, y,
            c, y, y, y, c,
            c, c, y, c, c,
        };
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 5, 5), new Vector2(0.5f, 0.5f), 32);
    }

    static Sprite Ellipse()
    {
        int w = 16, h = 8;
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Point };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f - w / 2f) / (w / 2f), dy = (y + 0.5f - h / 2f) / (h / 2f);
                tex.SetPixel(x, y, dx * dx + dy * dy < 1f ? Color.white : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16);
    }
}
