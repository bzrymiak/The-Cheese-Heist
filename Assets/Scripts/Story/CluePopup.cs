using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// full screen close-up of a clue (newspaper, sticky note...). Show() waits until it's closed.
public static class CluePopup
{
    public static IEnumerator Show(Sprite art)
    {
        if (art == null) yield break;
        PauseController.SetPause(true);

        var root = new GameObject("CluePopup");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;

        var dim = Child<Image>(root.transform, "Dim");
        dim.color = new Color(0, 0, 0, 0.75f);
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;

        var pic = Child<Image>(root.transform, "Clue");
        pic.sprite = art;
        pic.preserveAspect = true;
        float h = 800f;
        float w = h * art.rect.width / art.rect.height;
        if (w > 1500f) { w = 1500f; h = w * art.rect.height / art.rect.width; }
        pic.rectTransform.sizeDelta = new Vector2(w, h);
        pic.rectTransform.anchoredPosition = new Vector2(0, 30);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var any = Object.FindAnyObjectByType<Text>();
        if (any != null) font = any.font;
        var hint = Child<Text>(root.transform, "Hint");
        hint.font = font;
        hint.text = "press ENTER to close";
        hint.fontSize = 34;
        hint.alignment = TextAnchor.MiddleCenter;
        hint.color = new Color(1, 1, 1, 0.7f);
        hint.rectTransform.sizeDelta = new Vector2(900, 60);
        hint.rectTransform.anchoredPosition = new Vector2(0, -490);

        // pop in
        for (float t = 0; t < 0.15f; t += Time.unscaledDeltaTime)
        {
            pic.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, t / 0.15f);
            yield return null;
        }
        pic.rectTransform.localScale = Vector3.one;

        yield return null; // don't eat the key that opened it
        while (true)
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame
                || kb.escapeKey.wasPressedThisFrame || kb.iKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                break;
            yield return null;
        }
        Object.Destroy(root);
        PauseController.SetPause(false);
        yield return null;
    }

    static T Child<T>(Transform parent, string name) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var c = go.AddComponent<T>();
        if (c is Graphic g) g.raycastTarget = false;
        return c;
    }
}
