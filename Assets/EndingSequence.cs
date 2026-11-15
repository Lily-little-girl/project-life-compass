using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// ฉากจบ: เฟดพื้นหลังอวกาศ -> blob หมุนลอยออกไป -> หน้าต่าง error เด้งขึ้น
public class EndingSequence : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BlobRunner blob;                // blob_0
    [SerializeField] private SpriteRenderer spaceBackground; // sprite พื้นหลังอวกาศ
    [SerializeField] private GameObject[] hideDuringEnding;  // bubble command_0, enter_0 ฯลฯ
    [SerializeField] private GameObject errorWindow;         // หน้าต่าง error (UI)

    [Header("Timing")]
    [SerializeField] private float fadeTime = 2f;
    [SerializeField] private float floatTime = 6f;
    [SerializeField] private float pauseBeforeError = 0.8f;

    [Header("Float away")]
    [SerializeField] private float spinSpeed = 40f;          // องศา/วินาที
    [SerializeField] private Vector2 driftDirection = new Vector2(1f, 1f);
    [SerializeField] private float driftDistance = 12f;      // ระยะที่ลอยออกไป (หน่วย Unity)
    [SerializeField] private float endScaleFactor = 0.15f;   // ตอนจบตัวเล็กเหลือกี่เท่า

    private bool playing;

    private void Awake()
    {
        if (spaceBackground != null) spaceBackground.gameObject.SetActive(false);
        if (errorWindow != null) errorWindow.SetActive(false);
    }

    public void Play()
    {
        if (playing) return;
        playing = true;
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        foreach (GameObject go in hideDuringEnding)
            if (go != null) go.SetActive(false);

        // ให้พื้นหลังอยู่หลัง blob เสมอ และอยู่หน้าของอื่นทั้งหมดในด่าน
        SpriteRenderer blobSr = blob.GetComponent<SpriteRenderer>();
        spaceBackground.sortingOrder = 90;
        if (blobSr != null) blobSr.sortingOrder = 91;

        FitBackgroundToCamera();
        spaceBackground.gameObject.SetActive(true);

        // 1) เฟดพื้นหลัง
        Color c = spaceBackground.color;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / fadeTime);
            spaceBackground.color = c;
            yield return null;
        }
        c.a = 1f;
        spaceBackground.color = c;

        // 2) blob หมุนช้าๆ ลอยออกไป (เริ่มช้า ค่อยๆ เร็วขึ้น)
        Transform bt = blob.transform;
        Vector3 startPos = bt.position;
        Vector3 startScale = bt.localScale;
        float startAngle = bt.eulerAngles.z;
        Vector3 dir = driftDirection.normalized;

        t = 0f;
        while (t < floatTime)
        {
            t += Time.deltaTime;
            float k = t / floatTime;
            float eased = k * k;
            bt.position = startPos + dir * driftDistance * eased;
            bt.localScale = Vector3.Lerp(startScale, startScale * endScaleFactor, eased);
            bt.rotation = Quaternion.Euler(0f, 0f, startAngle + spinSpeed * t);
            yield return null;
        }

        // 3) หน้าต่าง error เด้งขึ้น
        yield return new WaitForSeconds(pauseBeforeError);

        if (errorWindow != null)
        {
            errorWindow.SetActive(true);
            Transform et = errorWindow.transform;
            Vector3 full = et.localScale;
            float p = 0f;
            while (p < 0.12f)
            {
                p += Time.deltaTime;
                et.localScale = Vector3.Lerp(full * 0.85f, full, p / 0.12f);
                yield return null;
            }
            et.localScale = full;
        }
    }

    private void FitBackgroundToCamera()
    {
        Camera cam = Camera.main;
        if (cam == null || spaceBackground.sprite == null) return;

        float h = cam.orthographicSize * 2f;
        float w = h * cam.aspect;
        Vector2 s = spaceBackground.sprite.bounds.size;
        float scale = Mathf.Max(w / s.x, h / s.y) * 1.02f;

        Transform bg = spaceBackground.transform;
        bg.localScale = new Vector3(scale, scale, 1f);
        bg.position = new Vector3(cam.transform.position.x, cam.transform.position.y, blob.transform.position.z);
    }

    // ผูกกับปุ่มในหน้าต่าง error (OnClick)
    public void RestartGame()
    {
        LevelManager.savedIndex = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}