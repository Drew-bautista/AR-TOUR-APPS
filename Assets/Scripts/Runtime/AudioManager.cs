using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Plays scan narration audio. If no AudioClip is assigned for a scan item,
/// the manager falls back to Android text-to-speech so the user still hears
/// a voice explanation.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Text To Speech")]
    [SerializeField] private bool useTextToSpeechWhenClipIsMissing = true;
    [SerializeField] private float estimatedWordsPerSecond = 2.05f;
    [SerializeField] private bool preferGoogleTextToSpeechEngine = true;
    [SerializeField] [Range(0.55f, 1.15f)] private float textToSpeechRate = 0.82f;
    [SerializeField] [Range(0.75f, 1.25f)] private float textToSpeechPitch = 0.96f;
    [SerializeField] private bool chooseHighestQualityVoice = true;
    [SerializeField] private int maxTextToSpeechCharacters = 720;

    private Coroutine estimatedSpeechRoutine;
    private bool isClipPlaying;
    private bool isTextSpeaking;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject textToSpeech;
    private string pendingSpeechText;
    private bool textToSpeechReady;
    private const string GoogleTextToSpeechEnginePackage = "com.google.android.tts";

    private sealed class TextToSpeechInitListener : AndroidJavaProxy
    {
        private readonly Action<int> onInitialized;

        public TextToSpeechInitListener(Action<int> onInitialized)
            : base("android.speech.tts.TextToSpeech$OnInitListener")
        {
            this.onInitialized = onInitialized;
        }

        public void onInit(int status)
        {
            onInitialized?.Invoke(status);
        }
    }
