using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

// เรียงจากซ้ายไปขวา: Left, LeftSub, RightSub, Right (ค่าสองตัวแรกเหมือนเดิม)
public enum Speaker { Left, Right, LeftSub, RightSub }

[Serializable]
public class DialogueLine
{
    public Speaker speaker = Speaker.Right;
    public Sprite expression;
    [TextArea(2, 4)] public string text;
    [Tooltip("ติ๊ก = ชื่อของคนที่พูดบรรทัดนี้เริ่มขึ้นตั้งแต่บรรทัดนี้ (ค้างไว้ ขึ้นเฉพาะตอนเขาพูด)")]
    public bool showName;
}

// ช่องเสริม "ซ้ายย่อย / ขวายย่อย" ของแต่ละบทสนทนา
[Serializable]
public class SubSlots
{
    public bool showLeftSub;
    public bool showRightSub;
    public Sprite leftSubSprite;
    public Sprite rightSubSprite;
    public Sprite leftSubName;
    public Sprite rightSubName;
}

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    public RectTransform bubbleBackground;

    [Header("Portraits (ซ้าย / ขวา)")]
    [FormerlySerializedAs("npcPortrait")] public Image leftPortrait;
    public Image rightPortrait;
    [FormerlySerializedAs("npcSprite")] public Sprite leftSprite;
    public Sprite rightSprite;

    [Header("Sub Portraits (ซ้ายย่อย / ขวาย่อย)")]
    public Image leftSubPortrait;
    public Image rightSubPortrait;

    [Header("Speaker Effect")]
    public Color dimColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public float bounceHeight = 20f;
    public float effectDuration = 0.2f;

    [Header("Typewriter")]
    public float charDelay = 0.03f;

    [Header("Typing Sound")]
    public AudioSource audioSource;
    public AudioClip leftBlip;
    public AudioClip rightBlip;
    public AudioClip leftSubBlip;     // ว่าง = ใช้เสียงฝั่งซ้าย
    public AudioClip rightSubBlip;    // ว่าง = ใช้เสียงฝั่งขวา
    [Range(0f, 1f)] public float blipVolume = 0.6f;
    public Vector2 leftPitchRange = new Vector2(0.95f, 1.1f);
    public Vector2 rightPitchRange = new Vector2(1.2f, 1.4f);
    public Vector2 leftSubPitchRange = new Vector2(0.8f, 0.9f);
    public Vector2 rightSubPitchRange = new Vector2(1.5f, 1.65f);
    public int blipEveryNChars = 2;

    [Header("Background Dim")]
    public Image dimOverlay;
    [Range(0f, 1f)] public float dimAmount = 0.6f;
    public float dimFadeTime = 0.3f;

    [Header("Name Sprites")]
    public Image leftNameImage;
    public Image rightNameImage;
    public Image leftSubNameImage;    // ตำแหน่งเดียวกับชื่อซ้ายได้ เพราะขึ้นทีละคน
    public Image rightSubNameImage;   // ตำแหน่งเดียวกับชื่อขวาได้
    public float nameFadeTime = 0.15f;

    private const int SlotCount = 4;   // 0=Left 1=Right 2=LeftSub 3=RightSub

    private Image[] portraits = new Image[SlotCount];
    private Image[] nameImages = new Image[SlotCount];
    private bool[] slotActive = new bool[SlotCount];
    private Vector2[] basePos = new Vector2[SlotCount];
    private Sprite[] slotNameSprite = new Sprite[SlotCount];
    private Coroutine[] portraitRoutines = new Coroutine[SlotCount];
    private Coroutine[] nameRoutines = new Coroutine[SlotCount];
    private readonly HashSet<Sprite> revealedNames = new HashSet<Sprite>();

    private List<DialogueLine> currentLines;
    private int currentIndex;
    private Action onDialogueFinished;

    private bool isTyping;
    private Coroutine typingRoutine;
    private Coroutine dimRoutine;

    void Awake()
    {
        portraits[0] = leftPortrait;
        portraits[1] = rightPortrait;
        portraits[2] = leftSubPortrait;
        portraits[3] = rightSubPortrait;

        nameImages[0] = leftNameImage;
        nameImages[1] = rightNameImage;
        nameImages[2] = leftSubNameImage;
        nameImages[3] = rightSubNameImage;

        for (int i = 0; i < SlotCount; i++)
        {
            if (portraits[i] != null) basePos[i] = portraits[i].rectTransform.anchoredPosition;
        }
    }

    void Start()
    {
        dialoguePanel.SetActive(false);

        for (int i = 0; i < SlotCount; i++)
        {
            if (portraits[i] != null) portraits[i].gameObject.SetActive(false);
            HideNameNow(nameImages[i]);
        }

        if (dimOverlay != null)
        {
            Color c = dimOverlay.color;
            c.a = 0f;
            dimOverlay.color = c;
            dimOverlay.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (dialoguePanel.activeSelf &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            NextLine();
        }
    }

    // แบบเดิม: ข้อความล้วน ตัวละครซ้ายคนเดียวพูด
    public void StartDialogue(List<string> lines, Action onFinished, Sprite portrait = null)
    {
        if (lines == null || lines.Count == 0) return;

        var converted = new List<DialogueLine>();
        foreach (string s in lines)
            converted.Add(new DialogueLine { speaker = Speaker.Left, text = s });

        StartDialogue(converted, onFinished, true, false, portrait, null);
    }

    // แบบเต็ม: โชว์ได้สูงสุดสี่ช่อง (ซ้าย ขวา และช่องย่อยผ่านพารามิเตอร์ sub)
    public void StartDialogue(
        List<DialogueLine> lines,
        Action onFinished,
        bool showLeft = true,
        bool showRight = true,
        Sprite leftOverride = null,
        Sprite rightOverride = null,
        Sprite leftName = null,
        Sprite rightName = null,
        SubSlots sub = null)
    {
        if (lines == null || lines.Count == 0) return;

        currentLines = lines;
        currentIndex = 0;
        onDialogueFinished = onFinished;

        Sprite l = leftOverride != null ? leftOverride : leftSprite;
        Sprite r = rightOverride != null ? rightOverride : (rightSprite != null ? rightSprite : l);

        slotActive[0] = showLeft;
        slotActive[1] = showRight;
        slotActive[2] = sub != null && sub.showLeftSub;
        slotActive[3] = sub != null && sub.showRightSub;

        SetupPortrait(0, l);
        SetupPortrait(1, r);
        SetupPortrait(2, sub != null ? sub.leftSubSprite : null);
        SetupPortrait(3, sub != null ? sub.rightSubSprite : null);

        slotNameSprite[0] = leftName;
        slotNameSprite[1] = rightName;
        slotNameSprite[2] = sub != null ? sub.leftSubName : null;
        slotNameSprite[3] = sub != null ? sub.rightSubName : null;

        for (int i = 0; i < SlotCount; i++) HideNameNow(nameImages[i]);

        dialoguePanel.SetActive(true);
        SetDim(true);
        ShowLine();
    }

    void SetupPortrait(int i, Sprite sprite)
    {
        Image img = portraits[i];
        if (img == null) return;

        img.gameObject.SetActive(slotActive[i]);
        if (!slotActive[i]) return;

        img.sprite = sprite;
        img.color = dimColor;
        img.rectTransform.anchoredPosition = basePos[i];
    }

    void ShowLine()
    {
        DialogueLine line = currentLines[currentIndex];
        int speakerIndex = (int)line.speaker;

        SetBubbleSide(line.speaker);
        ApplyExpression(line, speakerIndex);
        UpdateNames(line, speakerIndex);

        for (int i = 0; i < SlotCount; i++)
        {
            if (slotActive[i] && portraits[i] != null)
                AnimatePortrait(i, i == speakerIndex);
        }

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeText(line.text, line.speaker));
    }

    // ----- บับเบิล -----
    void SetBubbleSide(Speaker speaker)
    {
        if (bubbleBackground == null) return;

        bool leftSide = speaker == Speaker.Left || speaker == Speaker.LeftSub;
        float flip = leftSide ? 1f : -1f;

        bubbleBackground.localScale = new Vector3(flip, 1f, 1f);

        if (dialogueText != null && dialogueText.transform.IsChildOf(bubbleBackground))
        {
            dialogueText.rectTransform.localScale = new Vector3(flip, 1f, 1f);
        }
    }

    // ----- สีหน้า -----
    void ApplyExpression(DialogueLine line, int idx)
    {
        if (line.expression == null) return;

        if (slotActive[idx] && portraits[idx] != null)
            portraits[idx].sprite = line.expression;
    }

    // ----- ภาพตัวละคร: คนพูดสว่างและเด้ง คนอื่นมืด -----
    void AnimatePortrait(int i, bool speaking)
    {
        if (portraitRoutines[i] != null) StopCoroutine(portraitRoutines[i]);
        portraitRoutines[i] = StartCoroutine(PortraitEffect(portraits[i], basePos[i], speaking));
    }

    IEnumerator PortraitEffect(Image img, Vector2 pos, bool isSpeaking)
    {
        RectTransform rt = img.rectTransform;
        Color startColor = img.color;
        Color targetColor = isSpeaking ? Color.white : dimColor;

        rt.anchoredPosition = pos;

        float t = 0f;
        while (t < effectDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / effectDuration);

            img.color = Color.Lerp(startColor, targetColor, p);

            float offset = isSpeaking ? Mathf.Sin(p * Mathf.PI) * bounceHeight : 0f;
            rt.anchoredPosition = pos + new Vector2(0f, offset);

            yield return null;
        }

        img.color = targetColor;
        rt.anchoredPosition = pos;
    }

    // ----- ชื่อตัวละคร -----
    void UpdateNames(DialogueLine line, int speakerIndex)
    {
        if (line.showName)
        {
            Sprite s = slotNameSprite[speakerIndex];
            if (s != null) revealedNames.Add(s);
        }

        for (int i = 0; i < SlotCount; i++)
        {
            ShowName(i, slotActive[i] && i == speakerIndex);
        }
    }

    void ShowName(int i, bool speakingNow)
    {
        Image img = nameImages[i];
        if (img == null) return;

        Sprite sprite = slotNameSprite[i];
        bool visible = speakingNow && sprite != null && revealedNames.Contains(sprite);

        if (nameRoutines[i] != null) StopCoroutine(nameRoutines[i]);
        if (visible) img.sprite = sprite;

        nameRoutines[i] = StartCoroutine(NameFade(img, visible));
    }

    IEnumerator NameFade(Image img, bool show)
    {
        if (show) img.gameObject.SetActive(true);
        else if (!img.gameObject.activeSelf) yield break;

        Color c = img.color;
        float start = c.a;
        float target = show ? 1f : 0f;
        float t = 0f;

        while (t < nameFadeTime)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(start, target, Mathf.Clamp01(t / nameFadeTime));
            img.color = c;
            yield return null;
        }

        c.a = target;
        img.color = c;

        if (!show) img.gameObject.SetActive(false);
    }

    void HideNameNow(Image img)
    {
        if (img == null) return;

        Color c = img.color;
        c.a = 0f;
        img.color = c;
        img.gameObject.SetActive(false);
    }

    // ----- พิมพ์ทีละตัวอักษร + เสียง -----
    IEnumerator TypeText(string text, Speaker speaker)
    {
        isTyping = true;

        dialogueText.text = text;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int total = dialogueText.textInfo.characterCount;

        Vector2 pitch;
        AudioClip clip = GetBlip(speaker, out pitch);

        for (int i = 1; i <= total; i++)
        {
            dialogueText.maxVisibleCharacters = i;

            char c = dialogueText.textInfo.characterInfo[i - 1].character;
            bool silent = char.IsWhiteSpace(c)
                          || char.IsPunctuation(c)
                          || char.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark;

            if (!silent && i % Mathf.Max(1, blipEveryNChars) == 0)
                PlayBlip(clip, pitch);

            yield return new WaitForSeconds(charDelay);
        }

        isTyping = false;
    }

    AudioClip GetBlip(Speaker s, out Vector2 pitch)
    {
        switch (s)
        {
            case Speaker.Right:
                pitch = rightPitchRange;
                return rightBlip != null ? rightBlip : leftBlip;

            case Speaker.LeftSub:
                pitch = leftSubPitchRange;
                return leftSubBlip != null ? leftSubBlip : leftBlip;

            case Speaker.RightSub:
                pitch = rightSubPitchRange;
                return rightSubBlip != null ? rightSubBlip : (rightBlip != null ? rightBlip : leftBlip);

            default:
                pitch = leftPitchRange;
                return leftBlip;
        }
    }

    void PlayBlip(AudioClip clip, Vector2 pitchRange)
    {
        if (audioSource == null || clip == null) return;

        audioSource.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, blipVolume);
    }

    void FinishTyping()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);

        dialogueText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
    }

    // ----- หรี่ฉาก -----
    void SetDim(bool on)
    {
        if (dimOverlay == null) return;

        if (dimRoutine != null) StopCoroutine(dimRoutine);
        dimRoutine = StartCoroutine(DimRoutine(on ? dimAmount : 0f));
    }

    IEnumerator DimRoutine(float targetAlpha)
    {
        dimOverlay.gameObject.SetActive(true);

        Color c = dimOverlay.color;
        float startAlpha = c.a;
        float t = 0f;

        while (t < dimFadeTime)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(t / dimFadeTime));
            dimOverlay.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        dimOverlay.color = c;

        if (targetAlpha <= 0f) dimOverlay.gameObject.SetActive(false);
    }

    // ----- ไปบรรทัดถัดไป / จบ -----
    public void NextLine()
    {
        if (isTyping)
        {
            FinishTyping();
            return;
        }

        currentIndex++;

        if (currentIndex >= currentLines.Count)
        {
            EndDialogue();
            return;
        }

        ShowLine();
    }

    void EndDialogue()
    {
        StopAllCoroutines();
        SetDim(false);
        isTyping = false;

        dialoguePanel.SetActive(false);

        for (int i = 0; i < SlotCount; i++)
        {
            if (portraits[i] != null) portraits[i].gameObject.SetActive(false);
            HideNameNow(nameImages[i]);
        }

        Action finished = onDialogueFinished;
        onDialogueFinished = null;
        currentLines = null;

        finished?.Invoke();
    }
}