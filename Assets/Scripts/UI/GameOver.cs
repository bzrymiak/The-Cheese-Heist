using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// "you got ___!" screen from joann's figma. the mini games call GameOver.Show() when you lose,
// retry sends you back into whatever you lost, return to menu goes to the main menu.
public class GameOver : MonoBehaviour
{
    public enum Kind { KnockedOut, Lasers, Reggie, Vent, Jail }

    public const string SceneName = "GameOver";
    public const string MenuScene = "Menu";

    static Kind kind = Kind.KnockedOut;
    static string retryScene = "";

    public static void Show(Kind k, string retry)
    {
        kind = k;
        retryScene = retry;
        Time.timeScale = 1f;
        PauseController.SetPause(false);
        SceneManager.LoadScene(SceneName);
    }

    [Header("Art (one per kind)")]
    public GameObject knockedOutArt;
    public GameObject laserArt;

    [Header("Joann's exported frames (same order as Kind), used when there")]
    public Image frameImage;
    public Sprite[] frames = new Sprite[5];
    // her buttons aren't all the same width (the vent one is narrower), in figma pixels
    static readonly float[] buttonWidths = { 367f, 367f, 367f, 297f, 367f };

    [Header("UI")]
    public Text titleBottom;          // second line, "KNOCKED OUT!" / "CAUGHT!"
    public Button retryButton, menuButton;
    public Image retryOutline, menuOutline;

    int selected;

    void Start()
    {
        Time.timeScale = 1f;
        PauseController.SetPause(false);

        bool knocked = kind == Kind.KnockedOut || kind == Kind.Reggie;
        if (knockedOutArt != null) knockedOutArt.SetActive(knocked);
        if (laserArt != null) laserArt.SetActive(!knocked);
        if (titleBottom != null) titleBottom.text = knocked ? "KNOCKED OUT!" : "CAUGHT!";

        // the real figma frame for this kind if it's been exported, falls back to the first one
        if (frameImage != null && frames != null && frames.Length > 0)
        {
            int i = (int)kind;
            var f = i < frames.Length && frames[i] != null ? frames[i] : frames[0];
            if (f != null)
            {
                frameImage.sprite = f;
                frameImage.gameObject.SetActive(true);
                FitButtons(i < buttonWidths.Length ? buttonWidths[i] : 367f);
                if (knockedOutArt != null) knockedOutArt.SetActive(false);
                if (laserArt != null) laserArt.SetActive(false);
            }
        }

        retryButton.onClick.AddListener(Retry);
        menuButton.onClick.AddListener(ToMenu);
        Select(0);
    }

    // line the invisible buttons + glow up with the drawn ones on this frame
    void FitButtons(float figmaWidth)
    {
        const float scale = 1920f / 1004f;
        float w = figmaWidth * scale;
        float x = (33f + figmaWidth / 2f) * scale - 960f;
        foreach (var g in new Graphic[] { retryButton.GetComponent<Image>(), menuButton.GetComponent<Image>(), retryOutline, menuOutline })
        {
            if (g == null) continue;
            var rt = g.rectTransform;
            rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
            rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) Select(0);
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Select(1);
        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
        {
            if (selected == 0) Retry();
            else ToMenu();
        }
    }

    // mouse hover picks a button too (hooked up by the builder)
    public void Select(int i)
    {
        selected = i;
        if (retryOutline != null) retryOutline.enabled = i == 0;
        if (menuOutline != null) menuOutline.enabled = i == 1;
    }

    public void Retry()
    {
        GameAudio.Click();
        SceneManager.LoadScene(string.IsNullOrEmpty(retryScene) ? MenuScene : retryScene);
    }

    public void ToMenu()
    {
        GameAudio.Click();
        SceneManager.LoadScene(MenuScene);
    }
}
