using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class LockedDoor : MonoBehaviour
{
    [Header("Requirements")]
    [Tooltip("Flag the player must have (set by the key's ItemPickupTrigger Flag Id). Leave empty for no requirement.")]
    public string requiredFlag;

    [Tooltip("Remembers this door was unlocked when you leave and return to the scene.")]
    public string doorId;

    [Header("Interaction")]
    public float unlockRadius = 1.5f;

    [Tooltip("Scene transition doorway, active on unlocked")]
    public GameObject blocker;

    [Tooltip("Unlocked sprite")]
    public Sprite unlockedSprite;
    public SpriteRenderer doorRenderer;

    [Header("Events")]
    public UnityEvent onUnlocked;      // unlock sound
    public UnityEvent onUnlockFailed;  // rattle sound

    public bool IsLocked { get; private set; } = true;

    private Transform player;

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
        if (IsLocked && blocker != null) blocker.SetActive(false);

        // already unlocked earlier (e.g. before a scene change)
        if (!string.IsNullOrEmpty(doorId) && GameFlags.Has(doorId))
            Unlock();
    }

    void Update()
    {
        if (!IsLocked || player == null) return;
        if (PauseController.IsGamePaused) return;

        var kb = Keyboard.current;
        if (kb == null || !kb.uKey.wasPressedThisFrame) return;

        float dist = Vector2.Distance(player.position, transform.position);
        if (dist > unlockRadius) return;

        bool hasKey = string.IsNullOrEmpty(requiredFlag) || GameFlags.Has(requiredFlag);
        if (hasKey) Unlock();
        else onUnlockFailed.Invoke();
    }

    public void Unlock()
    {
        if (!IsLocked) return;
        IsLocked = false;

        if (!string.IsNullOrEmpty(doorId)) GameFlags.Set(doorId);
        if (blocker != null) blocker.SetActive(true);
        if (doorRenderer != null && unlockedSprite != null)
            doorRenderer.sprite = unlockedSprite;

        onUnlocked.Invoke();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, unlockRadius);
    }
}