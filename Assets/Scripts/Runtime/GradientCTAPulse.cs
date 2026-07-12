using UnityEngine;

public class GradientCTAPulse : MonoBehaviour
{
    public RectTransform glowRect;
    public RectTransform buttonRect;
    public float pulsePeriod = 1.35f;
    public float glowScaleAmount = 0.06f;
    public float buttonScaleAmount = 0.02f;

    Vector3 glowBaseScale = Vector3.one;
    Vector3 buttonBaseScale = Vector3.one;

    void Awake()
    {
        if (glowRect != null)
        {
            glowBaseScale = glowRect.localScale;
        }

        if (buttonRect != null)
        {
            buttonBaseScale = buttonRect.localScale;
        }
    }

    void OnEnable()
    {
        if (glowRect != null)
        {
            glowBaseScale = glowRect.localScale;
        }

        if (buttonRect != null)
        {
            buttonBaseScale = buttonRect.localScale;
        }
    }

    void Update()
    {
        float safePeriod = Mathf.Max(0.2f, pulsePeriod);
        float t = (Time.unscaledTime / safePeriod) * Mathf.PI * 2f;
        float pulse = (Mathf.Sin(t) + 1f) * 0.5f;

        float glowScale = 1f + (pulse * glowScaleAmount);
        float buttonScale = 1f + (pulse * buttonScaleAmount);

        if (glowRect != null)
        {
            glowRect.localScale = glowBaseScale * glowScale;
        }

        if (buttonRect != null)
        {
            buttonRect.localScale = buttonBaseScale * buttonScale;
        }
    }
}
