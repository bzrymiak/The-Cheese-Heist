using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// pixel circle wipe between rooms. To() closes the circle, loads, and it opens again on the other side.
// going in or out of a minigame always opens with the circle, even when the scene was loaded by brooke's code.
public class Transition : MonoBehaviour
{
    const int W = 320, H = 180; // low res on purpose so the circle edge is chunky like the art
    const float Speed = 0.45f;

    static Transition instance;
    static readonly string[] Games = { "GusFight", "ReggieFight", "LaserDodge", "VentMaze", "Ending", "GameOver" };

    RawImage screen;
    Texture2D tex;
    Color32[] px;
    bool closedForLoad;
    string lastScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() => Ensure();

    static Transition Ensure()
    {
        if (instance != null) return instance;
        var go = new GameObject("Transition");
        DontDestroyOnLoad(go);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        instance = go.AddComponent<Transition>();

        var img = new GameObject("Wipe", typeof(RectTransform)).AddComponent<RawImage>();
        img.transform.SetParent(go.transform, false);
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        instance.tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        instance.px = new Color32[W * H];
        img.texture = instance.tex;
        img.enabled = false;
        instance.screen = img;
        instance.lastScene = SceneManager.GetActiveScene().name;

        SceneManager.sceneLoaded += instance.OnLoaded;
        return instance;
    }

    void OnDestroy() { SceneManager.sceneLoaded -= OnLoaded; }

    void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        bool game = System.Array.IndexOf(Games, scene.name) >= 0 || System.Array.IndexOf(Games, lastScene) >= 0;
        lastScene = scene.name;
        if (!closedForLoad && !game) return;
        closedForLoad = false;
        Draw(0f); // black before the first frame shows
        StopAllCoroutines();
        StartCoroutine(Open());
    }

    // radius 0 = all black, 1 = fully open
    void Draw(float radius)
    {
        float max = Mathf.Sqrt(W * W + H * H) * 0.5f;
        float r = radius * max;
        float r2 = r * r;
        var black = new Color32(0, 0, 0, 255);
        var clear = new Color32(0, 0, 0, 0);
        for (int y = 0; y < H; y++)
        {
            float dy = y - H * 0.5f + 0.5f;
            for (int x = 0; x < W; x++)
            {
                float dx = x - W * 0.5f + 0.5f;
                px[y * W + x] = dx * dx + dy * dy < r2 ? clear : black;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        screen.enabled = radius < 1f;
    }

    IEnumerator Close()
    {
        for (float t = 0; t < Speed; t += Time.unscaledDeltaTime)
        {
            Draw(1f - t / Speed);
            yield return null;
        }
        Draw(0f);
    }

    IEnumerator Open()
    {
        yield return null;
        for (float t = 0; t < Speed; t += Time.unscaledDeltaTime)
        {
            Draw(t / Speed);
            yield return null;
        }
        Draw(1f);
    }

    // close the circle (scene stays as it is)
    public static IEnumerator Out()
    {
        var t = Ensure();
        t.StopAllCoroutines();
        yield return t.Close();
        t.closedForLoad = true; // whatever loads next opens back up
    }

    public static IEnumerator To(string sceneName)
    {
        yield return Out();
        SceneManager.LoadScene(sceneName);
    }
}
