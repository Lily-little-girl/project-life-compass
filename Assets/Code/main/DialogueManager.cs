using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

public enum Speaker { Left, Right }

[Serializable]
public class DialogueLine
{
    public Speaker speaker = Speaker.Right;
    public Sprite expression;
    [TextArea(2, 4)] public string text;
    [Tooltip("ติ๊ก =  ชื่อของคนที่พูดบรรทัดนี้้เริ่มตั้งแต่บรรทัดนี้ (ค้างไว้ ขึ้นเฉพาะตอนเขาพูด)")]
    public bool showName;
}

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    public RectTransform bubbleBackground;

    [Header("Portraits")]
    [FormerlySerializedAs("npcPortrait")] public Image leftPortrait;   // ตัวละครแรก (ซ้าย)
    public Image rightPortrait;                                        // ตัวละครที่สอง (ขวา)
    [FormerlySerializedAs("npcSprite")] public Sprite leftSprite;      // ภาพเริ่มต้นฝั่งซ้าย
    public Sprite rightSprite;                                         // ภาพเริ่มต้นฝั่งขวา (ว่างได้ จะใช้ภาพซ้ายแทน)

    [Header("Speaker Effect")]
    public Color dimColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public float bounceHeight = 20f;
    public float effectDuration = 0.2f;

    [Header("Typewriter")]
    public float charDelay = 0.03f;

    [Header("Typing Sound")]
public AudioSource audioSource;
public AudioClip leftBlip;                 // เสียงตัวละครซ้าย
public AudioClip rightBlip;                // เสียงตัวละครขวา (ว่าง = ใช้เสียงซ้าย)
[Range(0f, 1f)] public float blipVolume = 0.6f;
public Vector2 leftPitchRange = new Vector2(0.95f, 1.1f);
public Vector2 rightPitchRange = new Vector2(1.2f, 1.4f);
public int blipEveryNChars = 2;            // เล่นเสียงทุกกี่ตัวอักษร

[Header("Background Dim")]
public Image dimOverlay;                 // ภาพดำเต็มจอ อยู่ใต้ภาพตัวละครและกล่องข้อความ
[Range(0f, 1f)] public float dimAmount = 0.6f;
public float dimFadeTime = 0.3f;

[Header("Name Sprites")]
public Image leftNameImage;      // Image ชื่อฝั่งซ้าย
public Image rightNameImage;     // Image ชื่อฝั่งขวา
public float nameFadeTime = 0.15f;

private Sprite leftNameSprite, rightNameSprite;
private readonly HashSet<Sprite> revealedNames = new HashSet<Sprite>();
private Coroutine leftNameRoutine, rightNameRoutine;

private Coroutine dimRoutine;

    private List<DialogueLine> currentLines;
    private int currentIndex;
    private Action onDialogueFinished;

    private bool leftActive, rightActive;
    private bool isTyping;
    private Coroutine typingRoutine;
    private Vector2 leftBasePos, rightBasePos;
    private Coroutine leftRoutine, rightRoutine;

    void Awake()
    {
        if (leftPortrait != null) leftBasePos = leftPortrait.rectTransform.anchoredPosition;
        if (rightPortrait != null) rightBasePos = rightPortrait.rectTransform.anchoredPosition;
    }

    void Start()
    {
        dialoguePanel.SetActive(false);
        if (leftPortrait != null) leftPortrait.gameObject.SetActive(false);
        if (rightPortrait != null) rightPortrait.gameObject.SetActive(false);
        if (dimOverlay != null)

        
{
    Color c = dimOverlay.color;
    c.a = 0f;
    dimOverlay.color = c;
    dimOverlay.gameObject.SetActive(false);
}
HideNameNow(leftNameImage);
        HideNameNow(rightNameImage);
    }

    void SetBubbleSide(Speaker speaker)
{
    if (bubbleBackground == null) return;

    // รูปบับเบิลเดิมชี้ซ้าย: Left = ปกติ, Right = กลับด้าน
    float flip = speaker == Speaker.Left ? 1f : -1f;

    bubbleBackground.localScale = new Vector3(flip, 1f, 1f);

    // ถ้าข้อความเป็นลูกของบับเบิล ต้องกลับซ้ำอีกรอบให้ตัวอักษรไม่กลับด้าน
    if (dialogueText != null && dialogueText.transform.IsChildOf(bubbleBackground))
    {
        dialogueText.rectTransform.localScale = new Vector3(flip, 1f, 1f);
    }
}

