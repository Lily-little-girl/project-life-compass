using UnityEngine;

public class ExamplePanel : MonoBehaviour
{
    public float fadeInTime = 0.6f;
    public float fadeOutTime = 0.5f;

    private SpriteRenderer[] renderers;
    private float alpha = 0f;
    private float target = 0f;
    private float speed = 1f;

    public bool IsFadedOut { get { return target == 0f && alpha <= 0.01f; } }

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        alpha = 0f;
        Apply();
    }

    void OnEnable()
    {
        FadeIn();
    }

    void Update()
    {
        if (!Mathf.Approximately(alpha, target))
        {
            alpha = Mathf.MoveTowards(alpha, target, Time.deltaTime * speed);
            Apply();
        }
    }

    public void FadeIn()
    {
        alpha = 0f;
        target = 1f;
        speed = 1f / Mathf.Max(0.01f, fadeInTime);
        Apply();
    }

    public void FadeOut()
    {
        target = 0f;
        speed = 1f / Mathf.Max(0.01f, fadeOutTime);
    }

    void Apply()
    {
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}