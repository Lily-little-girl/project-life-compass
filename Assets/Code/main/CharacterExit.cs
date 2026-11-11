using UnityEngine;
using System;
using System.Collections;

public enum FlipMethod { SpriteRendererFlipX, ScaleX }

public class CharacterExit : MonoBehaviour
{
    [Header("Character")]
    public Transform character;      // ว่าง = ใช้ object ที่สคริปต์นี้อยู่
    public Transform exitPosition;
    public Animator animator;
    public string walkingParameter = "isWalking";
    public float moveSpeed = 2f;

    [Header("Facing")]
    public bool turnToFaceExit = true;            // กลับหน้าไปทางจุดหมายก่อนเดิน
    public FlipMethod flipMethod = FlipMethod.SpriteRendererFlipX;
    public bool spriteFacesRightByDefault = false; // ภาพต้นฉบับวาดให้หันขวาไหม (หันซ้าย = ไม่ติ๊ก)

    [Header("After Exit")]
    public bool hideWhenArrived = true;

    private bool started = false;

    public void WalkOut(Action onFinished)
    {
        Transform who = character != null ? character : transform;

        if (started || exitPosition == null)
        {
            onFinished?.Invoke();
            return;
        }

        started = true;
        StartCoroutine(WalkOutRoutine(who, onFinished));
    }

    private void FaceTowards(Transform who, bool faceRight)
    {
        // ถ้าหันขวาตามที่ภาพต้นฉบับวาดไว้ = ไม่ต้องกลับ
        bool flip = faceRight != spriteFacesRightByDefault;

        if (flipMethod == FlipMethod.SpriteRendererFlipX)
        {
            SpriteRenderer sr = who.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.flipX = flip;
        }
        else
        {
            Vector3 s = who.localScale;
            s.x = Mathf.Abs(s.x) * (flip ? -1f : 1f);
            who.localScale = s;
        }
    }

    private IEnumerator WalkOutRoutine(Transform who, Action onFinished)
    {
        int walkingID = Animator.StringToHash(walkingParameter);
        bool canAnimate = animator != null && animator.runtimeAnimatorController != null;

        if (turnToFaceExit)
        {
            bool faceRight = exitPosition.position.x >= who.position.x;
            FaceTowards(who, faceRight);
        }

        if (canAnimate) animator.SetBool(walkingID, true);

        while (Vector2.Distance(who.position, exitPosition.position) > 0.05f)
        {
            who.position = Vector2.MoveTowards(
                who.position,
                exitPosition.position,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        who.position = exitPosition.position;

        if (canAnimate) animator.SetBool(walkingID, false);

        if (hideWhenArrived) who.gameObject.SetActive(false);

        onFinished?.Invoke();
    }
}