using UnityEngine;

// fake walk cycle until we get real frames. bounces + squashes the sprite a bit while moving
public class WalkBob : MonoBehaviour
{
    public Transform visual;
    [SerializeField] private float bobHeight = 0.06f;
    [SerializeField] private float bobSpeed = 14f;
    [SerializeField] private float squash = 0.05f;

    private PlayerController player;
    private Vector3 startPos;
    private Vector3 startScale;
    private float t;

    void Start()
    {
        player = GetComponent<PlayerController>();
        if (visual == null) visual = transform.Find("Visual");
        if (visual != null)
        {
            startPos = visual.localPosition;
            startScale = visual.localScale;
        }
    }

    void Update()
    {
        if (visual == null || player == null) return;

        if (player.IsMoving)
        {
            t += Time.deltaTime * bobSpeed;
            float step = Mathf.Abs(Mathf.Sin(t));
            visual.localPosition = startPos + Vector3.up * step * bobHeight;
            // a little squash when the foot lands
            float s = (1f - step) * squash;
            visual.localScale = new Vector3(startScale.x * (1f + s), startScale.y * (1f - s), startScale.z);
        }
        else
        {
            t = 0f;
            visual.localPosition = Vector3.Lerp(visual.localPosition, startPos, Time.deltaTime * 20f);
            visual.localScale = Vector3.Lerp(visual.localScale, startScale, Time.deltaTime * 20f);
        }
    }
}
