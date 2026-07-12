using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

// Small behavior to handle hover/press scale on cards
public class CardButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    RectTransform rt;
    Coroutine scaleCoroutine;
    public float hoverScale = 1.04f;
    public float pressScale = 0.96f;
    public float hoverTime = 0.16f;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (rt == null) rt = transform as RectTransform;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartScale(hoverScale, hoverTime);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartScale(1f, 0.12f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        rt.localScale = Vector3.one * pressScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StartScale(hoverScale, 0.08f);
    }

    void StartScale(float target, float duration)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(target, duration));
    }

    IEnumerator ScaleTo(float target, float duration)
    {
        Vector3 from = rt.localScale;
        Vector3 to = Vector3.one * target;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / duration);
            rt.localScale = Vector3.Lerp(from, to, p);
            yield return null;
        }
        rt.localScale = to;
        scaleCoroutine = null;
    }
}
