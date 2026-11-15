using UnityEngine;

public class SlidePanel : MonoBehaviour
{
    [Header("ปุ่ม (GameObject ลูกที่มี Collider2D)")]
    public Collider2D closeButton;
    public Collider2D doneButton;

    [Header("การเลื่อน")]
    public float peekAmount = 0.5f;
    public float slideSpeed = 8f;

    public bool IsShown { get { return isShown; } }
    public bool DonePressed { get; private set; }

    private Collider2D panelCollider;
    private Vector3 shownPos;
    private Vector3 hiddenPos;
    private bool isShown = true;

    void Awake()
    {
        panelCollider = GetComponent<Collider2D>();
        shownPos = transform.position;

        float width = panelCollider.bounds.size.x;
        hiddenPos = shownPos - new Vector3(width - peekAmount, 0f, 0f);
    }

    void OnEnable()
    {
        isShown = true;
        DonePressed = false;
        transform.position = shownPos;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouse = GetMouseWorldPos();

            if (isShown)
            {
                if (closeButton != null && closeButton.OverlapPoint(mouse))
                    isShown = false;
                else if (doneButton != null && doneButton.OverlapPoint(mouse))
                    DonePressed = true;
            }
            else
            {
                if (panelCollider.OverlapPoint(mouse))
                    isShown = true;
            }
        }

        Vector3 target = isShown ? shownPos : hiddenPos;
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * slideSpeed);
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