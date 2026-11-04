using UnityEngine;
using UnityEngine.Events;

public class ItemPickupTrigger : MonoBehaviour
{
    public string flagId;
    public UnityEvent onPickedUp;

    public void Fire()
    {
        if (!string.IsNullOrEmpty(flagId))
            GameFlags.Set(flagId);

        onPickedUp.Invoke();
    }
}