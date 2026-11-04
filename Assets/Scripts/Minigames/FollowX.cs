using UnityEngine;

// keeps the robber's shadow under him when he moves across the screen
public class FollowX : MonoBehaviour
{
    public RectTransform target;
    RectTransform rt;

    void Awake() { rt = (RectTransform)transform; }

    void LateUpdate()
    {
        if (target == null) return;
        rt.anchoredPosition = new Vector2(target.anchoredPosition.x, rt.anchoredPosition.y);
    }
}
