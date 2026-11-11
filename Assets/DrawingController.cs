using UnityEngine;
using System.Collections.Generic;

public class DrawingController : MonoBehaviour
{
    enum Tool { None, Pencil, Eraser }

    [System.Serializable]
    public class ColorButton
    {
        public Collider2D collider;
        public Color color = Color.black;
    }

    [Header("Line Settings")]
    public GameObject linePrefab;
    public float minDistance = 0.05f;

    [Header("Tools")]
    public Collider2D pencilCollider;
    public Collider2D eraserCollider;

    [Header("Selected Highlight")]
    public float selectedScale = 1.2f;

    [Header("Colors")]
    public ColorButton[] colorButtons;

    [Header("Opacity Slider (ความจาง)")]
    public Collider2D opacityHandleCollider;  // ตัวเลื่อน (รูปแรก)
    public SpriteRenderer opacityTrack;       // แถบ (รูปที่สอง)

    [Header("Brush Size (คลิกขวาเพื่อวนขนาด)")]
    public float[] brushSizes = { 0.03f, 0.06f, 0.1f, 0.16f, 0.25f };
    public int sizeIndex = 1;

    [Header("Drawing Area")]
    public Collider2D paperCollider;

    private Tool currentTool = Tool.None;
    private Color baseColor = Color.black;
    private float opacity = 1f;
    private LineRenderer currentLine;
    private Vector3 lastPos;
    private Vector3 lastEraserPos;
    private bool strokeActive = false;
    private List<LineRenderer> lines = new List<LineRenderer>();

    private Vector3 pencilBaseScale = Vector3.one;
    private Vector3 eraserBaseScale = Vector3.one;

    // สไลเดอร์
    private bool draggingSlider = false;
    private float handleBottomY;
    private float handleTopY;
    private float handleCenterY;
    private float grabOffsetY;

    float BrushWidth { get { return brushSizes[sizeIndex]; } }
    float EraserRadius { get { return Mathf.Max(0.1f, BrushWidth * 2f); } }
    Color DrawColor { get { return new Color(baseColor.r, baseColor.g, baseColor.b, opacity); } }

    void Start()
    {
        if (pencilCollider != null) pencilBaseScale = pencilCollider.transform.localScale;
        if (eraserCollider != null) eraserBaseScale = eraserCollider.transform.localScale;

        if (opacityHandleCollider != null && opacityTrack != null)
        {
            handleCenterY = opacityHandleCollider.bounds.center.y;
            handleBottomY = handleCenterY;  // ตำแหน่งเริ่มต้น = ล่างสุด = 100%
            handleTopY = opacityTrack.bounds.max.y - opacityHandleCollider.bounds.extents.y;

            if (handleTopY <= handleBottomY)
                Debug.LogWarning("ช่วงเลื่อนของแถบความจางผิดปกติ เช็กตำแหน่งตัวเลื่อนและแถบ");
        }
    }

    void Update()
    {
        UpdateToolHighlight();

        Vector3 mouseWorld = GetMouseWorldPos();

        // คลิกขวา = เปลี่ยนขนาด
        if (Input.GetMouseButtonDown(1))
        {
            sizeIndex = (sizeIndex + 1) % brushSizes.Length;
            Debug.Log("ขนาดที่ " + (sizeIndex + 1) + "/" + brushSizes.Length);
        }

        // กำลังลากตัวเลื่อนความจาง
        if (draggingSlider)
        {
            if (Input.GetMouseButton(0))
            {
                DragSlider(mouseWorld);
            }
            else
            {
                draggingSlider = false;
                Debug.Log("ความเข้ม " + Mathf.RoundToInt(opacity * 100f) + "%");
            }
            return;
        }

        // กดเมาส์ซ้าย
        if (Input.GetMouseButtonDown(0))
        {
            strokeActive = false;

            if (pencilCollider != null && pencilCollider.OverlapPoint(mouseWorld))
            {
                currentTool = Tool.Pencil;
                Debug.Log("เลือกดินสอแล้ว");
                return;
            }
            if (eraserCollider != null && eraserCollider.OverlapPoint(mouseWorld))
            {
                currentTool = Tool.Eraser;
                Debug.Log("เลือกยางลบแล้ว");
                return;
            }
            if (TryPickColor(mouseWorld))
            {
                currentTool = Tool.Pencil;
                return;
            }
            if (opacityHandleCollider != null && opacityHandleCollider.OverlapPoint(mouseWorld))
            {
                draggingSlider = true;
                grabOffsetY = mouseWorld.y - handleCenterY;
                return;
            }

            if (IsInsidePaper(mouseWorld))
            {
                if (currentTool == Tool.Pencil)
                {
                    strokeActive = true;
                    StartLine(mouseWorld);
                }
                else if (currentTool == Tool.Eraser)
                {
                    strokeActive = true;
                    lastEraserPos = mouseWorld;
                    EraseAt(mouseWorld);
                }
            }
        }

        // กดค้าง
        if (Input.GetMouseButton(0) && strokeActive)
        {
            bool inside = IsInsidePaper(mouseWorld);

            if (currentTool == Tool.Pencil)
            {
                if (inside)
                {
                    if (currentLine == null)
                        StartLine(mouseWorld);
                    else if (Vector3.Distance(mouseWorld, lastPos) > minDistance)
                        AddPoint(mouseWorld);
                }
                else
                {
                    currentLine = null;
                }
            }
            else if (currentTool == Tool.Eraser)
            {
                if (inside)
                    EraseAlong(lastEraserPos, mouseWorld);
                lastEraserPos = mouseWorld;
            }
        }

        // ปล่อยเมาส์
        if (Input.GetMouseButtonUp(0))
        {
            currentLine = null;
            strokeActive = false;
        }
    }

