using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    public GameObject currentItem;

    public Image background;
    private Color normalColour = new Color(0.88f, 0.66f, 0.42f);
    private Color selectedColour = new Color(0.70f, 0.51f, 0.32f, 1f);

    public System.Action<Slot> OnClicked;

    void Awake()
    {
        if (background == null) background = GetComponent<Image>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColour : normalColour;
    }
}
