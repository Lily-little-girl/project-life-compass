using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGrabber : MonoBehaviour
{
    public Key grabKey = Key.E;
    public float grabRadius = 1.2f;

    [Header("Landing")]
    public Transform landingPoint;        // จุดที่เด็กจะกระเด็นไปลง
    public float scatterRadius = 0.5f;    // สุ่มกระจายรอบจุด เด็กหลายคนจะได้ไม่ซ้อนกันพอดี

    [Header("Bounce")]
    public float bounceDuration = 0.6f;
    public float bounceHeight = 1.2f;

    void Update()
    {
        if (GameOverManager.IsGameOver) return;

        if (Keyboard.current != null && Keyboard.current[grabKey].wasPressedThisFrame)
        {
            TryGrab();
        }
    }

    void TryGrab()
    {
        if (landingPoint == null)
        {
            Debug.LogWarning("ยังไม่ได้ใส่ Landing Point");
            return;
        }

        Kid nearest = null;
        float best = grabRadius;

        // Kid.All คือเด็กที่ active อยู่ตอนนี้ (รวมเด็กวิ่งเร็วที่เพิ่งโผล่)
        foreach (Kid kid in Kid.All)
        {
            if (kid == null || kid.IsAirborne) continue;

            float d = Vector2.Distance(transform.position, kid.transform.position);
            if (d <= best)
            {
                best = d;
                nearest = kid;
            }
        }

        if (nearest == null) return;

        Vector2 landing = (Vector2)landingPoint.position + Random.insideUnitCircle * scatterRadius;
        nearest.BounceTo(landing, bounceDuration, bounceHeight);
    }

    void OnDrawGizmos()
    {
        if (landingPoint == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(landingPoint.position, Mathf.Max(0.1f, scatterRadius));
    }
}