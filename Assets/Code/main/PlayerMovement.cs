using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 4f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;

    private float moveX;

    // เพิ่มตัวแปรควบคุมการเดิน
    private bool canMove = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // ถ้าถูกล็อกการเดิน ให้หยุดรับคำสั่ง
        if (!canMove)
        {
            moveX = 0;
            animator.SetBool("isWalking", false);
            return;
        }

        // รับปุ่ม A, D และลูกศรซ้าย-ขวา
        moveX = 0;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed)
            {
                moveX = -1;
            }

            if (Keyboard.current.dKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed)
            {
                moveX = 1;
            }
        }

        // หันซ้าย-ขวา
        if (moveX < 0)
            sr.flipX = false;
        else if (moveX > 0)
            sr.flipX = true;

        // Animation
        animator.SetBool("isWalking", moveX != 0);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveX * moveSpeed, 0);
    }

    // เปิด-ปิดการควบคุมตัวละคร
    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;

        if (!enabled)
        {
            moveX = 0;
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("isWalking", false);
        }
    }
}