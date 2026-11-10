using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// title screen from joann's figma: PLAY / INSTRUCTIONS, and the instructions card with its X.
// mouse or arrow keys + enter both work. esc / enter / X closes the instructions.
public class TitleMenu : MonoBehaviour
{
    public Button playButton, instructionsButton, closeButton;
    public Image playGlow, instructionsGlow;
    public GameObject instructions;

    int selected;

    void Start()
    {
        Time.timeScale = 1f;
        playButton.onClick.AddListener(Play);
        instructionsButton.onClick.AddListener(OpenInstructions);
        if (closeButton != null) closeButton.onClick.AddListener(CloseInstructions);
        instructions.SetActive(false);
        Select(0);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        bool enter = kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;

        if (instructions.activeSelf)
        {
            if (enter || kb.escapeKey.wasPressedThisFrame) CloseInstructions();
            return;
        }

        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) Select(0);
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Select(1);
        if (enter) { if (selected == 0) Play(); else OpenInstructions(); }
    }

    public void Select(int i)
    {
        selected = i;
        if (playGlow != null) playGlow.enabled = i == 0;
        if (instructionsGlow != null) instructionsGlow.enabled = i == 1;
    }

    void Play()
    {
        GameAudio.Click();
        Story.ResetAll();
        SceneManager.LoadScene("Intro");
    }

    void OpenInstructions() { GameAudio.Click(); instructions.SetActive(true); }
    void CloseInstructions() { GameAudio.Click(); instructions.SetActive(false); }
}
