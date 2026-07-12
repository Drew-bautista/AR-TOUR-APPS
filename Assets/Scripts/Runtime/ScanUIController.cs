using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Controls the scan button, scan selection popup, camera hint, scan result panel,
/// and the video lesson plus short quiz flow for matched scan items.
/// </summary>
public class ScanUIController : MonoBehaviour
{
    public static readonly string[] AllowedVideoQuizImageKeys =
    {
        "att.4UPALrJaWNcL1rFsRBeARFu0UP8B2dBB58haSNUi1o0",
        "att.6AY3Aamn8uJgp6bH7VExt7OaW5Kaws6Zbn_d3RB1hek",
        "att.10jcFhOTiIJ93gcwokjBtZpwdhIdS2ga8wYuAj7hBdc",
        "att.TgqwnWu-F-o9tgSb03GQTNEv0XtpmdoN6kfT4veqTSQ",
        "att.V3EgYkGryHaaiV42sgk-m6Vr83rsNK7R2ICG9RAAoXM",
        "att.ZwJoStPTv-n2eHPWge4PT_XmZgOqTPxmrFzy2-LaPtM"
    };

    private static readonly string[] KnownMediaKeyExtensions =
    {
        ".meta",
        ".mp4",
        ".mov",
        ".m4v",
        ".webm",
        ".jpg",
        ".jpeg",
        ".png"
    };
    private static readonly HashSet<string> AllowedVideoQuizImageKeySet = BuildAllowedVideoQuizImageKeySet();

    public static bool IsAllowedVideoQuizImageKey(string value)
    {
        string key = NormalizeVideoKey(value);
        return !string.IsNullOrEmpty(key) && AllowedVideoQuizImageKeySet.Contains(key);
    }

    private class QuizQuestion
    {
        public readonly string Question;
        public readonly string[] Choices;
        public readonly int CorrectIndex;

        public QuizQuestion(string question, string[] choices, int correctIndex)
        {
            Question = question;
            Choices = choices;
            CorrectIndex = Mathf.Clamp(correctIndex, 0, choices.Length - 1);
        }
    }

    [Header("Flow Buttons")]
    [SerializeField] private Button scanItemButton;
    [SerializeField] private Button qrModeButton;
    [SerializeField] private Button qrShortcutButton;
    [SerializeField] private Button cameraModeButton;
    [SerializeField] private Button galleryModeButton;
    [SerializeField] private Button cancelChoiceButton;

    [Header("Result Buttons")]
    [SerializeField] private Button qrCloseButton;
    [SerializeField] private Button playPauseButton;
    [SerializeField] private Button closeResultButton;

    [Header("Panels")]
    [SerializeField] private GameObject scanChoiceOverlay;
    [SerializeField] private GameObject qrScanOverlay;
    [SerializeField] private GameObject cameraHintPanel;
    [SerializeField] private GameObject resultOverlay;

    [Header("Texts")]
    [SerializeField] private Text qrScanHintText;
    [SerializeField] private Text cameraHintText;
    [SerializeField] private Text resultTitleText;
    [SerializeField] private Text resultDescriptionText;
    [SerializeField] private Text resultStatusText;
    [SerializeField] private Text playPauseButtonText;

    [Header("Images")]
    [SerializeField] private Image resultPreviewImage;

    [Header("Video Quiz")]
    [SerializeField] private string[] videoQuizImageKeys = Array.Empty<string>();
    [SerializeField] private VideoClip[] videoQuizClips = Array.Empty<VideoClip>();

    private ImageRecognitionManager imageRecognitionManager;
    private AudioManager audioManager;

    private AudioClip currentAudioClip;
    private string currentNarrationText;
    private ScanResult3DPreview result3DPreview;

    private Button playVideoButton;
    private Text playVideoButtonText;
    private GameObject videoQuizOverlay;
    private GameObject videoStageGroup;
    private GameObject quizStageGroup;
    private GameObject quizResultGroup;
    private RawImage videoImage;
    private Text videoTitleText;
    private Text videoHintText;
    private Text quizProgressText;
    private Text quizQuestionText;
    private Text quizResultTitleText;
    private Text quizResultScoreText;
    private Button closeVideoQuizButton;
    private Button quizDoneButton;
    private Button[] quizChoiceButtons;
    private Text[] quizChoiceTexts;
    private VideoPlayer videoPlayer;
    private RenderTexture videoRenderTexture;
    private Coroutine videoPrepareRoutine;

    private VideoClip currentVideoClip;
    private string currentVideoMediaKey;
    private string currentItemTitle;
    private string currentItemDescription;
    private readonly List<QuizQuestion> currentQuiz = new List<QuizQuestion>();
    private int currentQuizIndex;
    private int currentQuizScore;
    private bool quizAcceptingInput;

