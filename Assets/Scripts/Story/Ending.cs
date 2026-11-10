using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// the wrap up after you get out, following joann's ending wireframes:
//   time card ("3:00 AM, you slip out...") -> second beat -> "2 years later" newspaper -> end screen
// two versions: escaped with tony, or tony bailed and you went solo through the vent.
public class Ending : MonoBehaviour
{
    public const string SceneName = "Ending";

    static bool withTony = true;

    public static void Show(bool tony)
    {
        withTony = tony;
        Time.timeScale = 1f;
        PauseController.SetPause(false);
        SceneManager.LoadScene(SceneName);
    }

    [Header("Time cards")]
    public GameObject timePanel;
    public Text timeText, timeLine;
    public RectTransform runner;
    public Image car;                 // tony's blue getaway car on the first card
    public Image prop;                // second card picture (desk scenes)
    public Sprite carSprite, deskSoloSprite, deskTonySprite;

    [Header("Newspaper")]
    public GameObject paperPanel;
    public RectTransform paper;
    public Text paperHeadline, paperSub, paperSide;
    public Image paperPhoto;
    public Sprite robberSprite, tonySprite;
    public Text finishPrompt;

    [Header("End screens")]
    public GameObject tonyEnd, soloEnd;
    public Button[] tonyButtons = new Button[2];   // start over, menu
    public Button[] soloButtons = new Button[2];
    public Image[] tonyHighlights = new Image[2];
    public Image[] soloHighlights = new Image[2];

    int step;
    bool advance, onEndScreen;
    int selected;

    void Start()
    {
        Time.timeScale = 1f;
        PauseController.SetPause(false);
        timePanel.SetActive(false);
        paperPanel.SetActive(false);
        tonyEnd.SetActive(false);
        soloEnd.SetActive(false);

        Hook(tonyButtons);
        Hook(soloButtons);
        StartCoroutine(Play());
    }

    void Hook(Button[] b)
    {
        if (b == null || b.Length < 2) return;
        if (b[0] != null) b[0].onClick.AddListener(StartOver);
        if (b[1] != null) b[1].onClick.AddListener(ToMenu);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        bool enter = kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;

        if (onEndScreen)
        {
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) Select(0);
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Select(1);
            if (enter) { if (selected == 0) StartOver(); else ToMenu(); }
            return;
        }
        if (enter) advance = true;
    }

    IEnumerator Play()
    {
        // wording from joann's ending wireframes (34-39)
        string[] times = { "3:00 AM", "8:00 AM" };
        string[] lines = withTony
            ? new[] { "You successfully run over to Tony's getaway car.",
                      "You hand over the recipe and receive money from Tony." }
            : new[] { "You successfully escape with the recipe.",
                      "You read the recipe and unlock the secrets of cheese." };

        for (int i = 0; i < 2; i++)
        {
            yield return TimeCard(times[i], lines[i], i == 0);
        }

        // 2 years later, the paper slides up over the text
        timePanel.SetActive(true);
        timeText.text = "2 years later";
        timeLine.text = "";
        runner.gameObject.SetActive(false);
        if (car != null) car.gameObject.SetActive(false);
        if (prop != null) prop.gameObject.SetActive(false);
        yield return Wait(1.6f);

        paperPanel.SetActive(true);
        paperHeadline.text = withTony ? "Tony's Restaurant Makes a Comeback!" : "Mystery Chef's Restaurant Wins the Golden Whisk!";
        paperSub.text = withTony
            ? "Tony's brand new cheese has customers lining up around the block once again. Critics are calling it \"impossibly good\"."
            : "A newcomer's cheese has left Tony's and Reggie's in the dust. The chef refused to say where the recipe came from.";
        paperSide.text = "Reggie's Restaurant shut down after co-owner revealed to be a MOUSE.";
        if (paperPhoto != null) paperPhoto.sprite = withTony && tonySprite != null ? tonySprite : robberSprite;
        finishPrompt.gameObject.SetActive(false);

        Vector2 home = paper.anchoredPosition;
        Vector2 from = home + Vector2.down * 1400f;
        float t = 0;
        while (t < 0.7f)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.7f), 3f);
            paper.anchoredPosition = Vector2.Lerp(from, home, k);
            paper.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-14f, -4f, k));
            yield return null;
        }
        timePanel.SetActive(false);
        finishPrompt.gameObject.SetActive(true);
        advance = false;
        while (!advance) yield return null;

        // end screen
        paperPanel.SetActive(false);
        (withTony ? tonyEnd : soloEnd).SetActive(true);
        onEndScreen = true;
        Select(0);
    }

    IEnumerator TimeCard(string time, string line, bool showRunner)
    {
        timePanel.SetActive(true);
        timeText.text = time;
        timeLine.text = line;
        runner.gameObject.SetActive(showRunner);
        if (car != null) car.gameObject.SetActive(showRunner && withTony && carSprite != null);
        if (car != null && carSprite != null) car.sprite = carSprite;
        var desk = withTony ? deskTonySprite : deskSoloSprite;
        if (prop != null)
        {
            prop.gameObject.SetActive(!showRunner && desk != null);
            if (desk != null) prop.sprite = desk;
        }
        advance = false;
        float t = 0;
        while (t < 4f && !advance)
        {
            t += Time.deltaTime;
            if (showRunner)
            {
                // little robber running along the line, bobbing
                // with tony the run ends at the car
                float endX = withTony && carSprite != null ? 120f : 300f;
                runner.anchoredPosition = new Vector2(Mathf.Lerp(-300f, endX, Mathf.Clamp01(t / 3f)), -170f + Mathf.Abs(Mathf.Sin(t * 12f)) * 10f);
            }
            yield return null;
        }
        advance = false;
    }

    IEnumerator Wait(float s)
    {
        advance = false;
        float t = 0;
        while (t < s && !advance) { t += Time.deltaTime; yield return null; }
        advance = false;
    }

    public void Select(int i)
    {
        selected = i;
        var h = withTony ? tonyHighlights : soloHighlights;
        for (int k = 0; k < h.Length; k++) if (h[k] != null) h[k].enabled = k == i;
    }

    public void StartOver()
    {
        GameAudio.Click();
        Story.ResetAll();
        SceneManager.LoadScene("Intro");
    }

    public void ToMenu()
    {
        GameAudio.Click();
        SceneManager.LoadScene("Menu");
    }
}
