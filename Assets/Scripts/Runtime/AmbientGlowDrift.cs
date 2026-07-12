using UnityEngine;

public class AmbientGlowDrift : MonoBehaviour
{
    public Vector2 amplitude = new Vector2(60f, 80f);
    public float period = 12f;
    public float phase;
    public Vector2 scaleRange = new Vector2(0.95f, 1.10f);

    RectTransform rectTransform;
    Vector2 baseAnchoredPosition;
    Vector3 baseScale;

    void Awake()
    {
        rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            baseAnchoredPosition = rectTransform.anchoredPosition;
        }

        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        if (rectTransform != null)
        {
            baseAnchoredPosition = rectTransform.anchoredPosition;
        }

        baseScale = transform.localScale;
    }

    void Update()
    {
        float safePeriod = Mathf.Max(0.1f, period);
        float time = (Time.unscaledTime / safePeriod) * Mathf.PI * 2f + phase;

        if (rectTransform != null)
        {
            Vector2 offset = new Vector2(Mathf.Sin(time) * amplitude.x, Mathf.Cos(time * 0.87f) * amplitude.y);
            rectTransform.anchoredPosition = baseAnchoredPosition + offset;
        }

        float scaleT = (Mathf.Sin(time * 0.73f) + 1f) * 0.5f;
        float scale = Mathf.Lerp(scaleRange.x, scaleRange.y, scaleT);
        transform.localScale = baseScale * scale;
    }
}
