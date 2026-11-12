using System.Collections;
using UnityEngine;
using TMPro;

public class SkipButton : MonoBehaviour
{
    public GameObject[] dayIcons;     // day 1 - day 5
    public GameObject[] newsImages;   // news 1 - news 5
    public TMP_Text moneyText;        // ข้อความเงินบนธนบัตร

    public GameObject minerObject;    // รูป miner ตัวแรก
    public float minerGap = 1.1f;     // ระยะห่าง (1 = ชิดกันพอดี)

    public int minerCost = 700;       // ราคาเครื่องขุด
    public int goalMoney = 3000;      // เป้าหมาย

    // ราคาเหรียญของแต่ละวัน Day 1..5 (ใช้ตอนเริ่มวัน)
    public int[] dailyPrice = { 0, -300, 800, -400, 1000 };

    public TMP_Text popupTemplate;    // moneyPopup (ต้นแบบที่ปิดไว้)
    public float popupRise = 1.0f;    // ลอยขึ้นสูงแค่ไหน
    public float popupTime = 1.2f;    // ใช้เวลากี่วินาที

    [Header("เฟดจอตอนข้ามวัน")]
    public SpriteRenderer fadeSprite; // รูปดำ fade
    public float fadeOutTime = 0.5f;  // เวลาเฟดเป็นสีดำ
    public float blackHoldTime = 0.4f;// ค้างจอดำก่อนเฟดกลับ
    public float fadeInTime = 0.5f;   // เวลาเฟดกลับมาสว่าง

    [Header("ข่าวประจำวัน")]
    public TMP_Text newsText;         // newsText บนแถบ bubble
    public float typeDelay = 0.04f;   // หน่วงต่อตัวอักษร (ยิ่งน้อยยิ่งพิมพ์เร็ว)
    public float holdSeconds = 2f;    // ค้างให้อ่านหลังพิมพ์จบแต่ละหน้า
    public float fadeSeconds = 1f;    // เวลาจางหายตอนจบทั้งหมด

    [TextArea(2, 4)]
    public string[] dailyNews =
    {
        "ตลาดคริปโตทรงตัวในไซด์เวย์ นักวิเคราะห์ชี้ตลาดยังไร้ทิศทางชัดเจน ไร้ปัจจัยหนุน พรุ่งนี้สุ่มเสี่ยงย่อตัว",
        "ตลาดโดนเทขายตามคาด! เกิดแรง Panic Sell สั้นๆ พรุ่งนี้คาดการณ์วาฬเตรียมเข้าช้อน ดันราคารีบาวด์กลับ",
        "กราฟพุ่งทะยานสีเขียวขจี! แต่นักวิเคราะห์เตือนระวังแรงเทขายทำกำไร พรุ่งนี้เสี่ยงปรับฐานแรง",
        "ตลาดร่วงหนักปรับฐานตามนัด! ทว่าสัญญาณเทคนิคล่าสุดชี้ชัด โค้งสุดท้ายตลาดกระทิง พรุ่งนี้เตรียมทะยานส่งท้าย",
        "กราฟพุ่งทะลุเพดานส่งท้ายฤดูกาล! ตลาดปิดรอบอย่างสมบูรณ์แบบ"
    };

    int dayIndex = 0;                 // 0 = Day 1
    int money = 1000;                 // เงินเริ่มต้น
    int minerCount = 1;               // เริ่มมี 1 เครื่อง
    bool gameOver = false;
    bool busy = false;                // true ระหว่างเฟด กันกดซ้ำ
    Coroutine newsRoutine;

    void Start()
    {
        SetFade(0f);                  // เริ่มเกมจอสว่าง
        ShowDay();
        UpdateMoney();
        StartDay();                   // Day 1 (ราคา 0)
    }

    // คลิกปุ่มข้าม
    void OnMouseDown()
    {
        if (gameOver || busy) return;
        if (dayIndex >= 4) return;
        StartCoroutine(NextDayRoutine());
    }

    // ข้ามวัน: หยุดข่าว -> เฟดดำ -> เปลี่ยนวัน -> เฟดสว่าง -> เริ่มวันใหม่
    IEnumerator NextDayRoutine()
    {
        busy = true;

        StopNews();                                   // ไม่ให้ข่าวเดินต่อใต้จอดำ

        yield return Fade(0f, 1f, fadeOutTime);       // เฟดเป็นสีดำ

        dayIndex++;                                   // เปลี่ยนตอนจอดำสนิท
        ShowDay();

        yield return new WaitForSeconds(blackHoldTime);

        yield return Fade(1f, 0f, fadeInTime);        // เฟดกลับมาสว่าง

        busy = false;
        StartDay();                                   // ข่าว + ราคาของวันใหม่
    }

