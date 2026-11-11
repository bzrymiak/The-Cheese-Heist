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

    void FitToRoom()
    {
        var cam = GetComponent<Camera>();
        if (cam == null || !cam.orthographic) return;

        bool any = false;
        Bounds room = new Bounds();
        foreach (var r in FindObjectsByType<UnityEngine.Tilemaps.TilemapRenderer>(FindObjectsSortMode.None))
        {
            if (r.gameObject.scene != gameObject.scene) continue;
            if (!any) { room = r.bounds; any = true; } else room.Encapsulate(r.bounds);
        }
        if (!any) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 here = transform.position;
        minBounds.x = maxBounds.x = here.x;
        minBounds.y = maxBounds.y = here.y;
        if (room.size.x > halfW * 2f + 0.1f) { minBounds.x = room.min.x + halfW; maxBounds.x = room.max.x - halfW; }
        if (room.size.y > halfH * 2f + 0.1f) { minBounds.y = room.min.y + halfH; maxBounds.y = room.max.y - halfH; }
        useBounds = true;
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
        // the rooms never had the player dragged into Target, so go find them.
        // only scroll along an axis where the room is bigger than the screen, otherwise keep the framing it has
        if (target == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p == null) return;
            target = p.transform;
            if (!useBounds) FitToRoom();
            SnapToTarget();
        }

        Vector3 goal = Clamp(new Vector3(target.position.x, target.position.y, transform.position.z));

        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}
