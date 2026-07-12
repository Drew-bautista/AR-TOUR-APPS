using UnityEngine;
using UnityEngine.UI;

public class PremiumAudioIndicator : MonoBehaviour
{
    [SerializeField] private Image pulseImage;
    [SerializeField] private Text label;
    [SerializeField] private float pulseSpeed = 4.4f;

    private AudioSource[] audioSources;
    private CanvasGroup canvasGroup;
    private float refreshTimer;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        RefreshSources();
    }

    public void Configure(Image pulse, Text textLabel)
    {
        pulseImage = pulse;
        label = textLabel;
    }

    private void Update()
    {
        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            RefreshSources();
        }

        bool playing = false;
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null && audioSources[i].isPlaying)
            {
                playing = true;
                break;
            }
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, playing ? 1f : 0f, Time.unscaledDeltaTime * 8f);
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (!playing)
        {
            return;
        }

        float pulse = 0.72f + (Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.18f) + 0.18f;
        if (pulseImage != null)
        {
            Color color = pulseImage.color;
            color.a = pulse;
            pulseImage.color = color;
        }

        if (label != null)
        {
            label.text = "AUDIO";
        }
    }

    private void RefreshSources()
    {
        audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        refreshTimer = 1.5f;
    }
}
