using UnityEngine;

/// ใส่ที่ purple blob_0 (ต้องมี Collider2D ไว้เป็น hitbox)
/// แพ้เมื่อจุดกลางของ blob เข้าไปใน hitbox (+ รัศมีตัว blob ถ้าตั้งไว้)
[RequireComponent(typeof(Collider2D))]
public class PurpleBlobHazard : MonoBehaviour
{
    [SerializeField] private BlobRunner player;           // ลาก blob_0 มาใส่
    [SerializeField] private GameOverController gameOver; // ลาก GameOverCanvas มาใส่
    [SerializeField] private float playerRadius = 0f;     // 0 = ต้องเข้ามาถึงจุดกลางตัว blob; เพิ่มค่าถ้าอยากให้โดนง่ายขึ้น

    private Collider2D hitbox;
    private bool triggered;

    private void Awake() => hitbox = GetComponent<Collider2D>();

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        if (triggered || player == null || gameOver == null || player.Frozen) return;

        Vector2 p = player.transform.position;
        bool hit = hitbox.OverlapPoint(p) ||
                   (playerRadius > 0f && Vector2.Distance(hitbox.ClosestPoint(p), p) <= playerRadius);

        if (hit)
        {
            triggered = true;
            player.Halt();
            gameOver.Play();
        }
    }
}