using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// ใส่สคริปต์นี้ที่ bubble command_0 (ต้องมี Collider2D ด้วย)
/// คลิกซ้ายที่กล่อง -> พิมพ์ W A S D ได้ (แสดงเป็น ↑ ← ↓ →)
/// กด Enter หรือคลิกปุ่ม enter -> ยืนยัน (ด่านละครั้งเดียว) แล้ว blob เดินตามคำสั่ง
/// คลิกปุ่ม X -> พับแผงลง / คลิกแถบที่เหลือ -> กางกลับ
[RequireComponent(typeof(Collider2D))]
public class CommandInputBox : MonoBehaviour
{
    [SerializeField] private TMP_Text commandText;
    [SerializeField] private BlobRunner runner;
    [SerializeField] private Collider2D enterButton;
    [SerializeField] private int maxCommands = 20;
    [SerializeField] private bool showCursor = true;
    [SerializeField] private bool clearAfterRun = true;

    [Header("Collapse")]
    [SerializeField] private Collider2D closeButton;
    [SerializeField] private Transform[] moveWithPanel;
    [SerializeField] private float peekHeight = 0.35f;
    [SerializeField] private float slideTime = 0.25f;

    public IReadOnlyList<char> Commands => commands;

    private readonly List<char> commands = new List<char>();
    private Collider2D box;
    private bool isTyping;
    private bool used;   // กด enter ไปแล้วในด่านนี้

    // ระบบพับ/กาง
    private bool collapsed;
    private float amount;
    private float slideDistance;
    private Vector3 panelShownPos;
    private Vector3[] partShownPos;
    private Coroutine slideRoutine;

    private bool Locked => runner != null && runner.IsRunning;

    private void Awake()
    {
        box = GetComponent<Collider2D>();

        panelShownPos = transform.position;
        slideDistance = Mathf.Max(0f, box.bounds.size.y - peekHeight);

        partShownPos = new Vector3[moveWithPanel != null ? moveWithPanel.Length : 0];
        for (int i = 0; i < partShownPos.Length; i++)
            if (moveWithPanel[i] != null) partShownPos[i] = moveWithPanel[i].position;

        RefreshText();
    }

    private void Update()
    {
        if (LeftClickedThisFrame() && Camera.main != null)
        {
            Vector2 world = Camera.main.ScreenToWorldPoint(MousePosition());

            if (collapsed)
            {
                if (Hit(box, world) || Hit(enterButton, world) || Hit(closeButton, world))
                {
                    SetCollapsed(false);
                    isTyping = true;
                }
            }
            else if (Hit(closeButton, world))
            {
                SetCollapsed(true);
            }
            else if (Hit(enterButton, world))
            {
                Confirm();
            }
            else
            {
                isTyping = box.OverlapPoint(world);
            }
        }

        if (!collapsed && EnterPressedThisFrame()) Confirm();

#if !ENABLE_INPUT_SYSTEM
        if (isTyping) foreach (char c in Input.inputString) OnCharTyped(c);
#endif

        RefreshText();
    }

#if ENABLE_INPUT_SYSTEM
    private void OnEnable()
    {
        if (Keyboard.current != null) Keyboard.current.onTextInput += OnCharTyped;
    }

    private void OnDisable()
    {
        if (Keyboard.current != null) Keyboard.current.onTextInput -= OnCharTyped;
    }
#endif

    private static bool Hit(Collider2D col, Vector2 world) => col != null && col.OverlapPoint(world);

    // ---------- พับ / กาง ----------
    private void SetCollapsed(bool value)
    {
        collapsed = value;
        if (value) isTyping = false;

        if (slideRoutine != null) StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(Slide(value ? 1f : 0f));
    }

    private IEnumerator Slide(float target)
    {
        while (!Mathf.Approximately(amount, target))
        {
            amount = Mathf.MoveTowards(amount, target, Time.deltaTime / Mathf.Max(0.01f, slideTime));
            ApplyOffset();
            yield return null;
        }
        amount = target;
        ApplyOffset();
        slideRoutine = null;
    }

    private void ApplyOffset()
    {
        Vector3 offset = Vector3.down * slideDistance * Mathf.SmoothStep(0f, 1f, amount);

        transform.position = panelShownPos + offset;
        for (int i = 0; i < partShownPos.Length; i++)
            if (moveWithPanel[i] != null) moveWithPanel[i].position = partShownPos[i] + offset;

        Physics2D.SyncTransforms();
    }

    // ---------- คำสั่ง ----------
    private void Confirm()
    {
        if (runner == null || Locked || used || commands.Count == 0) return;

        used = true;   // ด่านนี้กดได้ครั้งเดียว

        runner.Run(new List<char>(commands), () =>
        {
            if (clearAfterRun) commands.Clear();
        });
    }

    private void OnCharTyped(char c)
    {
        if (!isTyping || Locked || used) return;

        if (c == '\b')
        {
            if (commands.Count > 0) commands.RemoveAt(commands.Count - 1);
            return;
        }

        char k = char.ToLower(c);
        if ((k == 'w' || k == 'a' || k == 's' || k == 'd') && commands.Count < maxCommands)
            commands.Add(k);
    }

    private void RefreshText()
    {
        if (commandText == null) return;

        var sb = new StringBuilder();
        foreach (char k in commands) sb.Append(ToArrow(k));

        if (showCursor && isTyping && !Locked && !used && Mathf.Repeat(Time.time, 1f) < 0.5f)
            sb.Append('_');

        commandText.text = sb.ToString();
    }

    private static char ToArrow(char k)
    {
        switch (k)
        {
            case 'w': return '↑';
            case 'a': return '←';
            case 's': return '↓';
            default: return '→';
        }
    }

    public void ClearCommands() => commands.Clear();

    public void ResetForNewLevel()
    {
        commands.Clear();
        used = false;
    }

    private static bool LeftClickedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private static bool EnterPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
    }

    private static Vector3 MousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current.position.ReadValue();
#else
        return Input.mousePosition;
#endif
    }
}