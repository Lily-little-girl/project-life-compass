using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public enum CutsceneStepType
{
    Dialogue,
    Move,
    Teleport,
    FadeActor,
    SetActorActive,
    SetFacing,
    SetControl,
    SetObjectActive,
    SetSprite,
    Wait,
    WaitForAllMoves,
    FadeScreen,
    InvokeEvent,
    LoadScene
}

[System.Serializable]
public class CutsceneStep
{
    public string note;
    public float waitAfter = 0f;   // เว้นช่วงเพิ่มหลังขั้นนี้ (วินาที)
    public CutsceneStepType type;

    [Header("Actor")]
    public CutsceneActor actor;
    public Transform target;
    public float moveSpeed = 0f;
    public bool waitForCompletion = true;

    [Header("Flag / Time")]
    [Tooltip("FadeActor=ปรากฏขึ้น(ไม่ติ๊ก=จางหาย), SetActorActive/SetObjectActive=เปิด, SetControl=ให้ควบคุมได้, SetFacing=หันขวา, FadeScreen=จอมืด(ไม่ติ๊ก=สว่าง)")]
    public bool flag = true;
    public float duration = 1f;

    [Header("Dialogue")]
    public bool showLeft = false;
    public bool showRight = true;
    public Sprite leftSprite;
    public Sprite rightSprite;
    public Sprite leftNameSprite;
    public Sprite rightNameSprite;
    public List<DialogueLine> lines;

    [Header("Object / Sprite / Event / Scene")]
    public GameObject targetObject;
    public SpriteRenderer targetRenderer;
    public Sprite sprite;
    public UnityEvent onInvoke;
    public string sceneName;
}

public class CutsceneSequence : MonoBehaviour
{
    public static readonly List<CutsceneSequence> All = new List<CutsceneSequence>();
    public static bool IsPlaying { get { return activeCount > 0; } }
    private static int activeCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        activeCount = 0;
        All.Clear();
    }

    public DialogueManager dialogueManager;
    public Image fadeImage;
    public bool playOnStart = false;
    public bool logSteps = false;
    public float pauseAfterMajorSteps = 0.4f;

    public List<CutsceneStep> steps;
    public UnityEvent onFinished;

    // ไว้ให้ตัวแสดงสถานะอ่าน
    public bool IsRunning { get { return running; } }
    public int CurrentStepIndex { get; private set; }
    public int ParallelCount { get { return runningParallel; } }

    private bool running;
    private int runningParallel;

    void OnEnable()  { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        if (playOnStart) Play();
    }

    void OnDestroy()
    {
        Release();
    }

    public void Play()
    {
        if (running) return;

        running = true;
        activeCount++;
        CurrentStepIndex = -1;

        if (logSteps) Debug.Log("Cutscene started: " + name);

        StartCoroutine(Run());
    }

    private void Release()
    {
        if (!running) return;

        running = false;
        activeCount = Mathf.Max(0, activeCount - 1);
    }

    private IEnumerator Run()
    {
        try
        {
            for (int i = 0; i < steps.Count; i++)
            {
                CurrentStepIndex = i;
                CutsceneStep step = steps[i];

                if (logSteps) Debug.Log("Cutscene step " + i + ": " + step.type + " " + step.note);

                yield return ExecuteStep(step);

                float pause = step.waitAfter;
                if (IsMajorStep(step)) pause += pauseAfterMajorSteps;

                if (pause > 0f) yield return new WaitForSeconds(pause);
            }

            // รอการเดิน/เฟดแบบ "ไม่รอ" ที่ยังค้าง แต่ไม่เกิน 15 วินาที
            float waited = 0f;
            while (runningParallel > 0 && waited < 15f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (runningParallel > 0)
            {
                Debug.LogWarning("Cutscene " + name + ": มีการเดิน/เฟดแบบไม่รอที่ยังไม่จบ ปิดฉากต่อ");
                runningParallel = 0;
            }
        }
        finally
        {
            // ปล่อยการล็อกเกมเสมอ แม้ฉากพังกลางทาง (ถ้าพัง จะมี error สีแดงใน Console)
            Release();
        }

        if (logSteps) Debug.Log("Cutscene finished: " + name);

        onFinished?.Invoke();
    }

    private bool IsMajorStep(CutsceneStep s)
    {
        switch (s.type)
        {
            case CutsceneStepType.Dialogue:
            case CutsceneStepType.FadeScreen:
                return true;

            case CutsceneStepType.Move:
                return s.waitForCompletion;

            case CutsceneStepType.FadeActor:
                return s.waitForCompletion && s.duration > 0f;

            default:
                return false;
        }
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
    step.rightSprite,
    step.leftNameSprite,
    step.rightNameSprite
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
                activeCount = 0;
                SceneManager.LoadScene(step.sceneName);
                yield return new WaitUntil(() => false);
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