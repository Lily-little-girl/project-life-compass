using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject gameOverPanel;
    public GameObject winPanel;            // ว่างได้ ถ้ายังไม่มีสไปรต์ชนะ

    [Header("Player")]
    public GameObject playerObject;        // ว่างได้ จะหาจาก Tag "Player" ให้เอง

    [Header("Timing")]
    public float extraDelay = 0.5f;        // รอเพิ่มก่อนขึ้นหน้าจอ
    public float clickDelay = 0.5f;        // กันคลิกพลาดตอนเพิ่งขึ้นหน้าจอ

    public static bool IsGameOver { get; private set; }   // เกมจบแล้ว (แพ้หรือชนะ)
    public static bool HasWon { get; private set; }

    private bool panelShown;
    private float shownAt;

    void Awake()
    {
        IsGameOver = false;
        HasWon = false;
        Time.timeScale = 1f;
    }

    void OnEnable()
    {
        Kid.OnAnyKidHurt += HandleKidHurt;
        GameTimer.OnTimeUp += HandleTimeUp;
    }

    void OnDisable()
    {
        Kid.OnAnyKidHurt -= HandleKidHurt;
        GameTimer.OnTimeUp -= HandleTimeUp;
    }

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
    }

    void HandleKidHurt(Kid kid, Hazard hazard)
    {
        if (IsGameOver) return;   // เด็กคนแรกที่โดนเท่านั้นที่นับ

        IsGameOver = true;
        FreezePlayer();

        StartCoroutine(GameOverSequence(kid, hazard));
    }

    void HandleTimeUp()
    {
        if (IsGameOver) return;   // ถ้ามีเด็กเจ็บไปก่อนแล้ว ไม่นับว่าชนะ

        IsGameOver = true;
        HasWon = true;
        FreezePlayer();

        StartCoroutine(WinSequence());
    }

    IEnumerator GameOverSequence(Kid kid, Hazard hazard)
    {
        float wait = hazard != null ? hazard.PlayHurtEffect(kid) : 0f;

        yield return new WaitForSeconds(wait + extraDelay);

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        panelShown = true;
        shownAt = Time.time;
    }

    IEnumerator WinSequence()
    {
        yield return new WaitForSeconds(extraDelay);

        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
        else
        {
            Debug.Log("เวลาหมด ชนะแล้ว (ยังไม่ได้ใส่ Win Panel)");
        }

        panelShown = true;
        shownAt = Time.time;
    }

    private void FreezePlayer()
    {
        GameObject p = playerObject != null
            ? playerObject
            : GameObject.FindGameObjectWithTag("Player");

        if (p == null)
        {
            Debug.LogWarning("หาผู้เล่นไม่เจอ ให้ตั้ง Tag เป็น Player หรือลากใส่ช่อง Player Object");
            return;
        }

        p.SendMessage("SetMovementEnabled", false, SendMessageOptions.DontRequireReceiver);

        Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    void Update()
    {
        if (!panelShown) return;
        if (Time.time - shownAt < clickDelay) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}