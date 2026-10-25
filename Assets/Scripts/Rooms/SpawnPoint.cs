using UnityEngine;

// where the player shows up when coming in from another room
public class SpawnPoint : MonoBehaviour
{
    public string id;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
