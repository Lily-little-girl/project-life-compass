using System.Collections;
using UnityEngine;

/// ใส่ที่ของที่เก็บได้ (ต้องมี Collider2D เป็น hitbox)
/// blob เดินทับ = ของหายไป (blob ไม่ติด ไม่หยุด)
[RequireComponent(typeof(Collider2D))]
public class Pickup : MonoBehaviour
{
    [SerializeField] private BlobRunner player;      // ลาก blob_0 มาใส่
    [SerializeField] private AudioClip pickupSound;  // ไม่ใส่ก็ได้
    [SerializeField] private float popTime = 0.15f;  // เวลาหดหายไป (0 = หายทันที)

    private Collider2D hitbox;
    private bool collected;

    private void Awake() => hitbox = GetComponent<Collider2D>();

    private void Update()
    {
        if (collected || player == null) return;

        if (hitbox.OverlapPoint(player.transform.position))
        {
            collected = true;
            StartCoroutine(Collect());
        }
    }

    private IEnumerator Collect()
    {
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < popTime)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, Vector3.zero, t / popTime);
            yield return null;
        }
        gameObject.SetActive(false);
    }
}