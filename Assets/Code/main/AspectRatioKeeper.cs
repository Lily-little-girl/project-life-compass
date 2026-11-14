using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectRatioKeeper : MonoBehaviour
{
    public float targetAspect = 16f / 9f;   // สัดส่วนที่ออกแบบเกมไว้ (กว้าง ÷ สูง)

    private Camera cam;
    private int lastWidth, lastHeight;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    void Update()
    {
        // ผู้เล่นย่อ/ขยายหน้าต่าง หรือเปลี่ยนความละเอียด ก็คำนวณใหม่
        if (Screen.width != lastWidth || Screen.height != lastHeight) Apply();
    }

    void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;   // ตอนย่อหน้าต่าง

        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float windowAspect = (float)Screen.width / Screen.height;
        float scale = windowAspect / targetAspect;

        Rect r;

        if (scale < 1f)
        {
            // จอแคบ/สูงกว่าเป้าหมาย: แถบดำบนและล่าง
            r = new Rect(0f, (1f - scale) / 2f, 1f, scale);
        }
        else
        {
            // จอกว้างกว่าเป้าหมาย: แถบดำซ้ายและขวา
            float w = 1f / scale;
            r = new Rect((1f - w) / 2f, 0f, w, 1f);
        }

        cam.rect = r;
    }
}