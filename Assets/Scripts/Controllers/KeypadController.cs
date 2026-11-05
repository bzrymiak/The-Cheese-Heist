using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeypadController : MonoBehaviour
{
    [SerializeField] private string answer = "MOUSE";
    [SerializeField] private int maxLength = 5;
    [SerializeField] private TMP_Text guessDisplay;

    [Tooltip("Optional. Set as a global flag when the correct answer is submitted (e.g. for a door to check).")]
    [SerializeField] private string successFlagId;

    [Header("Feedback")]
    [SerializeField] private Image flashPanel;
    [SerializeField] private Color incorrectFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color correctFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private int flashCount = 2;

    public UnityEvent onCorrect;
    public UnityEvent onIncorrect;

    private string guess = "";
    private int enabledFrame = -1;
    private Color originalColor;
    private bool isFlashing;

    public string Guess => guess;

    void Awake()
    {
        if (flashPanel != null) originalColor = flashPanel.color;
    }

    void OnEnable()
    {
        enabledFrame = Time.frameCount;
        Clear(); // fresh guess every time the popup opens
    }

    void OnDisable()
    {
        // popup closed mid-flash: coroutine stops with the object, so put things back
        isFlashing = false;
        if (flashPanel != null) flashPanel.color = originalColor;
    }

    void Update()
    {
        if (Time.frameCount == enabledFrame) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.backspaceKey.wasPressedThisFrame) Backspace();
        if (kb.enterKey.wasPressedThisFrame) Submit();
    }

    public void AddLetter(string letter)
    {
        if (isFlashing || guess.Length >= maxLength) return;
        guess += letter;
        UpdateDisplay();
    }

    // hook these to on-screen buttons too, if you have them
    public void Backspace()
    {
        if (isFlashing || guess.Length == 0) return;
        guess = guess.Substring(0, guess.Length - 1);
        UpdateDisplay();
    }

    public void Clear()
    {
        guess = "";
        UpdateDisplay();
    }

    public void Submit()
    {
        if (isFlashing) return;

        if (string.Equals(guess, answer, System.StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(successFlagId)) GameFlags.Set(successFlagId);
            Correct();
            onCorrect.Invoke();
        }
        else
        {
            Incorrect();
        }
    }

    // flashes the panel red, then clears the guess.
    public void Incorrect()
    {
        if (isFlashing) return;

        onIncorrect.Invoke();

        if (flashPanel == null)
        {
            Clear();
            return;
        }

        StartCoroutine(FlashRoutine(incorrectFlashColor));
    }

    public void Correct()
    {
        if (isFlashing) return;

        onCorrect.Invoke();

        if (flashPanel == null)
        {
            return;
        }

        StartCoroutine(FlashRoutine(correctFlashColor));
    }

    IEnumerator FlashRoutine(Color color)
    {
        isFlashing = true;

        for (int i = 0; i < flashCount; i++)
        {
            flashPanel.color = color;
            yield return new WaitForSecondsRealtime(flashDuration);
            flashPanel.color = originalColor;
            yield return new WaitForSecondsRealtime(flashDuration);
        }

        if (color == incorrectFlashColor) Clear();
        isFlashing = false;
    }

    void UpdateDisplay()
    {
        if (guessDisplay != null) guessDisplay.text = guess;
    }
}