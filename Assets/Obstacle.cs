using UnityEngine;

/// ใส่ที่ rock / fence_x / fence_y (ต้องมี Collider2D)
/// = ช่องที่ blob เดินเข้าไปไม่ได้ (ชนแล้วแค่ติด ไม่แพ้)
[RequireComponent(typeof(Collider2D))]
public class Obstacle : MonoBehaviour { }