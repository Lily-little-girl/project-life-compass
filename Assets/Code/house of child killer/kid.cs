using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class Kid : MonoBehaviour
{
    public static event System.Action<Kid, Hazard> OnAnyKidHurt;
    public static readonly List<Kid> All = new List<Kid>();   // เด็กที่ active อยู่ตอนนี้

    [Header("Hazards (ของอันตรายเฉพาะเด็กคนนี้)")]
    public List<Hazard> myHazards = new List<Hazard>();

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float arriveDistance = 0.1f;

    [Header("Danger")]
    public float dangerSeconds = 4f;
    public UnityEvent onHurt;

    [Header("After Bounce")]
    public float pauseAfterLand = 1f;

    [Header("Visual")]
    public Transform visual;

    [Header("Animation")]
    public Animator animator;
    public string walkingParameter = "isWalking";
    public SpriteRenderer spriteRenderer;
    public bool spriteFacesRightByDefault = false;

    [Header("Runner (เด็กที่โผล่มาเป็นช่วงเวลา)")]
    public bool isRunner = false;
    public Transform spawnPoint;              // ประตู: จุดโผล่ และจุดที่เด้งกลับไป
    public float fadeOutTime = 1f;            // ค่อยๆ จางหายหลังเด้งกลับ
    public float fadeDelay = 0.3f;            // ยืนที่ประตูสักครู่ก่อนเริ่มจาง
    public float returnBounceDuration = 0.9f; // เด้งกลับประตูนานแค่ไหน
    public float returnBounceHeight = 1.5f;   // เด้งสูงแค่ไหน

    private Hazard current;
    private bool airborne;
    private bool hurt;
    private float dangerTimer;
    private int walkingID;
    private Vector3 visualStartScale = Vector3.one;
    private Vector2 doorPosition;
    private bool hasDoor;

    public bool IsAirborne => airborne;

    void Awake()
    {
        walkingID = Animator.StringToHash(walkingParameter);

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (visual != null) visualStartScale = visual.localScale;
    }

    void OnEnable()  { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        PickNewHazard();
    }

    void Update()
    {
        // เกมจบแล้ว (แพ้หรือชนะ) ทุกคนยืนนิ่ง
        if (GameOverManager.IsGameOver || CutsceneSequence. IsPlaying)
        {
            SetWalking(false);
            return;
        }

        // ตอนกระเด้ง, หยุดงงหลังตกลงมา, เจ็บแล้ว หรือไม่มีของอันตราย = ยืนนิ่ง
        if (airborne || hurt || current == null)
        {
            SetWalking(false);
            return;
        }

        Vector2 pos = transform.position;
        Vector2 target = current.StandPosition;

        if (Vector2.Distance(pos, target) > arriveDistance)
        {
            // กำลังเดินไปหาของอันตราย
            SetWalking(true);
            FaceDirection(target.x - pos.x);

            transform.position = Vector2.MoveTowards(pos, target, moveSpeed * Time.deltaTime);
        }
        else
        {
            // ถึงแล้ว ยืนนิ่งอยู่ติดของ แล้วนับเวลาอันตราย
            SetWalking(false);

            dangerTimer += Time.deltaTime;

            if (dangerTimer >= dangerSeconds)
            {
                hurt = true;
                onHurt?.Invoke();
                OnAnyKidHurt?.Invoke(this, current);
            }
        }
    }

    void SetWalking(bool walking)
    {
        if (animator != null && animator.enabled && animator.runtimeAnimatorController != null)
        {
            animator.SetBool(walkingID, walking);
        }
    }

    void FaceDirection(float dx)
    {
        if (spriteRenderer == null || Mathf.Abs(dx) < 0.01f) return;

        bool faceRight = dx > 0f;
        spriteRenderer.flipX = faceRight != spriteFacesRightByDefault;
    }

    void PickNewHazard()
    {
        dangerTimer = 0f;

        // เก็บเฉพาะของที่ใส่ไว้จริงในลิสต์ของเด็กคนนี้
        List<Hazard> options = new List<Hazard>();
        foreach (Hazard h in myHazards)
        {
            if (h != null && h != current) options.Add(h);
        }

        if (options.Count == 0)
        {
            // มีของแค่ชิ้นเดียว ก็ไปชิ้นเดิม
            if (current == null && myHazards.Count > 0) current = myHazards[0];
            return;
        }

        current = options[Random.Range(0, options.Count)];
    }

    public void BounceTo(Vector2 landing, float duration, float height)
    {
        if (airborne || hurt) return;

        if (isRunner)
        {
            if (hasDoor) landing = doorPosition;
            else if (spawnPoint != null) landing = spawnPoint.position;
            else Debug.LogWarning("Runner Kid: ยังไม่ได้ใส่ Spawn Point เด็กเลยเด้งไปจุดลงทั่วไปแทน");

            duration = returnBounceDuration;
            height = returnBounceHeight;
        }

        StartCoroutine(BounceRoutine(landing, duration, height));
    }

    private IEnumerator BounceRoutine(Vector2 landing, float duration, float height)
    {
        airborne = true;
        SetWalking(false);

        Vector2 start = transform.position;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);

            transform.position = Vector2.Lerp(start, landing, p);

            if (visual != null)
            {
                visual.localPosition = new Vector3(0f, Mathf.Sin(p * Mathf.PI) * height, 0f);
            }

            yield return null;
        }

        transform.position = landing;
        if (visual != null) visual.localPosition = Vector3.zero;

        // เด็กวิ่งเร็ว: ถึงประตูแล้วค่อยๆ จางหาย
        if (isRunner)
        {
            yield return FadeOutAndHide();
            yield break;
        }

        // ยืนนิ่งช่วง Pause After Land (airborne ยังเป็น true อยู่ เลยไม่เดิน)
        yield return new WaitForSeconds(pauseAfterLand);

        PickNewHazard();
        airborne = false;
    }

    // ใช้กับเอฟเฟกต์ของ Hazard
    public void HideVisual()
    {
        if (visual != null) visual.gameObject.SetActive(false);
        else if (spriteRenderer != null) spriteRenderer.enabled = false;
    }

    public void FreezeAnimation()
    {
        SetWalking(false);
        if (animator != null) animator.enabled = false;
    }

    // เรียกจาก TimedKidSpawner ให้เด็กโผล่ที่ประตูแล้ววิ่ง
    public void Spawn()
    {
        gameObject.SetActive(true);   // ต้องเปิดก่อน เพื่อให้ Awake ทำงาน (ถ้าเพิ่งเปิดครั้งแรก)

        StopAllCoroutines();

        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            doorPosition = spawnPoint.position;   // จำตำแหน่งประตูไว้ตายตัว
            hasDoor = true;
        }

        airborne = false;
        hurt = false;
        dangerTimer = 0f;
        current = null;
        PickNewHazard();

        if (visual != null)
        {
            visual.gameObject.SetActive(true);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = visualStartScale;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        if (animator != null) animator.enabled = true;
    }

    private IEnumerator FadeOutAndHide()
    {
        if (fadeDelay > 0f) yield return new WaitForSeconds(fadeDelay);

        Color start = spriteRenderer != null ? spriteRenderer.color : Color.white;
        float t = 0f;

        while (t < fadeOutTime)
        {
            t += Time.deltaTime;

            if (spriteRenderer != null)
            {
                Color c = start;
                c.a = Mathf.Lerp(start.a, 0f, Mathf.Clamp01(t / fadeOutTime));
                spriteRenderer.color = c;
            }

            yield return null;
        }

        gameObject.SetActive(false);   // หายไปจนกว่าจะโผล่รอบหน้า
    }
}