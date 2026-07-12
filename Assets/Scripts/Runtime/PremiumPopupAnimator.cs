using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PremiumPopupAnimator : MonoBehaviour
{
    [SerializeField] private float duration = 0.18f;
    [SerializeField] private float startScale = 0.96f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector3 baseScale = Vector3.one;
    private Coroutine animationRoutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            baseScale = rectTransform.localScale;
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        canvasGroup.alpha = 0f;
        if (rectTransform != null)
        {
            rectTransform.localScale = baseScale * startScale;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            t = 1f - Mathf.Pow(1f - t, 3f);
            canvasGroup.alpha = t;
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.LerpUnclamped(baseScale * startScale, baseScale, t);
            }

            yield return null;
        }

        canvasGroup.alpha = 1f;
        if (rectTransform != null)
        {
            rectTransform.localScale = baseScale;
        }

        animationRoutine = null;
    }
}
