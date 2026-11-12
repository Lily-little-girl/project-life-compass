using UnityEngine;

public class SkipButton : MonoBehaviour
{
    public GameObject[] dayIcons;     // day 1 - day 5
    public GameObject[] newsImages;   // news 1 - news 5

    int dayIndex = 0;                 // 0 = Day 1

    void Start()
    {
        ShowDay();
    }

    // ทำงานเมื่อคลิกซ้ายที่ปุ่มนี้
    void OnMouseDown()
    {
        if (dayIndex >= 4) return;    // วัน 5 แล้ว ไปต่อไม่ได้
        dayIndex++;
        ShowDay();
    }

    void ShowDay()
    {
        for (int i = 0; i < dayIcons.Length; i++)
            dayIcons[i].SetActive(i == dayIndex);

        for (int i = 0; i < newsImages.Length; i++)
            newsImages[i].SetActive(i == dayIndex);
    }
}