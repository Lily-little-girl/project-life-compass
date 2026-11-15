using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class TaskManager : MonoBehaviour
{
    [System.Serializable]
    public class Task
    {
        public string name;
        public GameObject panel;           // แถบโจทย์ (ต้องมี SlidePanel)
        public float timeLimit = 0f;       // วินาที, 0 = ไม่จำกัดเวลา
        public bool isExample = false;     // true = ตัวอย่าง (วาดไม่ได้ ไม่มีเวลา)
        public ExamplePanel exampleImage;  // รูปตัวอย่างภาพวาด (ใส่เฉพาะโจทย์ตัวอย่าง)

        [System.NonSerialized] public SlidePanel slide;
    }

    public Task[] tasks;

    [Header("อ้างอิง")]
    public DrawingController drawing;
    public TMP_Text timerText;

    public UnityEvent onAllFinished;

    private int index = -1;
    private float timeLeft;
    private bool timed;
    private bool finished;
    private bool leaving;   // กำลังรอรูปตัวอย่างเฟดออก

    void Start()
    {
        foreach (Task t in tasks)
        {
            if (t.panel != null)
            {
                t.slide = t.panel.GetComponent<SlidePanel>();
                t.panel.SetActive(false);
            }
            if (t.exampleImage != null)
                t.exampleImage.gameObject.SetActive(false);
        }

        GoToTask(0);
    }

    void Update()
    {
        if (finished || index < 0) return;

        Task cur = tasks[index];

        // กำลังรอเฟดออก
        if (leaving)
        {
            if (cur.exampleImage == null || cur.exampleImage.IsFadedOut)
            {
                leaving = false;
                GoToTask(index + 1);
            }
            return;
        }

        // นับเวลาถอยหลัง
        if (timed)
        {
            timeLeft -= Time.deltaTime;
            UpdateTimerText();

            if (timeLeft <= 0f)
            {
                FinishCurrent();
                return;
            }
        }

        // กดปุ่ม "เสร็จ"
        if (cur.slide != null && cur.slide.DonePressed)
            FinishCurrent();
    }

    void FinishCurrent()
    {
        Task cur = tasks[index];

        if (cur.exampleImage != null)
        {
            // เฟดรูปตัวอย่างออกก่อนค่อยไปต่อ
            leaving = true;
            cur.exampleImage.FadeOut();
        }
        else
        {
            GoToTask(index + 1);
        }
    }

    void GoToTask(int i)
    {
        // ปิดโจทย์เก่า
        if (index >= 0)
        {
            if (tasks[index].panel != null) tasks[index].panel.SetActive(false);
            if (tasks[index].exampleImage != null) tasks[index].exampleImage.gameObject.SetActive(false);
        }

        // จบทุกโจทย์
        if (i >= tasks.Length)
        {
            finished = true;
            SetUI(false);
            if (drawing != null) drawing.enabled = false;
            Debug.Log("จบทุกโจทย์แล้ว");
            onAllFinished.Invoke();
            return;
        }

        index = i;
        Task t = tasks[index];

        if (t.panel != null) t.panel.SetActive(true);
        if (t.exampleImage != null) t.exampleImage.gameObject.SetActive(true);  // เฟดเข้าเอง

        if (drawing != null)
        {
            drawing.ClearAll();
            drawing.enabled = !t.isExample;
        }

        timed = t.timeLimit > 0f;
        timeLeft = t.timeLimit;

        SetUI(!t.isExample);
        UpdateTimerText();

        Debug.Log("เริ่ม: " + t.name);
    }

    void SetUI(bool showTimer)
    {
        if (timerText != null) timerText.gameObject.SetActive(showTimer);
    }

    void UpdateTimerText()
    {
        if (timerText == null) return;

        if (!timed)
        {
            timerText.text = "ไม่จำกัดเวลา";
            return;
        }

        int sec = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
        timerText.text = (sec / 60) + ":" + (sec % 60).ToString("00");
    }
}