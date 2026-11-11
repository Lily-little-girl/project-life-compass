using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownPlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Animation (ไม่บังคับ)")]
    public Animator animator;
    public string walkingParameter = "isWalking";

    [Header("Facing (ไม่บังคับ)")]
    public SpriteRenderer spriteRenderer;
    public bool spriteFacesRightByDefault = false; // ท่ายืนปกติหันซ้าย = ไม่ติ๊ก

    private Rigidbody2D rb;
    private InputAction moveAction;
    private Vector2 input;
    private bool canMove = true;
    private int walkingID;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        walkingID = Animator.StringToHash(walkingParameter);

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");
    }

    void OnEnable()  { moveAction.Enable(); }
    void OnDisable() { moveAction.Disable(); }

    void Update()
    {
        input = canMove ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        input = Vector2.ClampMagnitude(input, 1f); // เดินเฉียงไม่เร็วกว่าเดินตรง

        bool walking = input.sqrMagnitude > 0.01f;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool(walkingID, walking);
        }

        if (spriteRenderer != null && Mathf.Abs(input.x) > 0.01f)
        {
            bool faceRight = input.x > 0f;
            spriteRenderer.flipX = faceRight != spriteFacesRightByDefault;
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * moveSpeed;
    }

    // ชื่อเดียวกับสคริปต์เดิม เผื่อเรียกจากระบบอื่นเช่นบทสนทนา
    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;
        if (!enabled) rb.linearVelocity = Vector2.zero;
    }
}