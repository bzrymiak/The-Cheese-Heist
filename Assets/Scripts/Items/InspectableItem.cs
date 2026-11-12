using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// For things the player can look at but not pick up (paintings, notes on a wall, etc.).
// Press I within range to inspect. Show a popup, fire events (e.g. start dialogue), or both.
public class InspectableItem : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float inspectRadius = 1.5f;

    [Tooltip("Optional 'Press I' hint. Shown only while the player is in range.")]
    [SerializeField] private GameObject tooltip;

    [Header("Popup (optional)")]
    [Tooltip("A UI panel to show when inspected. Leave empty if this item only triggers dialogue/events.")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private string popupId;
    [SerializeField] private Sprite inspectImage;
    [SerializeField] private bool closeWithInspectKey = true;
    [SerializeField] private bool showPanel = true;
    

    [Tooltip("Pause the game while the popup is open.")]
    [SerializeField] private bool pauseWhilePopupOpen = true;

    [Header("Behavior")]
    [Tooltip("Optional. Sets a global flag when inspected (e.g. so a door can require it).")]
    [SerializeField] private string flagId;

    [Tooltip("Only the first inspection triggers anything.")]
    [SerializeField] private bool onlyOnce = false;

    [Header("Events")]
    [Tooltip("Hook up dialogue (NPC.StartDialogue), sounds, etc.")]
    public UnityEvent onInspected;

    private Transform player;

    // everything you can press I on, so only the closest one offers itself
    public static readonly System.Collections.Generic.List<InspectableItem> All = new System.Collections.Generic.List<InspectableItem>();
    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public bool Offers(Vector2 from, out float dist)
    {
        dist = Vector2.Distance(from, transform.position);
        return enabled && !(onlyOnce && hasInspected) && dist <= inspectRadius;
    }
    private bool popupOpen;
    private bool hasInspected;

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        if (popupPanel == null && !string.IsNullOrEmpty(popupId))
        {
            popupPanel = PopupRegistry.Get(popupId);
            if (popupPanel == null)
                Debug.LogWarning($"[Inspectable] No popup found for id '{popupId}'. Start from the first scene, or check the registry.");
        }

        if (popupPanel != null) popupPanel.SetActive(false);
        if (tooltip != null) tooltip.SetActive(false);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        
        if (popupOpen)
        {
            if ((closeWithInspectKey && kb.iKey.wasPressedThisFrame) || kb.escapeKey.wasPressedThisFrame)
                ClosePopup();
            return;
        }

        if (player == null) return;

        bool inRange = Vector2.Distance(player.position, transform.position) <= inspectRadius;
        if (inRange && InspectSpot.SomethingCloser(player.position, Vector2.Distance(player.position, transform.position), this)) inRange = false;

        if (tooltip != null)
            tooltip.SetActive(inRange && !PauseController.IsGamePaused && !(onlyOnce && hasInspected));

        if (!inRange) return;
        if (PauseController.IsGamePaused) return;
        if (!kb.iKey.wasPressedThisFrame) return;

        Inspect();
    }

    GameObject GetPopup()
    {
        if (popupPanel == null && !string.IsNullOrEmpty(popupId))
            popupPanel = PopupRegistry.Get(popupId);

        if (popupPanel == null)
            Debug.LogWarning($"[Inspectable] '{name}': no popup. popupId='{popupId}', registryExists={PopupRegistry.Instance != null}");

        return popupPanel;
    }

    public void Inspect()
    {
        if (onlyOnce && hasInspected) return;
        hasInspected = true;

        if (!string.IsNullOrEmpty(flagId))
            GameFlags.Set(flagId);

        if (showPanel && GetPopup() != null)
            OpenPopup();

        onInspected.Invoke();
    }

    void OpenPopup()
    {
        popupOpen = true;

        if (popupPanel.TryGetComponent(out InspectPanelView view))
            view.SetImage(inspectImage);

        popupPanel.SetActive(true);
        PopupRegistry.CloseRequested += ClosePopup;
        if (tooltip != null) tooltip.SetActive(false);
        if (pauseWhilePopupOpen) PauseController.SetPause(true);
    }

    public void ClosePopup()
    {
        if (!popupOpen) return;
        popupOpen = false;
        PopupRegistry.CloseRequested -= ClosePopup;
        popupPanel.SetActive(false);
        if (pauseWhilePopupOpen) PauseController.SetPause(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, inspectRadius);
    }

    void OnDestroy()
    {
        if (popupOpen) ClosePopup();
    }
}