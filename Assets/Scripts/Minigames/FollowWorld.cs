using UnityEngine;

// the trap layer lives outside the scrolling world (so it can draw over the darkness)
// but still has to slide along with it
public class FollowWorld : MonoBehaviour
{
    public RectTransform world;
    RectTransform rt;

    void Awake() { rt = (RectTransform)transform; }

    void LateUpdate()
    {
        if (world != null) rt.anchoredPosition = world.anchoredPosition;
    }
}
