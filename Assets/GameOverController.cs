using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// ฉากแพ้: จั๊มสแกเต็มจอสั่นๆ -> โชว์ภาพ game over ทับ -> คลิกที่ไหนก็ได้เพื่อเริ่มใหม่
public class GameOverController : MonoBehaviour
{
    [SerializeField] private Image jumpscareImage;   // รูปตัวม่วงเต็มจอ
    [SerializeField] private Image gameOverImage;    // รูป gameover (พื้นหลังโปร่งใส) ต้องอยู่ใต้ jumpscare ใน Hierarchy
    [SerializeField] private AudioSource jumpscareSound; // ไม่ใส่ก็ได้

    [Header("Jumpscare")]
    [SerializeField] private float shakeTime = 1f;        // สั่นกี่วินาที
    [SerializeField] private float shakeStrength = 40f;   // แรงสั่น (พิกเซล)
    [SerializeField] private float baseScale = 1.15f;     // ขยายเกินจอนิดหน่อย ขอบจะได้ไม่โผล่ตอนสั่น

    [Header("Game Over")]
    [SerializeField] private float fadeInTime = 0.3f;
    [SerializeField] private float clickDelay = 0.5f;     // กันเผลอคลิกข้ามทันที

    private bool playing;
    private bool canRestart;

    private void Awake()
    {
        if (jumpscareImage != null) jumpscareImage.gameObject.SetActive(false);
        if (gameOverImage != null) gameOverImage.gameObject.SetActive(false);
    }

    public void Play()
    {
        if (playing) return;
        playing = true;
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        // 1) จั๊มสแก + สั่น
        jumpscareImage.gameObject.SetActive(true);
        if (jumpscareSound != null) jumpscareSound.Play();

        RectTransform rt = jumpscareImage.rectTransform;
        float t = 0f;
        while (t < shakeTime)
        {
            t += Time.unscaledDeltaTime;
            rt.anchoredPosition = Random.insideUnitCircle * shakeStrength;
            rt.localScale = Vector3.one * (baseScale + Random.Range(0f, 0.05f));
            yield return null;
        }
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one * baseScale;

        // 2) game over ทับ (fade เข้า)
        gameOverImage.gameObject.SetActive(true);
        Color c = gameOverImage.color;
        t = 0f;
        while (t < fadeInTime)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(t / fadeInTime);
            gameOverImage.color = c;
            yield return null;
        }
        c.a = 1f;
        gameOverImage.color = c;

        // 3) รอแป๊บ แล้วคลิกที่ไหนก็ได้เพื่อเริ่มใหม่
        yield return new WaitForSecondsRealtime(clickDelay);
        canRestart = true;
    }

    private void Update()
    {
        if (canRestart && ClickedThisFrame())
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private static bool ClickedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
}