    // ---------- สไลเดอร์ความจาง ----------
    void DragSlider(Vector3 mouse)
    {
        if (handleTopY <= handleBottomY) return;

        float target = Mathf.Clamp(mouse.y - grabOffsetY, handleBottomY, handleTopY);
        opacityHandleCollider.transform.position += new Vector3(0f, target - handleCenterY, 0f);
        handleCenterY = target;

        // ล่างสุด = 1 (เข้มสุด), บนสุด = 0 (จางสุด)
        opacity = 1f - Mathf.InverseLerp(handleBottomY, handleTopY, target);
    }

    // ---------- ไฮไลต์เครื่องมือที่เลือก ----------
    void UpdateToolHighlight()
    {
        float t = Time.deltaTime * 12f;

        if (pencilCollider != null)
        {
            Vector3 target = pencilBaseScale * (currentTool == Tool.Pencil ? selectedScale : 1f);
            pencilCollider.transform.localScale =
                Vector3.Lerp(pencilCollider.transform.localScale, target, t);
        }
        if (eraserCollider != null)
        {
            Vector3 target = eraserBaseScale * (currentTool == Tool.Eraser ? selectedScale : 1f);
            eraserCollider.transform.localScale =
                Vector3.Lerp(eraserCollider.transform.localScale, target, t);
        }
    }

    // ---------- สี ----------
    bool TryPickColor(Vector3 pos)
    {
        if (colorButtons == null) return false;
        foreach (ColorButton b in colorButtons)
        {
            if (b != null && b.collider != null && b.collider.OverlapPoint(pos))
            {
                baseColor = b.color;
                Debug.Log("เปลี่ยนสีแล้ว: " + baseColor);
                return true;
            }
        }
        return false;
    }

    // ---------- วาด ----------
    void StartLine(Vector3 pos)
    {
        List<Vector3> pts = new List<Vector3> { pos };
        currentLine = CreateLine(pts, DrawColor, BrushWidth);
        lastPos = pos;
    }

    void AddPoint(Vector3 pos)
    {
        float dist = Vector3.Distance(lastPos, pos);
        int steps = Mathf.CeilToInt(dist / minDistance);
        for (int i = 1; i <= steps; i++)
        {
            Vector3 p = Vector3.Lerp(lastPos, pos, (float)i / steps);
            currentLine.positionCount++;
            currentLine.SetPosition(currentLine.positionCount - 1, p);
        }
        lastPos = pos;
    }

    LineRenderer CreateLine(List<Vector3> points, Color color, float width)
    {
        GameObject obj = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
        LineRenderer lr = obj.GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.widthMultiplier = 1f;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.startColor = color;
        lr.endColor = color;
        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());
        lines.Add(lr);
        return lr;
    }

    // ---------- ยางลบ ----------
    void EraseAlong(Vector3 from, Vector3 to)
    {
        float dist = Vector3.Distance(from, to);
        float step = EraserRadius * 0.5f;
        int steps = Mathf.Max(1, Mathf.CeilToInt(dist / step));
        for (int i = 1; i <= steps; i++)
        {
            EraseAt(Vector3.Lerp(from, to, (float)i / steps));
        }
    }

    void EraseAt(Vector3 pos)
    {
        float r = EraserRadius;

        for (int i = lines.Count - 1; i >= 0; i--)
        {
            LineRenderer line = lines[i];
            if (line == null)
            {
                lines.RemoveAt(i);
                continue;
            }

            bool hit = false;
            int n = line.positionCount;
            for (int p = 0; p < n; p++)
            {
                if (Vector2.Distance(line.GetPosition(p), pos) <= r)
                {
                    hit = true;
                    break;
                }
            }
            if (!hit) continue;

            List<List<Vector3>> segments = new List<List<Vector3>>();
            List<Vector3> current = new List<Vector3>();
            for (int p = 0; p < n; p++)
            {
                Vector3 pt = line.GetPosition(p);
                if (Vector2.Distance(pt, pos) <= r)
                {
                    if (current.Count > 0)
                    {
                        segments.Add(current);
                        current = new List<Vector3>();
                    }
                }
                else
                {
                    current.Add(pt);
                }
            }
            if (current.Count > 0) segments.Add(current);

            Color c = line.startColor;
            float w = line.startWidth;

            Destroy(line.gameObject);
            lines.RemoveAt(i);

            foreach (List<Vector3> seg in segments)
            {
                if (seg.Count >= 2) CreateLine(seg, c, w);
            }
        }
    }

    // ---------- อื่นๆ ----------
    bool IsInsidePaper(Vector3 pos)
    {
        if (paperCollider == null) return true;
        return paperCollider.OverlapPoint(pos);
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = -Camera.main.transform.position.z;
        Vector3 world = Camera.main.ScreenToWorldPoint(mouse);
        world.z = 0f;
        return world;
    }
}