void ApplyExpression(DialogueLine line)
{
    if (line.expression == null) return;

    if (line.speaker == Speaker.Left && leftActive && leftPortrait != null)
        leftPortrait.sprite = line.expression;
    else if (line.speaker == Speaker.Right && rightActive && rightPortrait != null)
        rightPortrait.sprite = line.expression;
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

    // แบบเดิม: ข้อความล้วน ตัวละครซ้ายคนเดียวพูด (ใช้กับตัวละครแรกได้เลย ไม่ต้องแก้)
    public void StartDialogue(List<string> lines, Action onFinished, Sprite portrait = null)
    {
        if (lines == null || lines.Count == 0) return;

        var converted = new List<DialogueLine>();
        foreach (string s in lines)
            converted.Add(new DialogueLine { speaker = Speaker.Left, text = s });

        StartDialogue(converted, onFinished, true, false, portrait, null);
    }

    // แบบใหม่: บอกได้ว่าแต่ละบรรทัดใครพูด และจะโชว์ภาพฝั่งไหนบ้าง
    public void StartDialogue(
    List<DialogueLine> lines,
    Action onFinished,
    bool showLeft = true,
    bool showRight = true,
    Sprite leftOverride = null,
    Sprite rightOverride = null,
    Sprite leftName = null,
    Sprite rightName = null)
{
    if (lines == null || lines.Count == 0) return;

    currentLines = lines;
    currentIndex = 0;
    onDialogueFinished = onFinished;

    Sprite l = leftOverride != null ? leftOverride : leftSprite;
    Sprite r = rightOverride != null ? rightOverride : (rightSprite != null ? rightSprite : l);

    SetupPortrait(leftPortrait, l, showLeft, leftBasePos);
    SetupPortrait(rightPortrait, r, showRight, rightBasePos);
    leftActive = showLeft;
    rightActive = showRight;

    leftNameSprite = leftName;
    rightNameSprite = rightName;
    HideNameNow(leftNameImage);
    HideNameNow(rightNameImage);

    dialoguePanel.SetActive(true);
    SetDim(true);
    ShowLine();
}

    void SetupPortrait(Image img, Sprite sprite, bool show, Vector2 basePos)
    {
        if (img == null) return;

        img.gameObject.SetActive(show);
        if (!show) return;

        img.sprite = sprite;
        img.color = dimColor;
        img.rectTransform.anchoredPosition = basePos;
    }

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

    void ShowLine()
    {
        DialogueLine line = currentLines[currentIndex];

        SetBubbleSide(line.speaker);
        ApplyExpression(line);
        UpdateNames(line);

        if (leftActive)
            Animate(ref leftRoutine, leftPortrait, leftBasePos, line.speaker == Speaker.Left);
        if (rightActive)
            Animate(ref rightRoutine, rightPortrait, rightBasePos, line.speaker == Speaker.Right);

            if (typingRoutine != null) StopCoroutine(typingRoutine);
            typingRoutine = StartCoroutine(TypeText(line.text, line.speaker));
    }

    void Animate(ref Coroutine routine, Image img, Vector2 basePos, bool isSpeaking)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PortraitEffect(img, basePos, isSpeaking));
    }

    IEnumerator PortraitEffect(Image img, Vector2 basePos, bool isSpeaking)
    {
        RectTransform rt = img.rectTransform;
        Color startColor = img.color;
        Color targetColor = isSpeaking ? Color.white : dimColor;

        rt.anchoredPosition = basePos;

        float t = 0f;
        while (t < effectDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / effectDuration);

            img.color = Color.Lerp(startColor, targetColor, p);

            float offset = isSpeaking ? Mathf.Sin(p * Mathf.PI) * bounceHeight : 0f;
            rt.anchoredPosition = basePos + new Vector2(0f, offset);

            yield return null;
        }

        img.color = targetColor;
        rt.anchoredPosition = basePos;
    }

   IEnumerator TypeText(string text, Speaker speaker)
{
    isTyping = true;

    dialogueText.text = text;
    dialogueText.maxVisibleCharacters = 0;
    dialogueText.ForceMeshUpdate();

    int total = dialogueText.textInfo.characterCount;
   AudioClip clip = (speaker == Speaker.Right && rightBlip != null) ? rightBlip : leftBlip;
Vector2 pitch = speaker == Speaker.Right ? rightPitchRange : leftPitchRange;

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

void PlayBlip(AudioClip clip, Vector2 pitchRange)
{
    if (audioSource == null || clip == null) return;

    audioSource.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
    audioSource.PlayOneShot(clip, blipVolume);
}

void FinishTyping()
{
    if (typingRoutine != null) StopCoroutine(typingRoutine);

    dialogueText.maxVisibleCharacters = int.MaxValue; // โชว์ครบทั้งหมด
    isTyping = false;
}

    public void NextLine()
{
    // ถ้ายังพิมพ์ไม่จบ คลิกแล้วโชว์ทั้งบรรทัดทันที
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

void UpdateNames(DialogueLine line)
{
    // บรรทัดที่ติ๊ก Show Name: ชื่อของคนที่พูดบรรทัดนี้ถูกเปิดตั้งแต่ตรงนี้
    if (line.showName)
    {
        Sprite s = line.speaker == Speaker.Left ? leftNameSprite : rightNameSprite;
        if (s != null) revealedNames.Add(s);
    }

    ShowName(ref leftNameRoutine, leftNameImage, leftNameSprite,
             leftActive && line.speaker == Speaker.Left);
    ShowName(ref rightNameRoutine, rightNameImage, rightNameSprite,
             rightActive && line.speaker == Speaker.Right);
}

void ShowName(ref Coroutine routine, Image img, Sprite sprite, bool speakingNow)
{
    if (img == null) return;

    bool visible = speakingNow && sprite != null && revealedNames.Contains(sprite);

    if (routine != null) StopCoroutine(routine);
    if (visible) img.sprite = sprite;

    routine = StartCoroutine(NameFade(img, visible));
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

    void EndDialogue()
    {
        StopAllCoroutines();
        SetDim(false);
        HideNameNow(leftNameImage);
        HideNameNow(rightNameImage);

        isTyping = false;

        dialoguePanel.SetActive(false);
        if (leftPortrait != null) leftPortrait.gameObject.SetActive(false);
        if (rightPortrait != null) rightPortrait.gameObject.SetActive(false);

        Action finished = onDialogueFinished;
        onDialogueFinished = null;
        currentLines = null;

        finished?.Invoke();
    }
}