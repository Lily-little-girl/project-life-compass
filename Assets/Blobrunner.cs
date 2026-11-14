using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// ใส่สคริปต์นี้ที่ blob_0
/// รับชุดคำสั่ง w/a/s/d แล้วเดินทีละ 1 บล็อก โดยหันหน้าตามทิศนั้นก่อนเดินทุกก้าว
/// ถ้าก้าวต่อไปจะออกนอก Play Area จะข้ามก้าวนั้น แล้วทำคำสั่งถัดไปต่อ (ไม่หยุดเกม)
public class BlobRunner : MonoBehaviour
{
    public enum FacingMode { Rotate, FlipX }

    [Header("Movement")]
    [SerializeField] private float cellSize = 1f;     // ขนาด 1 บล็อก (หน่วย Unity)
    [SerializeField] private float stepTime = 0.3f;   // เวลาเดิน 1 บล็อก (วินาที)

    [Header("Boundary")]
    [SerializeField] private Renderer playArea;          // ลาก PlayArea (ที่มี SpriteRenderer) มาใส่
    [SerializeField] private Collider2D playAreaCollider; // หรือใช้ BoxCollider2D แทนก็ได้
    [SerializeField] private float padding = 0.3f;        // เว้นจากขอบเข้ามา (ประมาณครึ่งตัว blob)
    [SerializeField] private bool waitWhenBlocked = true; // ชนแล้วรอเท่าเวลา 1 ก้าว จังหวะจะได้เท่าเดิม

    [Header("Facing")]
    [SerializeField] private FacingMode facingMode = FacingMode.Rotate;
    [SerializeField] private float rotationOffset = 0f; // โหมด Rotate: ถ้า sprite ปกติหันขวา = 0
    [SerializeField] private bool invertFlip = false;   // โหมด FlipX: ติ๊กถ้า sprite ปกติหันซ้าย

    public bool IsRunning { get; private set; }
    public bool Frozen { get; private set; }

    public void Halt()
    {
        Frozen = true;
        StopAllCoroutines();
        IsRunning = false;
    }
    private SpriteRenderer sr;

    private void Awake() => sr = GetComponent<SpriteRenderer>();

    public void Run(IList<char> commands, Action onFinished = null)
    {
        if (Frozen || IsRunning || commands == null || commands.Count == 0) return;
        StartCoroutine(RunRoutine(commands, onFinished));
    }

    private IEnumerator RunRoutine(IList<char> commands, Action onFinished)
    {
        IsRunning = true;

        foreach (char k in commands)
        {
            Vector3 dir = ToDirection(k);
            Face(dir);                                    // หันก่อน

            Vector3 start = transform.position;
            Vector3 end = start + dir * cellSize;         // แล้วเดิน 1 บล็อก

            // ชนขอบ -> ข้ามก้าวนี้ ไปคำสั่งถัดไป
            if (!IsInsideArea(end) || IsBlocked(end))

            {
                if (waitWhenBlocked) yield return new WaitForSeconds(stepTime);
                continue;
            }

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / stepTime;
                transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }
            transform.position = end;
        }

        IsRunning = false;
        onFinished?.Invoke();
    }

    private bool IsInsideArea(Vector3 p)
    {
        Bounds b;
        if (playAreaCollider != null) b = playAreaCollider.bounds;
        else if (playArea != null) b = playArea.bounds;
        else return true; // ไม่ได้ตั้งค่า = ไม่จำกัด

        return p.x >= b.min.x + padding && p.x <= b.max.x - padding &&
               p.y >= b.min.y + padding && p.y <= b.max.y - padding;
    }

    private void Face(Vector3 dir)
    {
        if (facingMode == FacingMode.Rotate)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + rotationOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else if (sr != null && dir.x != 0f)               // ขึ้น/ลง คงทิศเดิมไว้
        {
            sr.flipX = (dir.x < 0f) != invertFlip;
        }
    }

    private static Vector3 ToDirection(char k)
    {
        switch (k)
        {
            case 'w': return Vector3.up;
            case 's': return Vector3.down;
            case 'a': return Vector3.left;
            default: return Vector3.right; // 'd'
        }
    }
    private static bool IsBlocked(Vector3 p)
    {
        foreach (Collider2D c in Physics2D.OverlapPointAll(p))
            if (c.GetComponent<Obstacle>() != null) return true;
        return false;
    }
}