using UnityEngine;

// walk into this and you go to the other room
[RequireComponent(typeof(BoxCollider2D))]
public class RoomDoor : MonoBehaviour
{
    public string targetScene;
    public string targetSpawn;

    void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        RoomTransition.Go(targetScene, targetSpawn);
    }

    void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box == null) return;
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
        Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
    }
}
