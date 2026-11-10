using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{

    public static DialogueController Instance {get; private set;}
    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    public Image portraitImage;
    public Transform choiceContainer;
    public GameObject choiceButtonPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowDialogueUI(bool show)
    {
        dialoguePanel.SetActive(show);
        if (show) MakeEnterHint();
    }

    // small blinking "ENTER" in the corner of the dialogue box so players know how to move on
    TMP_Text enterHint;
    void MakeEnterHint()
    {
        if (enterHint != null || dialogueText == null) return;
        var go = new GameObject("EnterHint", typeof(RectTransform));
        go.transform.SetParent(dialoguePanel.transform, false);
        enterHint = go.AddComponent<TextMeshProUGUI>();
        enterHint.font = dialogueText.font;
        enterHint.fontSize = dialogueText.fontSize * 0.8f;
        enterHint.text = "ENTER >";
        enterHint.alignment = TextAlignmentOptions.BottomRight;
        enterHint.raycastTarget = false;
        var rt = enterHint.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-14, 10);
        rt.sizeDelta = new Vector2(200, 40);
        go.AddComponent<EnterHintBlink>();
    }

    public void SetNPCInfo(string npcName, Sprite portrait)
    {
        nameText.text = npcName;
        portraitImage.sprite = portrait;
    }

    public void SetDialogueText(string text)
    {
        dialogueText.text = text;
    }

    public void ClearChoices()
    {
        foreach (Transform child in choiceContainer) Destroy(child.gameObject);
    }

    public void CreateChoiceButton(string choiceText, UnityEngine.Events.UnityAction onClick)
    {
        GameObject choiceButton = Instantiate(choiceButtonPrefab, choiceContainer);
        choiceButton.GetComponentInChildren<TMP_Text>().text = choiceText;
        choiceButton.GetComponent<Button>().onClick.AddListener(onClick);
        choiceButton.GetComponent<Button>().onClick.AddListener(GameAudio.Click);
    }
}