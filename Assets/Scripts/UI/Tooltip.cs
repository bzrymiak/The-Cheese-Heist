using UnityEngine;

public class Tooltip : MonoBehaviour
{
    public float distanceRadius = 1.5f;
    public GameObject tooltip;

    void Start()
    {
        tooltip.SetActive(false);
    }

    void Update()
    {
        bool playerInRange = false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, distanceRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                playerInRange = true;
                break;
            }
        }

        if (tooltip.activeSelf != playerInRange)
            tooltip.SetActive(playerInRange);
        }
}
