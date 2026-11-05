using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class KeypadButton : MonoBehaviour
{
    public string letter = "A";

    [Tooltip("Leave empty to find the KeypadController on a parent.")]
    public KeypadController keypad;

    private Button button;
    private TMP_Text label;
    private Key key;
    private bool hasKey;
    private int enabledFrame = -1;

    void Awake()
    {
        button = GetComponent<Button>();
        label = GetComponentInChildren<TMP_Text>();
        if (keypad == null) keypad = GetComponentInParent<KeypadController>();

        Refresh();
        button.onClick.AddListener(OnPressed);
    }

    void OnEnable()
    {
        enabledFrame = Time.frameCount;
    }

    void Update()
    {
        if (!hasKey || Time.frameCount == enabledFrame) return;

        var kb = Keyboard.current;
        if (kb == null || !button.interactable) return;

        if (kb[key].wasPressedThisFrame)
            button.onClick.Invoke();
    }

    void OnPressed()
    {
        if (keypad != null && !string.IsNullOrEmpty(letter))
            keypad.AddLetter(letter.Substring(0, 1).ToUpper());
    }

    [ContextMenu("Refresh Label")]
    void Refresh()
    {
        if (string.IsNullOrEmpty(letter)) { hasKey = false; return; }

        char c = char.ToUpper(letter[0]);
        if (label != null) label.text = c.ToString();

        hasKey = true;
        if (c >= 'A' && c <= 'Z') key = Key.A + (c - 'A');
        else if (c == '0') key = Key.Digit0;
        else if (c >= '1' && c <= '9') key = Key.Digit1 + (c - '1');
        else hasKey = false; // symbols: clickable only
    }
}