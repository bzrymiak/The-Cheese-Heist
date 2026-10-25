using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    [SerializeField] private float smoothTime = 0.15f;

    // turn on to keep the camera inside the map
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    private Vector3 velocity;

    // jump straight to the player, used after switching rooms
    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = Clamp(new Vector3(target.position.x, target.position.y, transform.position.z));
        velocity = Vector3.zero;
    }

    Vector3 Clamp(Vector3 p)
    {
        if (!useBounds) return p;
        p.x = Mathf.Clamp(p.x, minBounds.x, maxBounds.x);
        p.y = Mathf.Clamp(p.y, minBounds.y, maxBounds.y);
        return p;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 goal = Clamp(new Vector3(target.position.x, target.position.y, transform.position.z));

        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}
