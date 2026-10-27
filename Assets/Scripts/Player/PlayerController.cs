using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;

    // set to false when dialogue / journal / minigames are open
    public bool canMove = true;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Vector2 input;

    public Vector2 Facing { get; private set; } = Vector2.down;
    public bool IsMoving => input.sqrMagnitude > 0.01f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Update()
    {
        bool allowed = canMove && !PauseController.IsGamePaused ;
        
        input = allowed ? ReadInput() : Vector2.zero;

        if (IsMoving)
        {
            Facing = input.normalized;
            if (sprite != null && Mathf.Abs(input.x) > 0.01f)
                sprite.flipX = input.x < 0f;
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * moveSpeed;
    }

    private Vector2 ReadInput()
    {
        Vector2 v = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1;
        }

        var pad = Gamepad.current;
        if (pad != null && v == Vector2.zero)
            v = pad.leftStick.ReadValue();

        // so diagonals aren't faster
        return Vector2.ClampMagnitude(v, 1f);
    }
}
