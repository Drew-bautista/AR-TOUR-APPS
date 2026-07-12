using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PremiumButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float pressedScale = 0.96f;
    [SerializeField] private float animationSpeed = 18f;

    private RectTransform rectTransform;
    private Vector3 baseScale = Vector3.one;
    private Coroutine scaleRoutine;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        baseScale = rectTransform != null ? rectTransform.localScale : transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AnimateTo(baseScale * pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateTo(baseScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateTo(baseScale);
    }

    private void AnimateTo(Vector3 targetScale)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
        }

        scaleRoutine = StartCoroutine(ScaleRoutine(targetScale));
    }

    private IEnumerator ScaleRoutine(Vector3 targetScale)
    {
        Transform target = rectTransform != null ? rectTransform : transform;
        while ((target.localScale - targetScale).sqrMagnitude > 0.0001f)
        {
            target.localScale = Vector3.Lerp(target.localScale, targetScale, animationSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        target.localScale = targetScale;
        scaleRoutine = null;
    }
}
