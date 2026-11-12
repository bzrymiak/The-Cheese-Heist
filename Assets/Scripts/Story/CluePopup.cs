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

        // use brooke's inspect panel (with its CLOSE button) so it matches the plaque and everything else
        var panel = PopupRegistry.Get("Inspect");
        var view = panel != null ? panel.GetComponentInChildren<InspectPanelView>(true) : null;
        if (view != null)
        {
            bool closed = false;
            System.Action onClose = () => closed = true;
            PopupRegistry.CloseRequested += onClose;
            view.SetImage(art);
            // brooke's panel stretches the picture to its box, which blows the square sticky notes up huge.
            // shrink tall/square art so it sits about the size of the plaque close-up
            Image shown = null;
            foreach (var img in panel.GetComponentsInChildren<Image>(true)) if (img.sprite == art) shown = img;
            Vector3 oldScale = shown != null ? shown.rectTransform.localScale : Vector3.one;
            if (shown != null)
            {
                float aspect = art.rect.height / art.rect.width;
                shown.rectTransform.localScale = oldScale * (aspect > 0.8f ? 0.55f : 0.85f);
            }
            panel.SetActive(true);
            PauseController.SetPause(true);
            yield return null; // don't eat the key that opened it
            while (!closed && panel.activeInHierarchy)
            {
                var k = Keyboard.current;
                if (k != null && (k.escapeKey.wasPressedThisFrame || k.iKey.wasPressedThisFrame)) break;
                yield return null;
            }
            PopupRegistry.CloseRequested -= onClose;
            if (shown != null) shown.rectTransform.localScale = oldScale; // the plaque uses this panel too
            panel.SetActive(false);
            PauseController.SetPause(false);
            yield return null;
            yield break;
        }

        // fallback if the run didn't start from the menu and brooke's panel isn't around
        PauseController.SetPause(true);

        var root = new GameObject("CluePopup");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        root.AddComponent<GraphicRaycaster>(); // so the X can be clicked

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

        // X in the corner, same look as the backpack's close button
        var content = StoryContent.Get();
        var font = content != null && content.pixelFont != null ? content.pixelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bool clicked = false;
        var x = Child<Image>(pic.transform, "Close");
        x.raycastTarget = true;
        x.color = new Color32(255, 178, 128, 255);
        x.rectTransform.anchorMin = x.rectTransform.anchorMax = new Vector2(1, 1);
        x.rectTransform.sizeDelta = new Vector2(64, 64);
        x.rectTransform.anchoredPosition = new Vector2(-10, -10);
        foreach (var d in new[] { new Vector2(5, -5), new Vector2(-5, 5) })
        {
            var o = x.gameObject.AddComponent<Outline>();
            o.effectColor = new Color32(92, 46, 30, 255);
            o.effectDistance = d;
        }
        var btn = x.gameObject.AddComponent<Button>();
        btn.targetGraphic = x;
        btn.onClick.AddListener(() => { GameAudio.Click(); clicked = true; });
        var label = Child<Text>(x.transform, "X");
        label.font = font;
        label.text = "X";
        label.fontSize = 40;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color32(60, 30, 20, 255);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;

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
            if (clicked) break;
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
