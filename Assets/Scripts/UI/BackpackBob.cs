using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// little hop on the backpack icon whenever something lands in your inventory (same feel as the walk bob)
public class BackpackBob : MonoBehaviour
{
    public static void Play()
    {
        foreach (var img in FindObjectsByType<Image>(FindObjectsInactive.Exclude))
        {
            if (img.sprite == null || !img.sprite.name.StartsWith("backpack")) continue;
            var bob = img.GetComponent<BackpackBob>();
            if (bob == null) bob = img.gameObject.AddComponent<BackpackBob>();
            bob.Hop();
        }
    }

    RectTransform rt;
    Vector2 home;
    bool hasHome;
    Coroutine running;

    void Hop()
    {
        rt = (RectTransform)transform;
        if (!hasHome) { home = rt.anchoredPosition; hasHome = true; }
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(DoHop());
    }

    IEnumerator DoHop()
    {
        float t = 0, dur = 0.45f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            // two quick hops, the second smaller, with a squash on landing
            float h = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2f)) * (k < 0.5f ? 22f : 10f);
            rt.anchoredPosition = home + Vector2.up * h;
            float squash = h < 2f ? 0.9f : 1.05f;
            rt.localScale = new Vector3(2f - squash, squash, 1f);
            yield return null;
        }
        rt.anchoredPosition = home;
        rt.localScale = Vector3.one;
    }
}
