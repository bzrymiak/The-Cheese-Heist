using UnityEngine;

public class DoorUnlockZone : MonoBehaviour
{
    [Tooltip("Must match the Door Id of the LockedDoor you want to unlock.")]
    [SerializeField] private string targetDoorId;

    [Tooltip("On: needs the key flag. Off: unlocks regardless.")]
    [SerializeField] private bool requireKey = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        foreach (LockedDoor door in FindObjectsByType<LockedDoor>(FindObjectsSortMode.None))
        {
            if (door.DoorId != targetDoorId) continue;

            if (requireKey) door.TryUnlock();
            else door.Unlock();
        }
    }
}