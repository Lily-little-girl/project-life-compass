using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum HurtEffectType { None, ReplaceAndShake, SpinShrinkFade, LieDownAndShake, SwapSpriteAndShake }

public class Hazard : MonoBehaviour
{
    public static readonly List<Hazard> All = new List<Hazard>();

    [Tooltip("จุดที่เด็กไปยืนหยุด ถ้าว่างจะใช้ตำแหน่งของ object นี้")]
    public Transform standPoint;

    [Header("Hurt Effect (ตอนเด็กโดนของชิ้นนี้)")]
    public HurtEffectType hurtEffect = HurtEffectType.None;
    public float effectDuration = 2f;   // รอเท่านี้ก่อนขึ้นหน้า Game Over (รวมเวลา delay ด้านล่างแล้ว)

    [Header("ReplaceAndShake (เครื่องซักผ้า / ยาพิษ)")]
    public GameObject replacementObject; // object ที่ปิดอยู่ จะถูกเปิดขึ้นมาแทนเด็ก
    public float replaceDelay = 0f;      // เด็กหายไปก่อน แล้วรอเท่านี้ค่อยโผล่ object แทน
    public float shakeStrength = 0.05f;
    public float shakeInterval = 0.03f;  // ยิ่งน้อยยิ่งสั่นถี่

    [Header("SwapSpriteAndShake (เครื่องซักผ้า)")]
public SpriteRenderer targetRenderer;  // ว่าง = ใช้ SpriteRenderer บน object นี้หรือลูกของมัน
public Sprite hurtSprite;              // สไปรต์ตอนเปลี่ยน เช่น เครื่องแบบปิด

    [Header("SpinShrinkFade (บันได)")]
    public float spinSpeed = 720f;       // องศาต่อวินาที
    public float spinDuration = 1.2f;
    public float fallDistance = 1.5f;                          // ตกลงไปไกลแค่ไหน (หน่วย Unity)
public Vector2 fallDirection = new Vector2(0f, -1f);       // ทิศที่ตก ลงตรงๆ = (0,-1)
[Range(0f, 1f)] public float endScale = 0.5f;              // ย่อเหลือเท่าไหร่ตอนท้าย (0 = ย่อจนหาย)

    [Header("LieDownAndShake (น้ำไฟช็อต)")]
    public float lieAngle = 90f;         // 90 หรือ -90 เลือกด้านที่ล้มลง
    public float lieDownTime = 0.3f;
    public float lieShakeStrength = 0.05f;
    public GameObject[] hideOnReplace;       // ของเดิมที่ต้องซ่อน เช่น ขวดยาตัวเก่า
public bool lieDownWhileShaking = false; // ติ๊ก = สไปรต์ใหม่ล้มนอนลงระหว่างสั่น

    public Vector2 StandPosition
    {
        get { return standPoint != null ? (Vector2)standPoint.position : (Vector2)transform.position; }
    }

    void OnEnable()  { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(StandPosition, 0.3f);
    }

    // เริ่มเล่นเอฟเฟกต์ แล้วบอกกลับว่าต้องรอกี่วินาทีก่อนขึ้น Game Over
    public float PlayHurtEffect(Kid kid)
    {
        switch (hurtEffect)
        {
            case HurtEffectType.ReplaceAndShake:
                StartCoroutine(ReplaceAndShakeRoutine(kid));
                return effectDuration;

            case HurtEffectType.SpinShrinkFade:
                StartCoroutine(SpinShrinkFadeRoutine(kid));
                return effectDuration;

            case HurtEffectType.LieDownAndShake:
                StartCoroutine(LieDownRoutine(kid));
                return effectDuration;

                case HurtEffectType.SwapSpriteAndShake:
    StartCoroutine(SwapSpriteAndShakeRoutine(kid));
    return effectDuration;

            default:
                return 0f;
        }
    }

    private IEnumerator ReplaceAndShakeRoutine(Kid kid)
{
    kid.FreezeAnimation();
    kid.HideVisual();

    if (replaceDelay > 0f) yield return new WaitForSeconds(replaceDelay);

    HideObjects(hideOnReplace);

    if (replacementObject == null)
    {
        Debug.LogWarning("ReplaceAndShake: ยังไม่ได้ใส่ Replacement Object");
        yield break;
    }

    replacementObject.SetActive(true);
    Transform r = replacementObject.transform;

    // สั่นไปเรื่อยๆ ในอีกคอรูทีน เพื่อให้หมุนล้มลงพร้อมกันได้
    StartCoroutine(ShakeForever(r, shakeStrength));

    if (lieDownWhileShaking)
    {
        yield return RotateTo(r, lieAngle, lieDownTime);
    }
}

