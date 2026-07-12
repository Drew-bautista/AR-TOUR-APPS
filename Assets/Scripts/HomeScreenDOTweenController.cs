using UnityEngine;

// Optional DOTween-based controller. Define ENABLE_DOTWEEN in Player Settings and install DOTween to enable.

#if ENABLE_DOTWEEN
using DG.Tweening;
#endif

public class HomeScreenDOTweenController : MonoBehaviour
{
#if ENABLE_DOTWEEN
    public CanvasGroup canvasGroup;
    public RectTransform headerTransform;
    public RectTransform[] cardTransforms;
    public RectTransform startGlow;
    public float fadeDuration = 0.6f;

    public void PlayEntrance()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutCubic);
        }

        if (headerTransform != null)
        {
            Vector2 from = headerTransform.anchoredPosition + new Vector2(0, -12);
            headerTransform.anchoredPosition = from;
            headerTransform.DOAnchorPosY(0f, 0.5f).SetEase(Ease.OutCubic).SetDelay(0.12f);
        }

        if (cardTransforms != null)
        {
            for (int i = 0; i < cardTransforms.Length; i++)
            {
                var rt = cardTransforms[i];
                Vector2 f = rt.anchoredPosition + new Vector2(0, 10);
                rt.anchoredPosition = f;
                rt.DOAnchorPosY(0f, 0.42f).SetEase(Ease.OutBack).SetDelay(0.08f + i * 0.08f);
            }
        }

        if (startGlow != null)
        {
            startGlow.DOPunchScale(Vector3.one * 0.04f, 1.2f, 1, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }
#else
    public void PlayEntrance() { Debug.LogWarning("DOTween not enabled. Define ENABLE_DOTWEEN and install DOTween to use DOTween animations."); }
#endif
}
