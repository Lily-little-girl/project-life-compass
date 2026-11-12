using UnityEngine;
using System.Collections;

public class CutsceneActor : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float arriveDistance = 0.05f;

    [Header("Animation (ว่าง = หาจากลูกให้เอง)")]
    public Animator animator;
    public string walkingParameter = "isWalking";
    public SpriteRenderer spriteRenderer;

    [Header("Facing")]
    public FlipMethod flipMethod = FlipMethod.SpriteRendererFlipX;
    public bool spriteFacesRightByDefault = false;

    [Header("Control (ใช้กับผู้เล่น)")]
    public MonoBehaviour[] controlScripts;   // สคริปต์เดินที่ต้องปิดระหว่างฉาก เช่น TopDownPlayerMovement

    private Rigidbody2D rb;
    private RigidbodyType2D originalBodyType;
    private bool refsReady;
    private int walkingID;

    void EnsureRefs()
    {
        if (refsReady) return;
        refsReady = true;

        walkingID = Animator.StringToHash(walkingParameter);

        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        rb = GetComponent<Rigidbody2D>();
        if (rb != null) originalBodyType = rb.bodyType;
    }

    void SetWalking(bool walking)
    {
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
        {
            animator.SetBool(walkingID, walking);
        }
    }

    public void Face(bool faceRight)
    {
        EnsureRefs();

        bool flip = faceRight != spriteFacesRightByDefault;

        if (flipMethod == FlipMethod.SpriteRendererFlipX)
        {
            if (spriteRenderer != null) spriteRenderer.flipX = flip;
        }
        else
        {
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (flip ? -1f : 1f);
            transform.localScale = s;
        }
    }

    public void TeleportTo(Vector2 position)
    {
        EnsureRefs();
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        SetWalking(false);
    }

    public IEnumerator MoveTo(Vector2 destination, float speedOverride)
    {
        EnsureRefs();

        float speed = speedOverride > 0f ? speedOverride : moveSpeed;

        SetWalking(true);

        while (Vector2.Distance(transform.position, destination) > arriveDistance)
        {
            Vector2 pos = transform.position;

            float dx = destination.x - pos.x;
            if (Mathf.Abs(dx) > 0.01f) Face(dx > 0f);

            pos = Vector2.MoveTowards(pos, destination, speed * Time.deltaTime);
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);

            yield return null;
        }

        transform.position = new Vector3(destination.x, destination.y, transform.position.z);
        SetWalking(false);
    }

    public IEnumerator FadeTo(float targetAlpha, float duration)
    {
        EnsureRefs();
        if (spriteRenderer == null) yield break;

        Color c = spriteRenderer.color;
        float startAlpha = c.a;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(t / duration));
            spriteRenderer.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        spriteRenderer.color = c;
    }

    // ปิด = ให้ฉากคุมตัวละคร (ปิดสคริปต์เดิน ไม่ให้ชนกำแพง) / เปิด = คืนการควบคุม
    public void SetControl(bool on)
    {
        EnsureRefs();

        if (controlScripts != null)
        {
            foreach (MonoBehaviour s in controlScripts)
            {
                if (s != null) s.enabled = on;
            }
        }

        if (rb != null)
        {
            if (!on)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                rb.bodyType = originalBodyType;
            }
        }

        if (!on) SetWalking(false);
    }
}