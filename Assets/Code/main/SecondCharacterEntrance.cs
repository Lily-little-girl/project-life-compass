using UnityEngine;
using System;
using System.Collections;

public class SecondCharacterEntrance : MonoBehaviour
{
    [Header("Character")]
    public Transform character;
    public Transform targetPosition;   // จุดที่เดินเข้ามาตอนแรก
    public Animator animator;
    public float moveSpeed = 2f;

    [Header("Portrait")]
    public Sprite portraitSprite;

    [Header("Facing")]
    public FlipMethod flipMethod = FlipMethod.SpriteRendererFlipX;
    public bool spriteFacesRightByDefault = false;  // ท่ายืนปกติหันซ้าย = ไม่ติ๊ก
    public bool returnToDefaultFacing = true;       // ถึงจุดหมายแล้วกลับไปหันทิศเดิม

    private bool started = false;

    // เดินเข้ามาครั้งแรก (ทำได้ครั้งเดียว)
    public void WalkIn(Action onArrived)
    {
        if (started || character == null || targetPosition == null)
        {
            onArrived?.Invoke();
            return;
        }

        started = true;
        StartCoroutine(MoveRoutine(targetPosition, onArrived));
    }

    // เดินไปจุดไหนก็ได้ (เรียกกี่ครั้งก็ได้)
    public void WalkTo(Transform destination, Action onArrived)
    {
        if (character == null || destination == null)
        {
            onArrived?.Invoke();
            return;
        }

        StartCoroutine(MoveRoutine(destination, onArrived));
    }

    private void FaceTowards(bool faceRight)
    {
        // หันตามทิศที่ภาพต้นฉบับวาดไว้ = ไม่ต้องกลับ
        bool flip = faceRight != spriteFacesRightByDefault;

        if (flipMethod == FlipMethod.SpriteRendererFlipX)
        {
            SpriteRenderer sr = character.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.flipX = flip;
        }
        else
        {
            Vector3 s = character.localScale;
            s.x = Mathf.Abs(s.x) * (flip ? -1f : 1f);
            character.localScale = s;
        }
    }

    private IEnumerator MoveRoutine(Transform destination, Action onArrived)
    {
        int walkingID = Animator.StringToHash("isWalking");
        bool canAnimate = animator != null && animator.runtimeAnimatorController != null;

        // หันตามทิศที่จะเดิน (ถ้าแทบไม่ได้ขยับแนวนอนจะไม่เปลี่ยนทิศ)
        float dx = destination.position.x - character.position.x;
        if (Mathf.Abs(dx) > 0.01f)
        {
            FaceTowards(dx > 0f);
        }

        if (canAnimate) animator.SetBool(walkingID, true);

        while (Vector2.Distance(character.position, destination.position) > 0.05f)
        {
            character.position = Vector2.MoveTowards(
                character.position,
                destination.position,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        character.position = destination.position;

        if (canAnimate) animator.SetBool(walkingID, false);

        // ถึงแล้ว กลับไปหันทิศเดิม
        if (returnToDefaultFacing)
        {
            FaceTowards(spriteFacesRightByDefault);
        }

        onArrived?.Invoke();
    }
}