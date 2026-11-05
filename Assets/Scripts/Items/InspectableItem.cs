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
        
        // popup is open: I or Escape closes it (checked before the pause guard,
        // since the popup itself may have paused the game)
        if (popupOpen)
        {
            if (kb.iKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
                ClosePopup();
            return;
        }

        if (player == null) return;

        bool inRange = Vector2.Distance(player.position, transform.position) <= inspectRadius;

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

        if (GetPopup() != null)
            OpenPopup();

        onInspected.Invoke();
    }

    void OpenPopup()
    {
        popupOpen = true;
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