using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public static event System.Action OnTimeUp;
    public static float RemainingTime { get; private set; }   // เวลาที่เหลือ (วินาที)

    public float duration = 60f;
    public TMP_Text timerText;

    [Header("Warning")]
    public float warningTime = 10f;
    public Color normalColor = Color.white;
    public Color warningColor = Color.red;

    private float remaining;
    private bool finished;

    void Awake()
    {
        RemainingTime = duration;
    }

    void Start()
    {
        remaining = duration;
        RemainingTime = remaining;
        UpdateText();
    }

    void Update()
    {
        if (finished || GameOverManager.IsGameOver) return;

        remaining -= Time.deltaTime;

        if (remaining <= 0f)
        {
            remaining = 0f;
            RemainingTime = 0f;
            finished = true;
            UpdateText();
            OnTimeUp?.Invoke();
            return;
        }

        RemainingTime = remaining;
        UpdateText();
    }

    void UpdateText()
    {
        if (timerText == null) return;

        int total = Mathf.CeilToInt(remaining);
        int minutes = total / 60;
        int seconds = total % 60;

        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        timerText.color = remaining <= warningTime ? warningColor : normalColor;
    }
}