    // เริ่มวัน: โชว์ข่าว และใช้ราคาของวันนี้ทันที (คูณจำนวนเครื่อง)
    void StartDay()
    {
        ShowNews();

        int change = dailyPrice[dayIndex] * minerCount;

        if (change != 0)
        {
            money += change;
            UpdateMoney();
            ShowPopup(change);
        }

        if (dayIndex == 4)            // วัน 5 คือวันตัดสิน
            EndGame();
    }

    void EndGame()
    {
        gameOver = true;
        if (money >= goalMoney)
            Debug.Log("ชนะ! เงิน: " + money);
        else
            Debug.Log("แพ้ เงิน: " + money);
    }

    // ---------- เฟดจอ ----------
    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetFade(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetFade(to);
    }

    void SetFade(float a)
    {
        if (fadeSprite == null) return;
        Color c = fadeSprite.color;
        c.a = a;
        fadeSprite.color = c;
    }

    // ---------- ข่าว ----------
    void ShowNews()
    {
        if (newsText == null) return;
        if (dayIndex >= dailyNews.Length) return;

        if (newsRoutine != null) StopCoroutine(newsRoutine);
        newsRoutine = StartCoroutine(PlayNews(dailyNews[dayIndex]));
    }

    // หยุดข่าวและล้างข้อความทันที
    void StopNews()
    {
        if (newsRoutine != null)
        {
            StopCoroutine(newsRoutine);
            newsRoutine = null;
        }
        if (newsText != null)
            newsText.text = "";
    }

    // พิมพ์ทีละตัว -> ค้างให้อ่าน -> (ถ้ามีหน้าต่อไปก็ทำซ้ำ) -> เฟดจางหาย
    IEnumerator PlayNews(string line)
    {
        newsText.alpha = 1f;                     // รีเซ็ตให้ชัดเต็ม
        newsText.text = line;
        newsText.maxVisibleCharacters = 99999;   // ให้คำนวณหน้าครบก่อน
        newsText.pageToDisplay = 1;
        newsText.ForceMeshUpdate();

        int pages = newsText.textInfo.pageCount;

        for (int p = 1; p <= pages; p++)
        {
            newsText.pageToDisplay = p;
            int first = newsText.textInfo.pageInfo[p - 1].firstCharacterIndex;
            int last = newsText.textInfo.pageInfo[p - 1].lastCharacterIndex;

            newsText.maxVisibleCharacters = first;   // เริ่มจากหน้าว่าง

            for (int c = first; c <= last; c++)
            {
                newsText.maxVisibleCharacters = c + 1;
                yield return new WaitForSeconds(typeDelay);
            }

            yield return new WaitForSeconds(holdSeconds);   // ค้างให้อ่าน
        }

        // จบทุกหน้าแล้ว เฟดจางหาย
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.deltaTime;
            newsText.alpha = 1f - t / fadeSeconds;
            yield return null;
        }
        newsText.alpha = 0f;
    }

    // ---------- ซื้อ ----------
    public void BuyMiner()
    {
        if (gameOver || busy) return;
        if (money < minerCost) return;

        money -= minerCost;
        minerCount++;
        UpdateMoney();
        SpawnMinerIcon();

        Debug.Log("เครื่องขุด: " + minerCount + "  เงิน: " + money);
    }

    void SpawnMinerIcon()
    {
        float width = minerObject.GetComponent<SpriteRenderer>().bounds.size.x * minerGap;

        Vector3 pos = minerObject.transform.position
                    + Vector3.right * width * (minerCount - 1);

        Instantiate(minerObject, pos, minerObject.transform.rotation,
                    minerObject.transform.parent);
    }

    // ---------- ตัวเลขลอย ----------
    void ShowPopup(int change)
    {
        if (popupTemplate == null) return;

        TMP_Text t = Instantiate(popupTemplate, popupTemplate.transform.parent);
        t.gameObject.SetActive(true);
        t.text = (change > 0 ? "+" : "-") + Mathf.Abs(change).ToString("N0");
        t.color = change > 0 ? Color.green : Color.red;

        StartCoroutine(FloatAndFade(t));
    }

    IEnumerator FloatAndFade(TMP_Text t)
    {
        Vector3 start = t.transform.position;
        Color c = t.color;
        float time = 0f;

        while (time < popupTime)
        {
            time += Time.deltaTime;
            float p = time / popupTime;
            t.transform.position = start + Vector3.up * popupRise * p;
            c.a = 1f - p;
            t.color = c;
            yield return null;
        }

        Destroy(t.gameObject);
    }

    // ---------- UI ----------
    void ShowDay()
    {
        for (int i = 0; i < dayIcons.Length; i++)
            dayIcons[i].SetActive(i == dayIndex);

        for (int i = 0; i < newsImages.Length; i++)
            newsImages[i].SetActive(i == dayIndex);
    }

    void UpdateMoney()
    {
        if (moneyText != null)
            moneyText.text = money.ToString("N0");
    }
}