    private void Awake()
    {
        EnsureResult3DPreview();
        EnsureVideoQuizUi();

        if (scanItemButton != null)
        {
            scanItemButton.onClick.AddListener(ShowScanChoices);
        }

        if (cameraModeButton != null)
        {
            cameraModeButton.onClick.AddListener(HandleCameraModeButtonPressed);
        }

        if (qrModeButton != null)
        {
            qrModeButton.onClick.AddListener(HandleQrModeButtonPressed);
        }

        if (qrShortcutButton != null)
        {
            qrShortcutButton.onClick.AddListener(HandleQrModeButtonPressed);
        }

        if (galleryModeButton != null)
        {
            galleryModeButton.onClick.AddListener(HandleGalleryModeButtonPressed);
        }

        if (cancelChoiceButton != null)
        {
            cancelChoiceButton.onClick.AddListener(HideAllScanOverlays);
        }

        if (qrCloseButton != null)
        {
            qrCloseButton.onClick.AddListener(HandleQrClosePressed);
        }

        if (playPauseButton != null)
        {
            playPauseButton.onClick.AddListener(HandlePlayPausePressed);
        }

        if (closeResultButton != null)
        {
            closeResultButton.onClick.AddListener(HandleCloseResultPressed);
        }

        if (closeVideoQuizButton != null)
        {
            closeVideoQuizButton.onClick.AddListener(CloseVideoQuiz);
        }

        if (quizDoneButton != null)
        {
            quizDoneButton.onClick.AddListener(CloseVideoQuiz);
        }

        HideAllScanOverlays();
    }

    private void OnDestroy()
    {
        if (scanItemButton != null)
        {
            scanItemButton.onClick.RemoveListener(ShowScanChoices);
        }

        if (cameraModeButton != null)
        {
            cameraModeButton.onClick.RemoveListener(HandleCameraModeButtonPressed);
        }

        if (qrModeButton != null)
        {
            qrModeButton.onClick.RemoveListener(HandleQrModeButtonPressed);
        }

        if (qrShortcutButton != null)
        {
            qrShortcutButton.onClick.RemoveListener(HandleQrModeButtonPressed);
        }

        if (galleryModeButton != null)
        {
            galleryModeButton.onClick.RemoveListener(HandleGalleryModeButtonPressed);
        }

        if (cancelChoiceButton != null)
        {
            cancelChoiceButton.onClick.RemoveListener(HideAllScanOverlays);
        }

        if (qrCloseButton != null)
        {
            qrCloseButton.onClick.RemoveListener(HandleQrClosePressed);
        }

        if (playPauseButton != null)
        {
            playPauseButton.onClick.RemoveListener(HandlePlayPausePressed);
        }

        if (closeResultButton != null)
        {
            closeResultButton.onClick.RemoveListener(HandleCloseResultPressed);
        }

        if (playVideoButton != null)
        {
            playVideoButton.onClick.RemoveListener(HandlePlayVideoPressed);
        }

        if (closeVideoQuizButton != null)
        {
            closeVideoQuizButton.onClick.RemoveListener(CloseVideoQuiz);
        }

        if (quizDoneButton != null)
        {
            quizDoneButton.onClick.RemoveListener(CloseVideoQuiz);
        }

        if (quizChoiceButtons != null)
        {
            for (int i = 0; i < quizChoiceButtons.Length; i++)
            {
                if (quizChoiceButtons[i] != null)
                {
                    quizChoiceButtons[i].onClick.RemoveAllListeners();
                }
            }
        }

        ReleaseVideoResources();
    }

    /// <summary>
    /// Connects the scan UI to the feature managers after the scene is generated.
    /// </summary>
    public void Configure(ImageRecognitionManager recognitionManager, AudioManager scanAudioManager)
    {
        imageRecognitionManager = recognitionManager;
        audioManager = scanAudioManager;
    }

#if UNITY_EDITOR
    public void SetVideoQuizClipsForEditor(VideoClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            videoQuizImageKeys = Array.Empty<string>();
            videoQuizClips = Array.Empty<VideoClip>();
            return;
        }

        HashSet<string> registeredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<string> keys = new List<string>();
        List<VideoClip> registeredClips = new List<VideoClip>();
        for (int i = 0; i < clips.Length; i++)
        {
            VideoClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            string key = NormalizeVideoKey(clip.name);
            if (string.IsNullOrEmpty(key) || !registeredKeys.Add(key))
            {
                continue;
            }

            keys.Add(clip.name);
            registeredClips.Add(clip);
        }

        videoQuizImageKeys = keys.ToArray();
        videoQuizClips = registeredClips.ToArray();
    }

    public void SetVideoQuizBindingsForEditor(string[] imageKeys, VideoClip[] clips)
    {
        videoQuizImageKeys = imageKeys ?? Array.Empty<string>();
        videoQuizClips = clips ?? Array.Empty<VideoClip>();
    }
