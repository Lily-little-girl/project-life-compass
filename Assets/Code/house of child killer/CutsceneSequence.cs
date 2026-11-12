using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public enum CutsceneStepType
{
    Dialogue,         // พูดคุย
    Move,             // เดินไปที่จุด
    Teleport,         // วาร์ปไปที่จุด
    FadeActor,        // ตัวละครจางหาย/ปรากฏ
    SetActorActive,   // เปิด/ปิดตัวละคร
    SetFacing,        // หันซ้าย/ขวา
    SetControl,       // ล็อก/ปลดล็อกการควบคุม (ใช้กับผู้เล่น)
    SetObjectActive,  // เปิด/ปิด object ทั่วไป
    SetSprite,        // เปลี่ยนสไปรต์ (เช่น ประตูเปิด/ปิด)
    Wait,             // รอ
    WaitForAllMoves,  // รอให้ทุกอย่างที่สั่งแบบ "ไม่รอ" เสร็จก่อน
    FadeScreen,       // เฟดจอดำ/สว่าง
    InvokeEvent,      // เรียก UnityEvent
    LoadScene         // โหลดซีนถัดไป
}

[System.Serializable]
public class CutsceneStep
{
    public string note;
    public CutsceneStepType type;

    [Header("Actor")]
    public CutsceneActor actor;
    public Transform target;                 // Move / Teleport
    public float moveSpeed = 0f;             // Move: 0 = ใช้ความเร็วของตัวละคร
    public bool waitForCompletion = true;    // Move / FadeActor: ไม่ติ๊ก = สั่งแล้วไปขั้นต่อไปเลย (ทำพร้อมกัน)

    [Header("Flag / Time")]
    [Tooltip("FadeActor=ปรากฏขึ้น(ไม่ติ๊ก=จางหาย), SetActorActive/SetObjectActive=เปิด, SetControl=ให้ควบคุมได้, SetFacing=หันขวา, FadeScreen=จอมืด(ไม่ติ๊ก=สว่าง)")]
    public bool flag = true;
    public float duration = 1f;              // Wait / FadeActor / FadeScreen

    [Header("Dialogue")]
    public bool showLeft = false;
    public bool showRight = true;
    public Sprite leftSprite;
    public Sprite rightSprite;
    public List<DialogueLine> lines;

    [Header("Object / Sprite / Event / Scene")]
    public GameObject targetObject;          // SetObjectActive
    public SpriteRenderer targetRenderer;    // SetSprite
    public Sprite sprite;                    // SetSprite
    public UnityEvent onInvoke;              // InvokeEvent
    public string sceneName;                 // LoadScene
}

public class CutsceneSequence : MonoBehaviour
{
    public static bool IsPlaying { get { return activeCount > 0; } }
    private static int activeCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { activeCount = 0; }

    public DialogueManager dialogueManager;
    public Image fadeImage;                  // ภาพดำเต็มจอ ไว้เฟดจอ (ว่างได้)
    public bool playOnStart = false;
    public bool logSteps = false;            // ติ๊กเพื่อดูว่าตอนนี้อยู่ขั้นไหนใน Console

    public List<CutsceneStep> steps;
    public UnityEvent onFinished;

    private bool running;
    private int runningParallel;

    void Start()
    {
        if (playOnStart) Play();
    }

    void OnDestroy()
    {
        if (running)
        {
            running = false;
            activeCount = Mathf.Max(0, activeCount - 1);
        }
    }

    public void Play()
    {
        if (running) return;

        running = true;
        activeCount++;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        for (int i = 0; i < steps.Count; i++)
        {
            if (logSteps) Debug.Log("Cutscene step " + i + ": " + steps[i].type + " " + steps[i].note);
            yield return ExecuteStep(steps[i]);
        }

        while (runningParallel > 0) yield return null;

        running = false;
        activeCount = Mathf.Max(0, activeCount - 1);

        onFinished?.Invoke();
    }

    private IEnumerator RunParallel(IEnumerator routine)
    {
        runningParallel++;
        yield return routine;
        runningParallel--;
    }

    private IEnumerator ExecuteStep(CutsceneStep step)
    {
        switch (step.type)
        {
            case CutsceneStepType.Dialogue:
            {
                if (dialogueManager == null || step.lines == null || step.lines.Count == 0)
                {
                    Debug.LogWarning("Dialogue ข้าม: ไม่มี DialogueManager หรือไม่มีบรรทัดข้อความ");
                    break;
                }

                bool finished = false;
                dialogueManager.StartDialogue(
                    step.lines,
                    () => finished = true,
                    step.showLeft,
                    step.showRight,
                    step.leftSprite,
                    step.rightSprite
                );

                while (!finished) yield return null;
                break;
            }

            case CutsceneStepType.Move:
            {
                if (step.actor == null || step.target == null)
                {
                    Debug.LogWarning("Move ข้าม: ยังไม่ได้ใส่ Actor หรือ Target");
                    break;
                }

                IEnumerator move = step.actor.MoveTo(step.target.position, step.moveSpeed);

                if (step.waitForCompletion) yield return move;
                else StartCoroutine(RunParallel(move));
                break;
            }

            case CutsceneStepType.Teleport:
            {
                if (step.actor == null || step.target == null)
                {
                    Debug.LogWarning("Teleport ข้าม: ยังไม่ได้ใส่ Actor หรือ Target");
                    break;
                }

                step.actor.TeleportTo(step.target.position);
                break;
            }

            case CutsceneStepType.FadeActor:
            {
                if (step.actor == null) break;

                IEnumerator fade = step.actor.FadeTo(step.flag ? 1f : 0f, step.duration);

                if (step.waitForCompletion) yield return fade;
                else StartCoroutine(RunParallel(fade));
                break;
            }

            case CutsceneStepType.SetActorActive:
                if (step.actor != null) step.actor.gameObject.SetActive(step.flag);
                break;

            case CutsceneStepType.SetFacing:
                if (step.actor != null) step.actor.Face(step.flag);
                break;

            case CutsceneStepType.SetControl:
                if (step.actor != null) step.actor.SetControl(step.flag);
                break;

            case CutsceneStepType.SetObjectActive:
                if (step.targetObject != null) step.targetObject.SetActive(step.flag);
                break;

            case CutsceneStepType.SetSprite:
                if (step.targetRenderer != null && step.sprite != null) step.targetRenderer.sprite = step.sprite;
                break;

            case CutsceneStepType.Wait:
                yield return new WaitForSeconds(step.duration);
                break;

            case CutsceneStepType.WaitForAllMoves:
                while (runningParallel > 0) yield return null;
                break;

            case CutsceneStepType.FadeScreen:
                yield return FadeScreenRoutine(step.flag ? 1f : 0f, step.duration);
                break;

            case CutsceneStepType.InvokeEvent:
                step.onInvoke?.Invoke();
                break;

            case CutsceneStepType.LoadScene:
            {
                running = false;
                activeCount = 0;   // กันค่าค้างข้ามซีน
                SceneManager.LoadScene(step.sceneName);
                yield return new WaitUntil(() => false);   // รอให้ซีนเปลี่ยน ไม่ทำขั้นต่อไป
                break;
            }
        }
    }

    private IEnumerator FadeScreenRoutine(float targetAlpha, float duration)
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);

        Color c = fadeImage.color;
        float startAlpha = c.a;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(t / duration));
            fadeImage.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        fadeImage.color = c;

        if (targetAlpha <= 0f) fadeImage.gameObject.SetActive(false);
    }
}