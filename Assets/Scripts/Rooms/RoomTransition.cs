using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// handles fading out, loading the next room and putting the player at the right spawn
public class RoomTransition : MonoBehaviour
{
    static RoomTransition instance;
    static string pendingSpawn;

    const float FadeTime = 0.25f;

    Image fade;
    bool busy;

    public static void Go(string scene, string spawnId)
    {
        if (instance == null) Create();
        if (instance.busy) return;
        instance.StartCoroutine(instance.DoTransition(scene, spawnId));
    }

    static void Create()
    {
        var go = new GameObject("RoomTransition");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<RoomTransition>();

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var img = new GameObject("Fade", typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(go.transform, false);
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        instance.fade = img;
    }

    IEnumerator DoTransition(string scene, string spawnId)
    {
        busy = true;
        SetPlayerFrozen(true);

        yield return Fade(0f, 1f);

        pendingSpawn = spawnId;
        yield return SceneManager.LoadSceneAsync(scene);
        PlaceAtSpawn();

        yield return Fade(1f, 0f);
        SetPlayerFrozen(false);
        busy = false;
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < FadeTime)
        {
            t += Time.unscaledDeltaTime;
            fade.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / FadeTime));
            yield return null;
        }
        fade.color = new Color(0, 0, 0, to);
    }

    static void PlaceAtSpawn()
    {
        if (string.IsNullOrEmpty(pendingSpawn)) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        foreach (var sp in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp.id != pendingSpawn) continue;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.position = sp.transform.position;
            player.transform.position = sp.transform.position;

            var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (cam != null) cam.SnapToTarget();
            break;
        }
        pendingSpawn = null;
    }

    static void SetPlayerFrozen(bool frozen)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        var pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.canMove = !frozen;
    }
}
