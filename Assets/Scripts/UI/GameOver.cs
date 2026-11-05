using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// "you got ___!" screen from joann's figma. the mini games call GameOver.Show() when you lose,
// retry sends you back into whatever you lost, return to menu goes to the main menu.
public class GameOver : MonoBehaviour
{
    public enum Kind { KnockedOut, Lasers }

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

    [Header("UI")]
    public Text titleBottom;          // second line, "KNOCKED OUT!" / "CAUGHT!"
    public Button retryButton, menuButton;
    public Image retryOutline, menuOutline;

    int selected;

    void Start()
    {
        Time.timeScale = 1f;
        PauseController.SetPause(false);

        if (knockedOutArt != null) knockedOutArt.SetActive(kind == Kind.KnockedOut);
        if (laserArt != null) laserArt.SetActive(kind == Kind.Lasers);
        if (titleBottom != null) titleBottom.text = kind == Kind.KnockedOut ? "KNOCKED OUT!" : "CAUGHT!";

        retryButton.onClick.AddListener(Retry);
        menuButton.onClick.AddListener(ToMenu);
        Select(0);
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
        SceneManager.LoadScene(string.IsNullOrEmpty(retryScene) ? MenuScene : retryScene);
    }

    public void ToMenu()
    {
        SceneManager.LoadScene(MenuScene);
    }
}
