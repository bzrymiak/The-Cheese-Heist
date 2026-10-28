using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Instructions popup")]
    public GameObject instructionsPanel;
    public Text pageTitle;
    public Text pageBody;
    public Text pageCounter;
    public Button backButton;
    public Button nextButton;

    // placeholder text, swap for the real stuff later
    private readonly string[] titles =
    {
        "The Job",
        "Moving Around",
        "Clues & Items",
        "Stay Hidden",
        "Watch the Clock"
    };

    private readonly string[] bodies =
    {
        "Chef Tony wants Chef Reggie's secret cheese recipe. It's locked in a safe in Reggie's basement. Get in, get the recipe, get out.",
        "Use WASD or the arrow keys to move around the restaurant.",
        "Press F near something to interact with it. Clues and items go into your journal automatically, and you can check it any time.",
        "Cameras are watching the basement. Duck behind furniture so they don't spot you. Getting caught sends you back to your last checkpoint.",
        "Some parts are on a timer. If it runs out, you're caught. Find the safe password before then."
    };

    private int page;

    void Start()
    {
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
    }

    void Update()
    {
        if (instructionsPanel != null && instructionsPanel.activeSelf &&
            Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseInstructions();
    }

    public void StartGame()
    {
        SceneLoader.Load(SceneLoader.Intro);
    }

    public void OpenInstructions()
    {
        page = 0;
        ShowPage();
        instructionsPanel.SetActive(true);
    }

    public void CloseInstructions()
    {
        instructionsPanel.SetActive(false);
    }

    public void NextPage()
    {
        if (page < titles.Length - 1) page++;
        ShowPage();
    }

    public void PrevPage()
    {
        if (page > 0) page--;
        ShowPage();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowPage()
    {
        pageTitle.text = titles[page];
        pageBody.text = bodies[page];
        pageCounter.text = (page + 1) + " / " + titles.Length;
        backButton.interactable = page > 0;
        nextButton.interactable = page < titles.Length - 1;
    }
}
