using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

public enum StoryStepType { Dialogue, SecondCharacterWalkIn ,FirstCharacterWalkOut ,SecondCharacterWalkTo }


[Serializable]
public class StoryStep
{
    public string note;                 // จดเตือนตัวเอง ไม่มีผลกับเกม
    public StoryStepType type;

    [Header("ใช้เมื่อ type = Dialogue")]
    public bool showLeft = true;
    public bool showRight = false;
    public Sprite leftNameSprite;
    public Sprite rightNameSprite;
    public List<DialogueLine> lines;
    [Header("ใช้เมื่อ type = SecondCharacterWalkTo")]
    public Transform destination;
    public bool waitUntilArrived = true;
}

public class StoryDialogueTrigger : MonoBehaviour
{
    [Header("Next Scene")]
    public string nextSceneName;
    public float loadDelay = 0.5f;
    public DialogueManager dialogueManager;
    public PlayerMovement playerMovement;
    public SecondCharacterEntrance secondCharacter;
    public CharacterExit firstCharacter; 

    public List<StoryStep> steps;

    public UnityEvent onDialogueFinished; // เรียกเมื่อจบทุกขั้นตอน

    private bool triggered = false;
    private int stepIndex;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        if (dialogueManager == null || playerMovement == null)
        {
            Debug.LogError("DialogueManager หรือ PlayerMovement ยังไม่ได้กำหนด!");
            return;
        }

        if (steps == null || steps.Count == 0)
        {
            Debug.LogError("ยังไม่มี Steps!");
            return;
        }

        triggered = true;
        playerMovement.SetMovementEnabled(false);

        stepIndex = 0;
        RunStep();
    }

    private void RunStep()
    {
        if (stepIndex >= steps.Count)
        {
            Finish();
            return;
        }

        StoryStep step = steps[stepIndex];

        switch (step.type)
        {
            case StoryStepType.Dialogue:
                if (step.lines == null || step.lines.Count == 0)
                {
                    Debug.LogWarning("Step " + stepIndex + " ไม่มีข้อความ ข้ามไป");
                    NextStep();
                    return;
                }

                Sprite rightSprite = secondCharacter != null
                    ? secondCharacter.portraitSprite
                    : null;

                dialogueManager.StartDialogue(
    step.lines,
    NextStep,
    step.showLeft,
    step.showRight,
    null,
    rightSprite,
    step.leftNameSprite,
    step.rightNameSprite
);
                break;

            case StoryStepType.SecondCharacterWalkIn:
                if (secondCharacter == null)
                {
                    Debug.LogWarning("ยังไม่ได้ใส่ Second Character ข้ามไป");
                    NextStep();
                    return;
                }

                secondCharacter.WalkIn(NextStep);
                break;

                case StoryStepType.FirstCharacterWalkOut:
    if (firstCharacter == null)
    {
        Debug.LogWarning("ยังไม่ได้ใส่ First Character ข้ามไป");
        NextStep();
        return;
    }

    firstCharacter.WalkOut(NextStep);
    break;

case StoryStepType.SecondCharacterWalkTo:
    if (secondCharacter == null || step.destination == null)
    {
        Debug.LogWarning("ยังไม่ได้ใส่ Second Character หรือ Destination ข้ามไป");
        NextStep();
        return;
    }

    if (step.waitUntilArrived)
                {
                    secondCharacter.WalkTo(step.destination, NextStep);
                }
                else
                {
                    secondCharacter.WalkTo(step.destination, null);
                    NextStep();
                }
                break;

        }
    }

    private void NextStep()
    {
        stepIndex++;
        RunStep();
    }

   private void Finish()
{
    onDialogueFinished?.Invoke();

    if (!string.IsNullOrEmpty(nextSceneName))
    {
        // กำลังจะเปลี่ยนซีน ไม่ต้องปลดล็อกผู้เล่น
        StartCoroutine(LoadNextScene());
        return;
    }

    playerMovement.SetMovementEnabled(true);
}

private IEnumerator LoadNextScene()
{
    yield return new WaitForSeconds(loadDelay);
    SceneManager.LoadScene(nextSceneName);
}
}