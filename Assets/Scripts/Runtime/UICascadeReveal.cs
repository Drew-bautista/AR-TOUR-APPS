using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICascadeReveal : MonoBehaviour
{
    public List<CanvasGroup> targets = new List<CanvasGroup>();
    public float duration = 0.45f;
    public float stagger = 0.08f;
    public Vector2 slideOffset = new Vector2(0f, -18f);

    Coroutine revealRoutine;

    public void Restart()
    {
        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }

        revealRoutine = StartCoroutine(RevealSequence());
    }

    IEnumerator RevealSequence()
    {
        float safeDuration = Mathf.Max(0.05f, duration);
        float safeStagger = Mathf.Max(0f, stagger);

        List<RectTransform> rects = new List<RectTransform>(targets.Count);
        List<Vector2> basePositions = new List<Vector2>(targets.Count);
        for (int i = 0; i < targets.Count; i++)
        {
            CanvasGroup group = targets[i];
            if (group == null)
            {
                rects.Add(null);
                basePositions.Add(Vector2.zero);
                continue;
            }

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            RectTransform rt = group.transform as RectTransform;
            rects.Add(rt);
            basePositions.Add(rt != null ? rt.anchoredPosition : Vector2.zero);
        }

        float startTime = Time.unscaledTime;

        while (true)
        {
            float now = Time.unscaledTime;
            bool anyRunning = false;

            for (int i = 0; i < targets.Count; i++)
            {
                CanvasGroup group = targets[i];
                RectTransform rt = rects[i];

                if (group == null || rt == null)
                {
                    continue;
                }

                float itemStart = startTime + (i * safeStagger);
                float t = (now - itemStart) / safeDuration;

                if (t <= 0f)
                {
                    anyRunning = true;
                    continue;
                }

                float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                group.alpha = eased;

                Vector2 basePos = basePositions[i];
                Vector2 offset = Vector2.Lerp(slideOffset, Vector2.zero, eased);
                rt.anchoredPosition = basePos + offset;

                if (t < 1f)
                {
                    anyRunning = true;
                }
                else
                {
                    group.alpha = 1f;
                    group.interactable = true;
                    group.blocksRaycasts = true;
                    rt.anchoredPosition = basePos;
                }
            }

            if (!anyRunning)
            {
                break;
            }

            yield return null;
        }

        revealRoutine = null;
    }
}
