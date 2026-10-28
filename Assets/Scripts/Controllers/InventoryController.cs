using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryController : MonoBehaviour
{
    public GameObject inventoryPanel;
    public GameObject slotPrefab;
    public int slotCount;
    public GameObject[] itemPrefabs;

     [Header("Details Panel")]
    public GameObject detailsPanel;
    public Image detailImage;
    public TMP_Text detailTitle;
    public TMP_Text detailDescription;
    public Button viewMoreButton;

    [Header("Detail View")]
    public GameObject closeUpPanel;
    public Image closeUpImage;

    private Slot selectedSlot;
    private Sprite currentCloseUp;
    
    void Start()
    {
        for (int i = 0; i < slotCount; i++)
        {
            Slot slot = Instantiate(slotPrefab, inventoryPanel.transform).GetComponent<Slot>();
            slot.OnClicked = SelectSlot;

            if (i < itemPrefabs.Length)
            {
                GameObject item = Instantiate (itemPrefabs[i], slot.transform);
                item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                slot.currentItem = item;
            }
        }

        viewMoreButton.onClick.AddListener(ShowCloseUp);
        ClearDetails();
    }

    void SelectSlot(Slot slot)
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);

        // clicking empty slot or deselecting
        if (slot.currentItem == null || slot == selectedSlot)
        {
            selectedSlot = null;
            ClearDetails();
            return;
        } 

        selectedSlot = slot;
        selectedSlot.SetSelected(true);
        ShowDetails(slot.currentItem);
    }

    void ShowDetails(GameObject itemObject)
    {
        Item item = itemObject.GetComponent<Item>();
        if (item == null) { ClearDetails(); return; }

        detailsPanel.SetActive(true);
        detailImage.sprite = itemObject.GetComponent<Image>().sprite;
        detailImage.preserveAspect = true;
        detailTitle.SetText(item.title);
        detailDescription.SetText(item.description);

        currentCloseUp = item.detailedView;
        viewMoreButton.gameObject.SetActive(currentCloseUp != null);
    }

    void ClearDetails()
    {
        detailsPanel.SetActive(false);
        viewMoreButton.gameObject.SetActive(false);
        closeUpPanel.SetActive(false);
        currentCloseUp = null;
    }

    void ShowCloseUp()
    {
        if (currentCloseUp == null) return;
        closeUpImage.sprite = currentCloseUp;
        closeUpImage.preserveAspect = true;
        closeUpPanel.SetActive(true);
    }

    public bool AddItem(GameObject itemPrefab)
    {
        // look for empty slot
        foreach(Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot != null && slot.currentItem == null)
            {
                GameObject newItem = Instantiate(itemPrefab, slot.transform);
                newItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                slot.currentItem = newItem;
                return true;
            }
        }

        Debug.Log("Inventory full");
        return false;
    }
}
