using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// จัดการด่าน: เปิดทีละด่าน, เช็กเส้นชัย, ไปด่านถัดไป
public class LevelManager : MonoBehaviour
{
    [Serializable]
    public class Level
    {
        public string name = "Level";
        public GameObject root;              // Level 1/2/3 (แม่ของทุกอย่างในด่านนี้)
        public Transform startPoint;         // จุดเริ่มของ blob
        public Collider2D finishHitbox;      // hitbox ช่องธง
        public Collider2D playAreaCollider;  // กรอบพื้นที่เดินได้ของด่านนี้
        public float cellSize = 1f;          // ขนาด 1 ก้าวของด่านนี้
        public float blobScale = 1f;         // คูณกับขนาดเดิมของ blob (ด่านละเอียดใส่ < 1)
        public float padding = 0.3f;         // เว้นจากขอบพื้นที่เดิน
    }

    [SerializeField] private BlobRunner runner;          // blob_0
    [SerializeField] private CommandInputBox commandBox; // bubble command_0
    [SerializeField] private List<Level> levels = new List<Level>();
    [SerializeField] private float clearDelay = 0.8f;    // รอก่อนเปลี่ยนด่าน
    [SerializeField] private GameObject winScreen;       // โชว์ตอนผ่านครบทุกด่าน (ไม่ใส่ก็ได้)
    [Header("Out of moves")]
    [SerializeField] private GameOverController outOfMovesGameOver; // ฉากแพ้แบบคำสั่งหมดแล้วไม่ถึงธง
    [SerializeField] private float finalAngle = 180f;  // องศา (Rotation Z) ที่ blob นอนหงายอยู่ตอนจบ
    [SerializeField] private int extraSpins = 0;       // หมุนเพิ่มกี่รอบระหว่างล้ม (0 = หมุนสั้นที่สุด)
    [SerializeField] private float flipTime = 0.5f;
    [SerializeField] private float hopHeight = 0.4f;   // กระโดดนิดหนึ่งตอนล้ม
    [SerializeField] private float failDelay = 0.6f;   // รอก่อนขึ้นฉากแพ้
    [SerializeField] private EndingSequence ending;   // ฉากจบ (ไม่ใส่ = ใช้ winScreen แบบเดิม)

    // จำด่านปัจจุบันไว้ตอนโดนรีสตาร์ทซีน (แพ้แล้วเริ่มด่านเดิม ไม่เด้งกลับด่าน 1)
    public static int savedIndex;

    private int index;
    private bool transitioning;
    private bool allCleared;
    private Vector3 baseBlobScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => savedIndex = 0;

    private void Awake() => baseBlobScale = runner.transform.localScale;

    private void Start()
    {
        if (winScreen != null) winScreen.SetActive(false);
        LoadLevel(savedIndex);
    }

    private void Update()
    {
        if (allCleared)
        {
            // ถ้ามีฉากจบ ปล่อยให้ปุ่มในหน้าต่าง error จัดการเอง
            if (ending == null && ClickedThisFrame())
            {
                savedIndex = 0;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        if (transitioning || levels.Count == 0) return;

        Collider2D finish = levels[index].finishHitbox;
        if (finish != null && finish.OverlapPoint(runner.transform.position))
            StartCoroutine(CompleteLevel());
    }

    private IEnumerator CompleteLevel()
    {
        transitioning = true;
        runner.Halt();
        yield return new WaitForSeconds(clearDelay);

        if (index + 1 < levels.Count)
        {
            savedIndex = index + 1;
            LoadLevel(savedIndex);
        }
        else
        {
            allCleared = true;
            if (ending != null) ending.Play();
            else if (winScreen != null) winScreen.SetActive(true);
        }
        transitioning = false;
    }

    private void LoadLevel(int i)
    {
        index = Mathf.Clamp(i, 0, levels.Count - 1);

        for (int k = 0; k < levels.Count; k++)
            if (levels[k].root != null) levels[k].root.SetActive(k == index);

        Level lv = levels[index];
        runner.ConfigureLevel(lv.cellSize, lv.playAreaCollider, lv.padding);

        Transform t = runner.transform;
        t.position = lv.startPoint.position;
        t.rotation = Quaternion.identity;
        t.localScale = baseBlobScale * lv.blobScale;

        var sr = runner.GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipX = false;

        if (commandBox != null) commandBox.ResetForNewLevel();
        Physics2D.SyncTransforms();
    }

    private static bool ClickedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
    private void OnEnable()
    {
        if (runner != null) runner.RunFinished += OnRunFinished;
    }

    private void OnDisable()
    {
        if (runner != null) runner.RunFinished -= OnRunFinished;
    }

    private void OnRunFinished()
    {
        if (transitioning || allCleared) return;

        Collider2D finish = levels[index].finishHitbox;
        bool atFinish = finish != null && finish.OverlapPoint(runner.transform.position);

        if (atFinish) StartCoroutine(CompleteLevel());
        else StartCoroutine(FailLevel());
    }

    private IEnumerator FailLevel()
    {
        transitioning = true;
        runner.Halt();

        Transform bt = runner.transform;
        Vector3 startPos = bt.position;
        float startZ = bt.eulerAngles.z;

        // หมุนไปทางเดียวจนถึง finalAngle พอดี ไม่ว่าจะเริ่มจากหันทางไหน
        float delta = Mathf.Repeat(finalAngle - startZ, 360f) + 360f * extraSpins;

        float t = 0f;
        while (t < flipTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / flipTime);
            bt.rotation = Quaternion.Euler(0f, 0f, startZ + delta * Mathf.SmoothStep(0f, 1f, k));
            bt.position = startPos + Vector3.up * (hopHeight * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }
        bt.position = startPos;
        bt.rotation = Quaternion.Euler(0f, 0f, finalAngle);   // ล็อคท่าสุดท้ายให้ตรงเสมอ

        yield return new WaitForSeconds(failDelay);
        if (outOfMovesGameOver != null) outOfMovesGameOver.Play();
    }
}