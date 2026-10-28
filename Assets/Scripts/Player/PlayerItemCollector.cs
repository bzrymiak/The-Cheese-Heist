using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerItemCollector : MonoBehaviour
{
    [SerializeField] private float pickupRadius = 1.2f;

    private InventoryController inventoryController;

    void Start()
    {
        inventoryController = FindFirstObjectByType<InventoryController>();
    }

    void Update()
    {
        if (PauseController.IsGamePaused) return; 

        var kb = Keyboard.current;
        if (kb == null || !kb.eKey.wasPressedThisFrame) return;

        TryCollectNearest();
    }

    void TryCollectNearest()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius);

        Collider2D nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Item")) continue;

            float dist = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = hit;
            }
        }

        if (nearest == null) return;

        Item item = nearest.GetComponent<Item>();
        if (item == null) return;

        if (inventoryController.AddItem(nearest.gameObject))
        {
            Destroy(nearest.gameObject);
        }
    }

    // shows the pickup range in the scene view when the player is selected
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}