#endif

    /// <summary>
    /// Opens the simple choice dialog with camera and gallery options.
    /// </summary>
    public void ShowScanChoices()
    {
        CloseVideoQuiz();
        SetQrShortcutVisible(false);

        if (scanChoiceOverlay != null)
        {
            scanChoiceOverlay.SetActive(true);
            scanChoiceOverlay.transform.SetAsLastSibling();
        }

        if (resultOverlay != null)
        {
            resultOverlay.SetActive(false);
        }

        if (qrScanOverlay != null)
        {
            qrScanOverlay.SetActive(false);
        }

        if (cameraHintPanel != null)
        {
            cameraHintPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Shows a small live hint while the user is scanning with the real camera.
    /// </summary>
    public void ShowCameraHint(string message)
    {
        SetQrShortcutVisible(true);

        if (cameraHintText != null)
        {
            cameraHintText.text = AppLanguage.TranslateKnown(message);
        }

        if (cameraHintPanel != null)
        {
            cameraHintPanel.SetActive(true);
        }

        if (scanChoiceOverlay != null)
        {
            scanChoiceOverlay.SetActive(false);
        }
    }

    /// <summary>
    /// Hides the live camera hint.
    /// </summary>
    public void HideCameraHint()
    {
        if (cameraHintPanel != null)
        {
            cameraHintPanel.SetActive(false);
        }
    }

    public void ShowQrScanOverlay(string message)
    {
        CloseVideoQuiz();
        SetQrShortcutVisible(false);

        if (qrScanHintText != null)
        {
            qrScanHintText.text = AppLanguage.TranslateKnown(message);
        }

        if (qrScanOverlay != null)
        {
            qrScanOverlay.SetActive(true);
            qrScanOverlay.transform.SetAsLastSibling();
        }

        if (scanChoiceOverlay != null)
        {
            scanChoiceOverlay.SetActive(false);
        }

        if (cameraHintPanel != null)
        {
            cameraHintPanel.SetActive(false);
        }

        if (resultOverlay != null)
        {
            resultOverlay.SetActive(false);
        }
    }

    public void HideQrScanOverlay()
    {
        if (qrScanOverlay != null)
        {
            qrScanOverlay.SetActive(false);
        }

        SetQrShortcutVisible(true);
    }

    /// <summary>
    /// Displays the matched scan item's information in a popup.
    /// </summary>
    public void ShowRecognitionResult(
        string title,
        string description,
        Sprite previewImage,
        AudioClip narrationClip,
        string narrationText,
        string sourceLabel,
        string mediaKey = null,
        bool allowVideoQuiz = false)
    {
        CloseVideoQuiz();
        currentVideoClip = null;
        currentVideoMediaKey = string.Empty;
        SetPlayVideoButtonVisible(false);

        currentAudioClip = narrationClip;
        currentNarrationText = narrationText;
        currentItemTitle = title;
        currentItemDescription = description;
        bool canShowVideoQuiz = allowVideoQuiz && IsAllowedVideoQuizImageKey(mediaKey);
        currentVideoMediaKey = canShowVideoQuiz ? FindAllowedVideoQuizFileKey(mediaKey) : string.Empty;
        currentVideoClip = !string.IsNullOrEmpty(currentVideoMediaKey) ? FindVideoClipFor(currentVideoMediaKey) : null;

        if (resultTitleText != null)
        {
            resultTitleText.text = title;
        }

        if (resultDescriptionText != null)
        {
            resultDescriptionText.text = description;
        }

        if (resultStatusText != null)
        {
            resultStatusText.text = AppLanguage.TranslateKnown(sourceLabel);
        }

        if (resultPreviewImage != null)
        {
            resultPreviewImage.enabled = previewImage != null;
            resultPreviewImage.sprite = previewImage;
            resultPreviewImage.preserveAspect = true;
        }

        EnsureResult3DPreview();
        result3DPreview?.Show(previewImage);

        if (playPauseButtonText != null)
        {
            playPauseButtonText.text = AppLanguage.Text("replay_pause");
        }

        SetPlayVideoButtonVisible(!string.IsNullOrEmpty(currentVideoMediaKey));

        if (resultOverlay != null)
        {
            resultOverlay.SetActive(true);
            resultOverlay.transform.SetAsLastSibling();
        }

        SetQrShortcutVisible(false);

        if (scanChoiceOverlay != null)
        {
            scanChoiceOverlay.SetActive(false);
        }

        if (cameraHintPanel != null)
        {
            cameraHintPanel.SetActive(false);
        }

        if (qrScanOverlay != null)
        {
            qrScanOverlay.SetActive(false);
        }
    }

    /// <summary>
    /// Shows a short status message in the same live hint area.
    /// </summary>
    public void ShowStatusMessage(string message)
    {
        ShowCameraHint(message);
    }

    private void HandleCameraModeButtonPressed()
    {
        if (scanChoiceOverlay != null)
        {
            scanChoiceOverlay.SetActive(false);
        }

        SetQrShortcutVisible(true);
        imageRecognitionManager?.BeginCameraScan();
    }

    private void HandleQrModeButtonPressed()
    {
        if (scanChoiceOverlay != null)
        {
            scanChoiceOverlay.SetActive(false);
        }

        SetQrShortcutVisible(false);
        imageRecognitionManager?.BeginQrMarkerScan();
    }

    private void HandleGalleryModeButtonPressed()
    {
        if (scanChoiceOverlay != null)
        {
            scanChoiceOverlay.SetActive(false);
        }

        SetQrShortcutVisible(false);
        imageRecognitionManager?.BeginGalleryScan();
    }

    private void HandleQrClosePressed()
    {
        HideQrScanOverlay();
        imageRecognitionManager?.CloseQrMarkerScan();
    }

    private void HandlePlayPausePressed()
    {
        if (audioManager == null)
        {
            return;
        }

        audioManager.ToggleNarration(currentAudioClip, currentNarrationText);
    }

    private void HandlePlayVideoPressed()
    {
        if (string.IsNullOrEmpty(currentVideoMediaKey))
        {
            SetPlayVideoButtonVisible(false);
            ShowStatusMessage(AppLanguage.Text("no_video_available"));
            return;
        }

        audioManager?.StopNarration();
        EnsureVideoQuizUi();
        EnsureVideoPlayer();

        if (videoQuizOverlay != null)
        {
            videoQuizOverlay.SetActive(true);
            videoQuizOverlay.transform.SetAsLastSibling();
        }

        if (videoTitleText != null)
        {
            videoTitleText.text = AppLanguage.Text("video_lesson") + ": " + currentItemTitle;
        }

        if (videoHintText != null)
        {
            videoHintText.text = AppLanguage.Text("video_loading");
        }

        SetVideoQuizStage(videoStageGroup);
        StartVideoPlayback(currentVideoMediaKey, currentVideoClip);
    }

    private void HandleCloseResultPressed()
    {
        audioManager?.StopNarration();
        CloseVideoQuiz();
        currentVideoClip = null;
        currentVideoMediaKey = string.Empty;
        SetPlayVideoButtonVisible(false);

        if (resultOverlay != null)
        {
            resultOverlay.SetActive(false);
        }

        result3DPreview?.Hide();
        SetQrShortcutVisible(true);
        imageRecognitionManager?.ClearRecognitionLock();
    }

    private void HideAllScanOverlays()
    {
        audioManager?.StopNarration();
        CloseVideoQuiz();
        currentVideoClip = null;
        currentVideoMediaKey = string.Empty;
        SetPlayVideoButtonVisible(false);

        if (scanChoiceOverlay != null)
        {
            scanChoiceOverlay.SetActive(false);
        }

        if (cameraHintPanel != null)
        {
            cameraHintPanel.SetActive(false);
        }

        if (qrScanOverlay != null)
        {
            qrScanOverlay.SetActive(false);
        }

        if (resultOverlay != null)
        {
            resultOverlay.SetActive(false);
        }

        result3DPreview?.Hide();
        SetQrShortcutVisible(true);
        imageRecognitionManager?.CancelScanMode();
    }

    private void EnsureResult3DPreview()
    {
        if (result3DPreview != null || resultPreviewImage == null)
        {
            return;
        }

        Transform previewParent = resultPreviewImage.transform.parent;
        GameObject host = previewParent != null ? previewParent.gameObject : resultPreviewImage.gameObject;
        result3DPreview = host.GetComponent<ScanResult3DPreview>();
        if (result3DPreview == null)
        {
            result3DPreview = host.AddComponent<ScanResult3DPreview>();
        }

        result3DPreview.Configure(resultPreviewImage);
    }

    private void EnsureVideoQuizUi()
    {
        LayoutResultButtons();

        if (videoQuizOverlay != null || resultOverlay == null)
        {
            return;
        }

        Transform overlayParent = resultOverlay.transform.parent != null
            ? resultOverlay.transform.parent
            : resultOverlay.transform;

        RectTransform overlay = NewRect("VideoQuizOverlay", overlayParent);
        videoQuizOverlay = overlay.gameObject;
        StretchToParent(overlay);

        Image overlayImage = videoQuizOverlay.AddComponent<Image>();
        overlayImage.color = new Color(0.02f, 0.04f, 0.07f, 0.92f);
        overlayImage.raycastTarget = true;

        RectTransform card = NewRect("VideoQuizCard", overlay);
        SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 900f), new Vector2(0.5f, 0.5f));
        Image cardImage = card.gameObject.AddComponent<Image>();
        cardImage.color = new Color(0.98f, 0.985f, 0.975f, 1f);
        cardImage.raycastTarget = true;
        Shadow cardShadow = card.gameObject.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
        cardShadow.effectDistance = new Vector2(0f, -10f);

        Text header = CreateText("VideoQuizHeader", card, AppLanguage.Text("video_lesson"), 34, new Color(0.08f, 0.11f, 0.16f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold, true);
        SetRect(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(34f, -50f), new Vector2(-140f, 78f), new Vector2(0f, 1f));
        videoTitleText = header;

        closeVideoQuizButton = CreateButton("CloseVideoQuizButton", card, "X", new Color(0.12f, 0.14f, 0.18f, 1f), Color.white, 24, FontStyle.Bold);
        SetRect(closeVideoQuizButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-54f, -50f), new Vector2(74f, 56f), new Vector2(0.5f, 0.5f));

        RectTransform divider = NewRect("VideoQuizDivider", card);
        SetRect(divider, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(34f, -105f), new Vector2(-68f, 2f), new Vector2(0f, 1f));
        Image dividerImage = divider.gameObject.AddComponent<Image>();
        dividerImage.color = new Color(0.16f, 0.46f, 0.88f, 0.16f);

        videoStageGroup = NewRect("VideoStage", card).gameObject;
        StretchToParent(videoStageGroup.GetComponent<RectTransform>());
        BuildVideoStage(videoStageGroup.transform);

        quizStageGroup = NewRect("QuizStage", card).gameObject;
        StretchToParent(quizStageGroup.GetComponent<RectTransform>());
        BuildQuizStage(quizStageGroup.transform);

        quizResultGroup = NewRect("QuizResultStage", card).gameObject;
        StretchToParent(quizResultGroup.GetComponent<RectTransform>());
        BuildQuizResultStage(quizResultGroup.transform);

        videoQuizOverlay.SetActive(false);
    }

    private void EnsurePlayVideoButton()
    {
        if (playPauseButton == null || closeResultButton == null)
        {
            return;
        }

        RectTransform buttonParent = playPauseButton.transform.parent as RectTransform;
        if (buttonParent == null)
        {
            return;
        }

        LayoutResultButtons();

        if (playVideoButton != null)
        {
            return;
        }

        playVideoButton = CreateButton("PlayScanVideoButton", buttonParent, AppLanguage.Text("play_video"), new Color(0.17f, 0.58f, 0.49f, 1f), Color.white, 22, FontStyle.Bold);
        playVideoButtonText = playVideoButton.GetComponentInChildren<Text>();
        playVideoButton.onClick.RemoveListener(HandlePlayVideoPressed);
        playVideoButton.onClick.AddListener(HandlePlayVideoPressed);
        LayoutResultButtons();
        playVideoButton.gameObject.SetActive(false);
    }

    private void LayoutResultButtons()
    {
        PositionBottomButton(playPauseButton, -160f, 250f, 74f, 56f);
        PositionBottomButton(closeResultButton, 160f, 250f, 74f, 56f);

        if (playVideoButton == null)
        {
            return;
        }

        RectTransform buttonRect = playVideoButton.GetComponent<RectTransform>();
        SetRect(buttonRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 154f), new Vector2(340f, 56f), new Vector2(0.5f, 0.5f));
    }

    private void BuildVideoStage(Transform parent)
    {
        RectTransform frame = NewRect("VideoFrame", parent);
        SetRect(frame, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -350f), new Vector2(820f, 462f), new Vector2(0.5f, 0.5f));
        Image frameImage = frame.gameObject.AddComponent<Image>();
        frameImage.color = new Color(0.04f, 0.06f, 0.09f, 1f);
        frameImage.raycastTarget = true;
        Shadow frameShadow = frame.gameObject.AddComponent<Shadow>();
        frameShadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
        frameShadow.effectDistance = new Vector2(0f, -6f);

        RectTransform rawRect = NewRect("VideoImage", frame);
        SetRect(rawRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(788f, 430f), new Vector2(0.5f, 0.5f));
        videoImage = rawRect.gameObject.AddComponent<RawImage>();
        videoImage.color = Color.white;
        videoImage.raycastTarget = false;

        videoHintText = CreateText("VideoHintText", parent, AppLanguage.Text("video_watch_hint"), 24, new Color(0.34f, 0.39f, 0.46f, 1f), TextAnchor.MiddleCenter, FontStyle.Normal, true);
        SetRect(videoHintText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -625f), new Vector2(780f, 86f), new Vector2(0.5f, 0.5f));
    }

    private void BuildQuizStage(Transform parent)
    {
        Text quizHeader = CreateText("QuizHeader", parent, AppLanguage.Text("short_quiz"), 32, new Color(0.16f, 0.46f, 0.88f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, true);
        SetRect(quizHeader.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -145f), new Vector2(780f, 56f), new Vector2(0.5f, 0.5f));

        quizProgressText = CreateText("QuizProgressText", parent, "Question 1 / 5", 22, new Color(0.34f, 0.39f, 0.46f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, true);
        SetRect(quizProgressText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -195f), new Vector2(760f, 42f), new Vector2(0.5f, 0.5f));

        quizQuestionText = CreateText("QuizQuestionText", parent, "", 28, new Color(0.08f, 0.11f, 0.16f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, true);
        quizQuestionText.verticalOverflow = VerticalWrapMode.Overflow;
        SetRect(quizQuestionText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -285f), new Vector2(780f, 110f), new Vector2(0.5f, 0.5f));

        quizChoiceButtons = new Button[4];
        quizChoiceTexts = new Text[4];
        for (int i = 0; i < quizChoiceButtons.Length; i++)
        {
            int capturedIndex = i;
            Button choiceButton = CreateButton("QuizChoiceButton_" + i, parent, "", new Color(0.96f, 0.98f, 1f, 1f), new Color(0.08f, 0.11f, 0.16f, 1f), 22, FontStyle.Bold);
            RectTransform choiceRect = choiceButton.GetComponent<RectTransform>();
            SetRect(choiceRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -410f - (i * 90f)), new Vector2(760f, 76f), new Vector2(0.5f, 0.5f));
            choiceButton.onClick.AddListener(() => HandleQuizChoice(capturedIndex));

            Text choiceText = choiceButton.GetComponentInChildren<Text>();
            choiceText.alignment = TextAnchor.MiddleLeft;
            choiceText.rectTransform.offsetMin = new Vector2(28f, 0f);
            choiceText.rectTransform.offsetMax = new Vector2(-24f, 0f);
            quizChoiceButtons[i] = choiceButton;
            quizChoiceTexts[i] = choiceText;
        }
    }

    private void BuildQuizResultStage(Transform parent)
    {
        quizResultTitleText = CreateText("QuizResultTitle", parent, AppLanguage.Text("congratulations"), 46, new Color(0.17f, 0.58f, 0.49f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, true);
        SetRect(quizResultTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(780f, 86f), new Vector2(0.5f, 0.5f));

        quizResultScoreText = CreateText("QuizResultScore", parent, "", 34, new Color(0.08f, 0.11f, 0.16f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, true);
        SetRect(quizResultScoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(780f, 72f), new Vector2(0.5f, 0.5f));

        Text resultBody = CreateText("QuizResultBody", parent, AppLanguage.Text("quiz_complete_body"), 24, new Color(0.34f, 0.39f, 0.46f, 1f), TextAnchor.MiddleCenter, FontStyle.Normal, true);
        resultBody.verticalOverflow = VerticalWrapMode.Overflow;
        SetRect(resultBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -465f), new Vector2(760f, 110f), new Vector2(0.5f, 0.5f));

        quizDoneButton = CreateButton("QuizDoneButton", parent, AppLanguage.Text("done"), new Color(0.16f, 0.46f, 0.88f, 1f), Color.white, 24, FontStyle.Bold);
        SetRect(quizDoneButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 86f), new Vector2(360f, 76f), new Vector2(0.5f, 0.5f));
    }

    private void StartVideoPlayback(string mediaKey, VideoClip fallbackClip)
    {
        StopVideoPlayback();

        if (videoPlayer == null)
        {
            ShowQuizStage();
            return;
        }

        string videoUrl = BuildStreamingVideoUrl(mediaKey);
        videoPlayer.time = 0d;
        videoPlayer.frame = 0;

        if (!string.IsNullOrEmpty(videoUrl))
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.clip = null;
            videoPlayer.url = videoUrl;
        }
        else if (fallbackClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.url = string.Empty;
            videoPlayer.clip = fallbackClip;
        }
        else
        {
            ShowQuizStage();
            return;
        }

        videoPlayer.time = 0d;
        videoPlayer.Prepare();

        videoPrepareRoutine = StartCoroutine(PlayVideoWhenReady());
    }

    private IEnumerator PlayVideoWhenReady()
    {
        float startedAt = Time.unscaledTime;
        while (videoPlayer != null && !videoPlayer.isPrepared && Time.unscaledTime - startedAt < 8f)
        {
            yield return null;
        }

        if (videoPlayer == null)
        {
            yield break;
        }

        if (videoHintText != null)
        {
            videoHintText.text = AppLanguage.Text("video_watch_hint");
        }

        videoPlayer.Play();
        videoPrepareRoutine = null;
    }

    private void HandleVideoFinished(VideoPlayer source)
    {
        if (source != videoPlayer)
        {
            return;
        }

        ShowQuizStage();
    }

    private void HandleVideoError(VideoPlayer source, string message)
    {
        if (source != videoPlayer || videoHintText == null)
        {
            return;
        }

        videoHintText.text = AppLanguage.Text("video_error");
    }

    private void ShowQuizStage()
    {
        StopVideoPlayback();

        currentQuiz.Clear();
        currentQuiz.AddRange(BuildQuiz(currentItemTitle, currentItemDescription));
        currentQuizIndex = 0;
        currentQuizScore = 0;
        quizAcceptingInput = true;

        SetVideoQuizStage(quizStageGroup);
        ShowCurrentQuizQuestion();
    }

    private void HandleQuizChoice(int choiceIndex)
    {
        if (!quizAcceptingInput || currentQuizIndex < 0 || currentQuizIndex >= currentQuiz.Count)
        {
            return;
        }

        QuizQuestion question = currentQuiz[currentQuizIndex];
        if (choiceIndex == question.CorrectIndex)
        {
            currentQuizScore++;
        }

        currentQuizIndex++;
        if (currentQuizIndex >= currentQuiz.Count)
        {
            ShowQuizResult();
            return;
        }

        ShowCurrentQuizQuestion();
    }

    private void ShowCurrentQuizQuestion()
    {
        if (currentQuizIndex < 0 || currentQuizIndex >= currentQuiz.Count)
        {
            ShowQuizResult();
            return;
        }

        QuizQuestion question = currentQuiz[currentQuizIndex];

        if (quizProgressText != null)
        {
            quizProgressText.text = AppLanguage.Text("question") + " " + (currentQuizIndex + 1) + " / " + currentQuiz.Count;
        }

        if (quizQuestionText != null)
        {
            quizQuestionText.text = question.Question;
        }

        for (int i = 0; i < quizChoiceButtons.Length; i++)
        {
            bool hasChoice = question.Choices != null && i < question.Choices.Length;
            quizChoiceButtons[i].gameObject.SetActive(hasChoice);

            if (hasChoice && quizChoiceTexts[i] != null)
            {
                char label = (char)('A' + i);
                quizChoiceTexts[i].text = label + ". " + question.Choices[i];
            }
        }
    }

    private void ShowQuizResult()
    {
        quizAcceptingInput = false;
        SetVideoQuizStage(quizResultGroup);

        if (quizResultTitleText != null)
        {
            quizResultTitleText.text = AppLanguage.Text("congratulations");
        }

        if (quizResultScoreText != null)
        {
            quizResultScoreText.text = AppLanguage.Text("score") + ": " + currentQuizScore + " / " + currentQuiz.Count;
        }
    }

    private List<QuizQuestion> BuildQuiz(string title, string description)
    {
        string itemTitle = string.IsNullOrWhiteSpace(title)
            ? AppLanguage.Text("scanned_item").ToLowerInvariant()
            : title;

        return new List<QuizQuestion>
        {
            new QuizQuestion(
                "Ano ang pangunahing ipinakita sa video?",
                new[]
                {
                    itemTitle,
                    "Mini Map route ng app",
                    "Home screen language menu",
                    "Camera permission settings"
                },
                0),
            new QuizQuestion(
                "Ano ang dapat gawin bago sagutin ang quiz?",
                new[]
                {
                    "Isara agad ang result panel",
                    "Pindutin lang ang back button",
                    "Panoorin muna ang video",
                    "I-off ang audio"
                },
                2),
            new QuizQuestion(
                "Saan lumalabas ang video lesson sa system?",
                new[]
                {
                    "Sa phone settings",
                    "Sa scan result ng matched heritage item",
                    "Sa Android install screen",
                    "Sa minimap floor selector"
                },
                1),
            new QuizQuestion(
                "Ano ang magandang gawin habang tinitingnan ang artifact?",
                new[]
                {
                    "Takpan ang camera",
                    "Lumayo agad sa exhibit",
                    "Palitan ang wika bawat segundo",
                    "Basahin o pakinggan ang paliwanag at obserbahan ang detalye"
                },
                3),
            new QuizQuestion(
                "Bakit mahalaga ang ganitong scan-and-video feature?",
                new[]
                {
                    "Mas madaling matutunan ang kasaysayan ng Aguinaldo Shrine",
                    "Para gawing mas malaki ang APK lang",
                    "Para mawala ang gallery",
                    "Para iwasan ang lahat ng impormasyon"
                },
                0)
        };
    }

    private void CloseVideoQuiz()
    {
        StopVideoPlayback();
        quizAcceptingInput = false;

        if (videoQuizOverlay != null)
        {
            videoQuizOverlay.SetActive(false);
        }
    }

    private void StopVideoPlayback()
    {
        if (videoPrepareRoutine != null)
        {
            StopCoroutine(videoPrepareRoutine);
            videoPrepareRoutine = null;
        }

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null;
            videoPlayer.url = string.Empty;
        }
    }

    private void EnsureVideoPlayer()
    {
        if (videoPlayer == null)
        {
            GameObject playerObject = new GameObject("ScanVideoQuizPlayer");
            playerObject.transform.SetParent(transform, false);
            videoPlayer = playerObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            videoPlayer.controlledAudioTrackCount = 1;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.loopPointReached += HandleVideoFinished;
            videoPlayer.errorReceived += HandleVideoError;
        }

        if (videoRenderTexture == null)
        {
            videoRenderTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
            videoRenderTexture.name = "ScanVideoQuizRenderTexture";
            videoRenderTexture.Create();
        }

        videoPlayer.targetTexture = videoRenderTexture;
        if (videoImage != null)
        {
            videoImage.texture = videoRenderTexture;
        }
    }

    private void ReleaseVideoResources()
    {
        StopVideoPlayback();

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= HandleVideoFinished;
            videoPlayer.errorReceived -= HandleVideoError;
        }

        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            Destroy(videoRenderTexture);
            videoRenderTexture = null;
        }
    }

    private void SetVideoQuizStage(GameObject activeStage)
    {
        if (videoStageGroup != null)
        {
            videoStageGroup.SetActive(videoStageGroup == activeStage);
        }

        if (quizStageGroup != null)
        {
            quizStageGroup.SetActive(quizStageGroup == activeStage);
        }

        if (quizResultGroup != null)
        {
            quizResultGroup.SetActive(quizResultGroup == activeStage);
        }
    }

    private VideoClip FindVideoClipFor(string mediaKey)
    {
        string requestedKey = FindAllowedVideoQuizKey(mediaKey);
        if (string.IsNullOrEmpty(requestedKey))
        {
            return null;
        }

        for (int i = 0; i < AllowedVideoQuizImageKeys.Length; i++)
        {
            string allowedKey = NormalizeVideoKey(AllowedVideoQuizImageKeys[i]);
            if (string.Equals(requestedKey, allowedKey, StringComparison.OrdinalIgnoreCase))
            {
                return FindVideoClipAtBindingIndex(i, requestedKey);
            }
        }

        return null;
    }

    private VideoClip FindVideoClipAtBindingIndex(int bindingIndex, string requestedKey)
    {
        if (videoQuizClips == null || videoQuizClips.Length == 0)
        {
            return null;
        }

        if (bindingIndex >= 0 && bindingIndex < videoQuizClips.Length)
        {
            VideoClip boundClip = videoQuizClips[bindingIndex];
            string boundKey = videoQuizImageKeys != null && bindingIndex < videoQuizImageKeys.Length
                ? NormalizeVideoKey(videoQuizImageKeys[bindingIndex])
                : string.Empty;

            if (boundClip != null && string.Equals(requestedKey, boundKey, StringComparison.OrdinalIgnoreCase))
            {
                return boundClip;
            }
        }

        for (int i = 0; i < videoQuizClips.Length; i++)
        {
            VideoClip clip = videoQuizClips[i];
            if (clip == null)
            {
                continue;
            }

            string bindingKey = videoQuizImageKeys != null && i < videoQuizImageKeys.Length
                ? NormalizeVideoKey(videoQuizImageKeys[i])
                : string.Empty;
            string clipKey = NormalizeVideoKey(clip.name);

            if (string.IsNullOrEmpty(bindingKey))
            {
                bindingKey = clipKey;
            }

            if (!AllowedVideoQuizImageKeySet.Contains(bindingKey))
            {
                continue;
            }

            if (string.Equals(requestedKey, bindingKey, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrEmpty(clipKey) || string.Equals(requestedKey, clipKey, StringComparison.OrdinalIgnoreCase)))
            {
                return clip;
            }
        }

        return null;
    }

    private static string FindAllowedVideoQuizKey(string mediaKey)
    {
        return NormalizeVideoKey(FindAllowedVideoQuizFileKey(mediaKey));
    }

    private static string FindAllowedVideoQuizFileKey(string mediaKey)
    {
        string normalizedMediaKey = NormalizeVideoKey(mediaKey);
        if (string.IsNullOrEmpty(normalizedMediaKey))
        {
            return string.Empty;
        }

        for (int i = 0; i < AllowedVideoQuizImageKeys.Length; i++)
        {
            string allowedKey = AllowedVideoQuizImageKeys[i];
            if (string.Equals(normalizedMediaKey, NormalizeVideoKey(allowedKey), StringComparison.OrdinalIgnoreCase))
            {
                return allowedKey;
            }
        }

        return string.Empty;
    }

    private static string BuildStreamingVideoUrl(string mediaKey)
    {
        string fileKey = FindAllowedVideoQuizFileKey(mediaKey);
        if (string.IsNullOrEmpty(fileKey))
        {
            return string.Empty;
        }

        string basePath = Application.streamingAssetsPath.Replace("\\", "/").TrimEnd('/');
        return $"{basePath}/video/{fileKey}.mp4";
    }

    private void SetPlayVideoButtonVisible(bool visible)
    {
        if (!visible)
        {
            DestroyPlayVideoButton();
            LayoutResultButtons();
            return;
        }

        DestroyPlayVideoButton();
        EnsurePlayVideoButton();

        if (playVideoButton == null)
        {
            return;
        }

        playVideoButton.gameObject.SetActive(visible);
        playVideoButton.interactable = true;
        playVideoButton.enabled = true;

        if (playVideoButtonText != null)
        {
            playVideoButtonText.text = AppLanguage.Text("play_video");
        }
    }

    private void DestroyPlayVideoButton()
    {
        playVideoButton = null;
        playVideoButtonText = null;

        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || button.gameObject == null)
            {
                continue;
            }

            if (!string.Equals(button.gameObject.name, "PlayScanVideoButton", StringComparison.Ordinal))
            {
                continue;
            }

            if (button.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            button.onClick.RemoveListener(HandlePlayVideoPressed);
            button.interactable = false;
            button.enabled = false;
            button.gameObject.SetActive(false);
            Destroy(button.gameObject);
        }
    }

    private void SetQrShortcutVisible(bool visible)
    {
        if (qrShortcutButton == null)
        {
            return;
        }

        GameObject buttonObject = qrShortcutButton.gameObject;
        if (buttonObject.activeSelf != visible)
        {
            buttonObject.SetActive(visible);
        }

        if (visible)
        {
            qrShortcutButton.transform.SetAsLastSibling();
        }
    }

    private static string NormalizeVideoKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string fileName = value.Trim().Replace('\\', '/');
        int lastSlashIndex = fileName.LastIndexOf('/');
        if (lastSlashIndex >= 0 && lastSlashIndex < fileName.Length - 1)
        {
            fileName = fileName.Substring(lastSlashIndex + 1);
        }

        bool strippedExtension;
        do
        {
            strippedExtension = false;
            string lowerFileName = fileName.ToLowerInvariant();
            for (int i = 0; i < KnownMediaKeyExtensions.Length; i++)
            {
                string extension = KnownMediaKeyExtensions[i];
                if (lowerFileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    fileName = fileName.Substring(0, fileName.Length - extension.Length);
                    strippedExtension = true;
                    break;
                }
            }
        }
        while (strippedExtension);

        return fileName.Trim().ToLowerInvariant();
    }

    private static HashSet<string> BuildAllowedVideoQuizImageKeySet()
    {
        HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < AllowedVideoQuizImageKeys.Length; i++)
        {
            string key = NormalizeVideoKey(AllowedVideoQuizImageKeys[i]);
            if (!string.IsNullOrEmpty(key))
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
        rectTransform.pivot = pivot;
    }

    private static Text CreateText(
        string name,
        Transform parent,
        string text,
        int fontSize,
        Color color,
        TextAnchor alignment,
        FontStyle fontStyle,
        bool bestFit)
    {
        RectTransform rectTransform = NewRect(name, parent);
        Text textComponent = rectTransform.gameObject.AddComponent<Text>();
        textComponent.font = GetBuiltInFont();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.color = color;
        textComponent.alignment = alignment;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Truncate;
        textComponent.resizeTextForBestFit = bestFit;
        textComponent.resizeTextMinSize = Mathf.Max(10, Mathf.RoundToInt(fontSize * 0.58f));
        textComponent.resizeTextMaxSize = fontSize;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color backgroundColor, Color textColor, int fontSize, FontStyle fontStyle)
    {
        RectTransform rectTransform = NewRect(name, parent);
        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.color = backgroundColor;
        image.raycastTarget = true;

        Shadow shadow = rectTransform.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.20f);
        shadow.effectDistance = new Vector2(0f, -4f);

        Button button = rectTransform.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.10f);
        colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.10f);
        colors.selectedColor = Color.Lerp(backgroundColor, Color.white, 0.06f);
        colors.disabledColor = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Text buttonText = CreateText("Text", rectTransform, label, fontSize, textColor, TextAnchor.MiddleCenter, fontStyle, true);
        StretchToParent(buttonText.rectTransform);
        buttonText.raycastTarget = false;

        return button;
    }

    private static void PositionBottomButton(Button button, float positionX, float width, float positionY, float height)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return;
        }

        SetRect(rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(positionX, positionY), new Vector2(width, height), new Vector2(0.5f, 0.5f));
    }

    private static Font GetBuiltInFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }
}
