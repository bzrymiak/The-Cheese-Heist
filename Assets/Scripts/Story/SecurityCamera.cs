using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// camera room stealth. each camera sweeps a red cone back and forth; stand in it (with nothing in the way)
// for too long and you're caught. boxes block the view, so duck behind them and time your moves.
// added at runtime by StoryDirector to the camera sprites brooke placed, nothing to set up in the scene.
public class SecurityCamera : MonoBehaviour
{
    public float range = 9f;
    public float halfAngle = 16f;     // half of the cone width
    public float sweep = 55f;         // how far it pans either side
    public float period = 5f;         // seconds for a full pan back and forth
    public float spotTime = 0.55f;    // how long you can be in view before it's over
    public float baseAngle;           // where it points at the middle of its pan
    public float phase;

    internal static float exposure;   // shared, any camera (or searchlight) seeing you fills it
    static int lastFrame;
    static bool caught;

    MeshFilter mf;
    MeshRenderer mr;
    Material mat;
    Transform player;
    SpriteRenderer playerSr;
    Vector3 eye;
    Collider2D mount; // the wall it's bolted to, light passes through it

    public static void ResetAll() { exposure = 0; caught = false; }

    void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        eye = sr != null ? sr.bounds.center : transform.position;

        var cone = new GameObject("Cone");
        cone.transform.SetParent(null);
        cone.transform.position = new Vector3(eye.x, eye.y, 0);
        SceneManager.MoveGameObjectToScene(cone, gameObject.scene);
        mf = cone.AddComponent<MeshFilter>();
        mr = cone.AddComponent<MeshRenderer>();
        mat = new Material(Shader.Find("Sprites/Default"));
        mr.material = mat;
        if (sr != null) { mr.sortingLayerID = sr.sortingLayerID; mr.sortingOrder = sr.sortingOrder - 1; }

        var p = GameObject.FindWithTag("Player");
        if (p != null) { player = p.transform; playerSr = p.GetComponentInChildren<SpriteRenderer>(); }
        FindMount();
    }

    // the top camera sits behind the back wall's collider, so the first wall the
    // light hits straight out of the lens is the one it's mounted on
    void FindMount()
    {
        float a = baseAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        bool old = Physics2D.queriesStartInColliders;
        Physics2D.queriesStartInColliders = true;
        foreach (var hit in Physics2D.RaycastAll(eye, dir, 2f))
        {
            if (hit.collider.isTrigger) continue;
            if (player != null && hit.transform.IsChildOf(player)) continue;
            mount = hit.collider;
            break;
        }
        Physics2D.queriesStartInColliders = old;
    }

    bool Blocks(RaycastHit2D hit)
    {
        if (hit.collider.isTrigger || hit.collider == mount) return false;
        if (player != null && (hit.transform == player || hit.transform.IsChildOf(player))) return false;
        return true;
    }

    float CurrentAngle() => baseAngle + Mathf.Sin((Time.time / period + phase) * Mathf.PI * 2f) * sweep;

    void Update()
    {
        if (caught) return;
        float ang = CurrentAngle();
        bool sees = !PauseController.IsGamePaused && Sees(ang);
        DrawCone(ang, sees);

        // one shared meter, filled by whichever camera spots you, drained once per frame
        if (lastFrame != Time.frameCount)
        {
            lastFrame = Time.frameCount;
            exposure = Mathf.MoveTowards(exposure, 0, Time.deltaTime * 0.8f);
        }
        if (sees) exposure += Time.deltaTime * 1.8f;

        if (playerSr != null)
        {
            float k = Mathf.Clamp01(exposure / spotTime);
            playerSr.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), k * (0.6f + 0.4f * Mathf.Sin(Time.time * 30f)));
        }

        if (exposure >= spotTime && !caught)
        {
            caught = true;
            StartCoroutine(Busted());
        }
    }

    bool Sees(float ang)
    {
        if (player == null) return false;
        Vector2 to = (Vector2)(player.position - eye);
        float dist = to.magnitude;
        if (dist > range) return false;
        float a = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
        if (Mathf.Abs(Mathf.DeltaAngle(a, ang)) > halfAngle) return false;

        // the camera's bolted into a wall, so ignore whatever collider the ray starts inside
        bool old = Physics2D.queriesStartInColliders;
        Physics2D.queriesStartInColliders = false;
        bool clear = true;
        foreach (var hit in Physics2D.LinecastAll(eye, player.position))
        {
            if (!Blocks(hit)) continue;
            clear = false; // a box or wall is in the way
            break;
        }
        Physics2D.queriesStartInColliders = old;
        return clear;
    }

    void DrawCone(float ang, bool sees)
    {
        const int seg = 14;
        var verts = new Vector3[seg + 2];
        var cols = new Color[seg + 2];
        var tris = new int[seg * 3];
        verts[0] = Vector3.zero;
        // bright at the lens, fading out at the far end
        float pulse = sees ? 0.25f * Mathf.Abs(Mathf.Sin(Time.time * 20f)) : 0f;
        cols[0] = new Color(1f, 0.15f, 0.15f, 0.55f + pulse);
        bool old = Physics2D.queriesStartInColliders;
        Physics2D.queriesStartInColliders = false;
        for (int i = 0; i <= seg; i++)
        {
            float a = (ang - halfAngle + 2f * halfAngle * i / seg) * Mathf.Deg2Rad;
            // stop the cone where it hits a wall/box so hiding spots read clearly
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            float len = range;
            foreach (var hit in Physics2D.RaycastAll(eye, dir, range))
            {
                if (!Blocks(hit)) continue;
                len = hit.distance;
                break;
            }
            verts[i + 1] = dir * len;
            bool edge = i == 0 || i == seg;
            cols[i + 1] = new Color(1f, 0.2f, 0.2f, (edge ? 0.35f : 0.12f) + pulse * 0.5f);
        }
        for (int i = 0; i < seg; i++) { tris[i * 3] = 0; tris[i * 3 + 1] = i + 2; tris[i * 3 + 2] = i + 1; }
        var m = mf.mesh;
        m.Clear();
        m.vertices = verts;
        m.triangles = tris;
        m.colors = cols;
        Physics2D.queriesStartInColliders = old;
    }

    IEnumerator Busted()
    {
        if (playerSr != null) playerSr.color = new Color(1f, 0.3f, 0.3f);
        PauseController.SetPause(true); // freeze the player
        yield return new WaitForSeconds(0.9f);
        PauseController.SetPause(false);
        GameOver.Show(GameOver.Kind.Jail, SceneManager.GetActiveScene().name);
    }
}
