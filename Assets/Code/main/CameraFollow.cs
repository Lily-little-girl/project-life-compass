using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public float smoothSpeed = 5f;
    public Vector2 offset = new Vector2(2f, 0f);

    [Header("Map Bounds")]
    public Vector2 mapMin;
    public Vector2 mapMax;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // ขนาดพื้นที่ที่กล้องมองเห็น
        float height = cam.orthographicSize;
        float width = height * cam.aspect;

        // ตำแหน่งเป้าหมายของกล้อง
        float targetX = target.position.x + offset.x;
        float targetY = target.position.y + offset.y;

        // จำกัดไม่ให้กล้องออกนอกขอบแมพ
        targetX = Mathf.Clamp(
            targetX,
            mapMin.x + width,
            mapMax.x - width
        );

        targetY = Mathf.Clamp(
            targetY,
            mapMin.y + height,
            mapMax.y - height
        );

        Vector3 targetPosition = new Vector3(
            targetX,
            targetY,
            transform.position.z
        );

        // เคลื่อนกล้องอย่างนุ่มนวล
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}