#endif

    /// <summary>
    /// Returns true while an AudioClip or text-to-speech line is active.
    /// </summary>
    public bool IsPlaying => isClipPlaying || isTextSpeaking;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Update()
    {
        if (isClipPlaying && audioSource != null && !audioSource.isPlaying)
        {
            isClipPlaying = false;
        }
    }

    /// <summary>
    /// Starts narration for the active scan item.
    /// </summary>
    public void PlayNarration(AudioClip narrationClip, string fallbackNarrationText)
    {
        StopNarration();

        if (narrationClip != null && audioSource != null)
        {
            audioSource.clip = narrationClip;
            audioSource.Play();
            isClipPlaying = true;
            return;
        }

        string speechText = PrepareSpeechText(fallbackNarrationText);
        if (useTextToSpeechWhenClipIsMissing && !string.IsNullOrWhiteSpace(speechText))
        {
            SpeakWithTextToSpeech(speechText);
        }
    }

    /// <summary>
    /// Reuses the same button for play and pause behavior.
    /// </summary>
    public void ToggleNarration(AudioClip narrationClip, string fallbackNarrationText)
    {
        if (IsPlaying)
        {
            StopNarration();
        }
        else
        {
            PlayNarration(narrationClip, fallbackNarrationText);
        }
    }

    /// <summary>
    /// Stops whichever narration method is currently active.
    /// </summary>
    public void StopNarration()
    {
        if (estimatedSpeechRoutine != null)
        {
            StopCoroutine(estimatedSpeechRoutine);
            estimatedSpeechRoutine = null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        isClipPlaying = false;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (textToSpeech != null)
        {
            textToSpeech.Call<int>("stop");
        }
#endif

        isTextSpeaking = false;
    }

    private void OnDestroy()
    {
        StopNarration();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (textToSpeech != null)
        {
            textToSpeech.Call("shutdown");
            textToSpeech.Dispose();
            textToSpeech = null;
        }
#endif
    }

    private void SpeakWithTextToSpeech(string narrationText)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (string.IsNullOrWhiteSpace(narrationText))
        {
            isTextSpeaking = false;
            return;
        }

        isTextSpeaking = true;

        if (textToSpeech == null)
        {
            InitializeTextToSpeech();
        }

        if (!textToSpeechReady)
        {
            pendingSpeechText = narrationText;
            return;
        }

        SpeakReadyText(narrationText);
#else
        Debug.Log("Scan narration: " + narrationText);
#endif
    }

    private string PrepareSpeechText(string narrationText)
    {
        if (string.IsNullOrWhiteSpace(narrationText))
        {
            return string.Empty;
        }

        string cleaned = CollapseWhitespace(narrationText);
        cleaned = cleaned.Replace(" ,", ",");
        cleaned = cleaned.Replace(" .", ".");
        cleaned = cleaned.Replace(" :", ":");
        cleaned = cleaned.Replace(" ;", ";");
        cleaned = cleaned.Replace(" / ", ", ");
        cleaned = ReplaceTokenForSpeech(cleaned, "AR", "A R");
        cleaned = ReplaceTokenForSpeech(cleaned, "QR", "Q R");
        cleaned = ReplaceTokenForSpeech(cleaned, "TTS", "text to speech");
        cleaned = CollapseWhitespace(cleaned);

        int characterLimit = Mathf.Max(180, maxTextToSpeechCharacters);
        if (cleaned.Length <= characterLimit)
        {
            return cleaned;
        }

        int sentenceCut = cleaned.LastIndexOfAny(new[] { '.', '!', '?' }, Mathf.Min(cleaned.Length - 1, characterLimit - 1));
        if (sentenceCut < characterLimit * 0.45f)
        {
            sentenceCut = characterLimit - 1;
        }

        return cleaned.Substring(0, sentenceCut + 1).Trim();
    }

    private static string CollapseWhitespace(string value)
    {
        return string.Join(" ", value.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string ReplaceTokenForSpeech(string text, string token, string spokenToken)
    {
        string padded = " " + text + " ";
        padded = padded.Replace(" " + token + " ", " " + spokenToken + " ");
        padded = padded.Replace(" " + token + ".", " " + spokenToken + ".");
        padded = padded.Replace(" " + token + ",", " " + spokenToken + ",");
        padded = padded.Replace(" " + token + ":", " " + spokenToken + ":");
        return padded.Trim();
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void InitializeTextToSpeech()
    {
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            TextToSpeechInitListener initListener = new TextToSpeechInitListener(OnTextToSpeechInitialized);

            if (preferGoogleTextToSpeechEngine && IsPackageInstalled(currentActivity, GoogleTextToSpeechEnginePackage))
            {
                try
                {
                    textToSpeech = new AndroidJavaObject(
                        "android.speech.tts.TextToSpeech",
                        currentActivity,
                        initListener,
                        GoogleTextToSpeechEnginePackage);
                    return;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Google Text-to-Speech engine could not be used. Falling back to default engine: " + exception.Message);
                }
            }

            textToSpeech = new AndroidJavaObject(
                "android.speech.tts.TextToSpeech",
                currentActivity,
                initListener);
        }
    }

    private void OnTextToSpeechInitialized(int status)
    {
        textToSpeechReady = status == 0;
        if (!textToSpeechReady || textToSpeech == null)
        {
            isTextSpeaking = false;
            return;
        }

        if (!string.IsNullOrWhiteSpace(pendingSpeechText))
        {
            string speechText = pendingSpeechText;
            pendingSpeechText = null;
            SpeakReadyText(speechText);
            return;
        }

        ConfigureTextToSpeechVoice(string.Empty);
    }

    private void SpeakReadyText(string narrationText)
    {
        if (textToSpeech == null)
        {
            isTextSpeaking = false;
            return;
        }

        ConfigureTextToSpeechVoice(narrationText);
        textToSpeech.Call<int>("stop");
        textToSpeech.Call<int>("speak", narrationText, 0, null, "AguinaldoShrineScanNarration");

        if (estimatedSpeechRoutine != null)
        {
            StopCoroutine(estimatedSpeechRoutine);
        }

        estimatedSpeechRoutine = StartCoroutine(FinishEstimatedSpeechAfterDelay(narrationText));
    }

    private void ConfigureTextToSpeechVoice(string narrationText)
    {
        if (textToSpeech == null)
        {
            return;
        }

        textToSpeech.Call<int>("setSpeechRate", Mathf.Clamp(textToSpeechRate, 0.55f, 1.15f));
        textToSpeech.Call<int>("setPitch", Mathf.Clamp(textToSpeechPitch, 0.75f, 1.25f));

        string[,] localeCandidates = ShouldPreferFilipinoVoice(narrationText)
            ? new[,] { { "fil", "PH" }, { "tl", "PH" }, { "en", "PH" }, { "en", "US" } }
            : new[,] { { "en", "US" }, { "en", "PH" }, { "fil", "PH" }, { "tl", "PH" } };

        for (int i = 0; i < localeCandidates.GetLength(0); i++)
        {
            using (AndroidJavaObject locale = new AndroidJavaObject("java.util.Locale", localeCandidates[i, 0], localeCandidates[i, 1]))
            {
                int languageResult = textToSpeech.Call<int>("setLanguage", locale);
                if (languageResult >= 0)
                {
                    if (chooseHighestQualityVoice)
                    {
                        TryUseBestVoiceForLocale(locale);
                    }

                    return;
                }
            }
        }
    }

    private bool ShouldPreferFilipinoVoice(string narrationText)
    {
        if (AppLanguage.IsFilipino)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(narrationText))
        {
            return false;
        }

        string lower = " " + narrationText.ToLowerInvariant() + " ";
        string[] filipinoMarkers =
        {
            " ang ",
            " mga ",
            " ng ",
            " sa ",
            " ay ",
            " ito ",
            " bahagi ",
            " laban ",
            " digmaan ",
            " pook ",
            " silid ",
            " noong "
        };

        int matches = 0;
        for (int i = 0; i < filipinoMarkers.Length; i++)
        {
            if (lower.Contains(filipinoMarkers[i]))
            {
                matches++;
                if (matches >= 2)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void TryUseBestVoiceForLocale(AndroidJavaObject targetLocale)
    {
        try
        {
            using (AndroidJavaObject voices = textToSpeech.Call<AndroidJavaObject>("getVoices"))
            {
                if (voices == null)
                {
                    return;
                }

                string targetLanguage = targetLocale.Call<string>("getLanguage");
                string targetCountry = targetLocale.Call<string>("getCountry");
                AndroidJavaObject bestVoice = null;
                int bestScore = int.MinValue;

                using (AndroidJavaObject iterator = voices.Call<AndroidJavaObject>("iterator"))
                {
                    while (iterator.Call<bool>("hasNext"))
                    {
                        AndroidJavaObject voice = iterator.Call<AndroidJavaObject>("next");
                        int score = ScoreVoiceForLocale(voice, targetLanguage, targetCountry);
                        if (score > bestScore)
                        {
                            bestVoice?.Dispose();
                            bestVoice = voice;
                            bestScore = score;
                        }
                        else
                        {
                            voice.Dispose();
                        }
                    }
                }

                if (bestVoice != null)
                {
                    textToSpeech.Call<int>("setVoice", bestVoice);
                    bestVoice.Dispose();
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Text-to-speech voice selection failed. Default voice will be used: " + exception.Message);
        }
    }

    private static int ScoreVoiceForLocale(AndroidJavaObject voice, string targetLanguage, string targetCountry)
    {
        using (AndroidJavaObject voiceLocale = voice.Call<AndroidJavaObject>("getLocale"))
        {
            string language = voiceLocale.Call<string>("getLanguage");
            string country = voiceLocale.Call<string>("getCountry");
            if (!string.Equals(language, targetLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return int.MinValue;
            }

            int quality = voice.Call<int>("getQuality");
            int latency = voice.Call<int>("getLatency");
            bool needsNetwork = voice.Call<bool>("isNetworkConnectionRequired");
            string name = voice.Call<string>("getName") ?? string.Empty;

            int score = 1000 + (quality * 3) - latency;
            if (string.Equals(country, targetCountry, StringComparison.OrdinalIgnoreCase))
            {
                score += 300;
            }

            if (!needsNetwork)
            {
                score += 220;
            }

            if (name.IndexOf("google", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 120;
            }

            if (name.IndexOf("network", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score -= 80;
            }

            return score;
        }
    }

    private static bool IsPackageInstalled(AndroidJavaObject activity, string packageName)
    {
        try
        {
            using (AndroidJavaObject packageManager = activity.Call<AndroidJavaObject>("getPackageManager"))
            using (AndroidJavaObject packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, 0))
            {
                return packageInfo != null;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }
#endif

    private IEnumerator FinishEstimatedSpeechAfterDelay(string narrationText)
    {
        int wordCount = string.IsNullOrWhiteSpace(narrationText)
            ? 1
            : narrationText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;

        float estimatedDuration = Mathf.Max(2f, wordCount / Mathf.Max(estimatedWordsPerSecond, 0.5f));
        yield return new WaitForSecondsRealtime(estimatedDuration);

        isTextSpeaking = false;
        estimatedSpeechRoutine = null;
    }
}
