using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Subtle pulsating glow for Start button backdrop
public class StartButtonPulse : MonoBehaviour
{
    public RectTransform glowRect;
    public float minScale = 1.0f;
    public float maxScale = 1.06f;
    public float duration = 1.6f;
    public float alphaMin = 0.12f;
    public float alphaMax = 0.22f;

    Image glowImage;
    float t = 0f;

    void Awake()
    {
        if (glowRect == null) glowRect = GetComponent<RectTransform>();
        glowImage = glowRect != null ? glowRect.GetComponent<Image>() : null;
    }

    void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(Pulse());
    }

    IEnumerator Pulse()
    {
        t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            float p = (Mathf.Sin((t / duration) * Mathf.PI * 2f) + 1f) * 0.5f; // 0..1
            float s = Mathf.Lerp(minScale, maxScale, p);
            if (glowRect != null) glowRect.localScale = Vector3.one * s;
            if (glowImage != null)
            {
                Color c = glowImage.color; c.a = Mathf.Lerp(alphaMin, alphaMax, p); glowImage.color = c;
            }
            yield return null;
        }
    }
}