    private void HideObjects(GameObject[] objs)
{
    if (objs == null) return;

    foreach (GameObject go in objs)
    {
        if (go == null) continue;

        foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.enabled = false;
        }
    }
}

private IEnumerator RotateTo(Transform t, float angle, float duration)
{
    Quaternion start = t.localRotation;
    Quaternion end = Quaternion.Euler(0f, 0f, angle);

    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        t.localRotation = Quaternion.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
        yield return null;
    }

    t.localRotation = end;
}
   private IEnumerator SpinShrinkFadeRoutine(Kid kid)
{
    kid.FreezeAnimation();

    Transform v = kid.visual != null ? kid.visual : kid.transform;
    SpriteRenderer sr = kid.spriteRenderer;

    Vector3 startScale = v.localScale;
    Vector3 startPos = v.position;
    Color startColor = sr != null ? sr.color : Color.white;

    Vector2 d = fallDirection.sqrMagnitude > 0.0001f ? fallDirection.normalized : Vector2.down;
    Vector3 dir = new Vector3(d.x, d.y, 0f);

    float t = 0f;
    while (t < spinDuration)
    {
        t += Time.deltaTime;
        float p = Mathf.Clamp01(t / spinDuration);

        // หมุนไปเรื่อยๆ
        v.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        // ตกลงแบบเร่งความเร็ว (p*p ทำให้ช้าตอนแรก แล้วเร็วขึ้น)
        v.position = startPos + dir * (fallDistance * p * p);

        // ย่อลงเล็กน้อยตามระยะที่ตกไกลออกไป
        v.localScale = startScale * Mathf.Lerp(1f, endScale, p);

        // ค่อยๆ จางจนหายตอนท้าย
        if (sr != null)
        {
            Color c = startColor;
            c.a = 1f - p;
            sr.color = c;
        }

        yield return null;
    }

    if (sr != null) sr.enabled = false;
}

    private IEnumerator LieDownRoutine(Kid kid)
    {
        kid.FreezeAnimation();

        Transform v = kid.visual != null ? kid.visual : kid.transform;

        Quaternion start = v.localRotation;
        Quaternion end = Quaternion.Euler(0f, 0f, lieAngle);

        float t = 0f;
        while (t < lieDownTime)
        {
            t += Time.deltaTime;
            v.localRotation = Quaternion.Lerp(start, end, Mathf.Clamp01(t / lieDownTime));
            yield return null;
        }

        v.localRotation = end;
        yield return ShakeForever(v, lieShakeStrength);
    }

   private IEnumerator SwapSpriteAndShakeRoutine(Kid kid)
{
    kid.FreezeAnimation();
    kid.HideVisual();

    if (replaceDelay > 0f) yield return new WaitForSeconds(replaceDelay);

    SpriteRenderer sr = targetRenderer != null
        ? targetRenderer
        : GetComponentInChildren<SpriteRenderer>();

    if (sr == null)
    {
        Debug.LogWarning("SwapSpriteAndShake: ไม่เจอ Sprite Renderer ของเครื่อง ให้ลากใส่ช่อง Target Renderer");
        yield break;
    }

    Debug.Log("Machine shake: " + sr.name + " strength=" + shakeStrength);

    if (hurtSprite != null) sr.sprite = hurtSprite;

    yield return ShakeForever(sr.transform, shakeStrength ,true);
}

    // สั่นรอบตำแหน่งเดิมไปเรื่อยๆ จนกว่าจะโหลดซีนใหม่
  private IEnumerator ShakeForever(Transform t, float strength, bool horizontalOnly = false)
{
    Vector3 basePos = t.position;
    WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.005f, shakeInterval));

    while (true)
    {
        Vector3 offset;

        if (horizontalOnly)
        {
            // สั่นซ้ายขวาอย่างเดียว ไม่ขยับแกน Y
            offset = new Vector3(Random.Range(-1f, 1f) * strength, 0f, 0f);
        }
        else
        {
            offset = (Vector3)(Random.insideUnitCircle * strength);
        }

        t.position = basePos + offset;
        yield return wait;
    }
}
}