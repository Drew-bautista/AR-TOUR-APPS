using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PremiumTourUIStyler : MonoBehaviour
{
    private static readonly Color Primary = new Color32(0x2D, 0x9C, 0xDB, 0xFF);
    private static readonly Color Secondary = new Color32(0x1B, 0x3A, 0x57, 0xFF);
    private static readonly Color Accent = new Color32(0x27, 0xAE, 0x60, 0xFF);
    private static readonly Color Warning = new Color32(0xF2, 0x99, 0x4A, 0xFF);
    private static readonly Color Danger = new Color32(0xC9, 0x33, 0x33, 0xFF);
    private static readonly Color Glass = new Color(0.07f, 0.07f, 0.07f, 0.76f);
    private static readonly Color GlassStrong = new Color(0.05f, 0.06f, 0.08f, 0.88f);
    private static readonly Color GlassStroke = new Color(1f, 1f, 1f, 0.12f);
    private static readonly Color TextPrimary = Color.white;
    private static readonly Color TextSecondary = new Color32(0xD8, 0xE1, 0xEA, 0xFF);
    private static readonly Color TextMuted = new Color32(0xA8, 0xB4, 0xC2, 0xFF);
    private static readonly Color LightSurface = new Color32(0xF3, 0xF7, 0xFA, 0xF8);

    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool showLoadingFlash = true;

    private Sprite roundedSprite;
    private Sprite buttonSprite;
    private Sprite circleSprite;
    private Font uiFont;
    private RectTransform canvasRect;

    private void Awake()
    {
        EnsureAssets();
    }

    private void Start()
    {
        if (applyOnStart)
        {
            StartCoroutine(ApplyRepeatedly());
        }
    }

    public void ApplyNow()
    {
        EnsureAssets();
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : transform as RectTransform;

        StylePanels();
        StyleTexts();
        StyleButtons();
        StyleGallery();
        StyleScanUi();
        EnsureAudioIndicator();
        EnsureLoadingOverlay();
        AppLanguage.ApplyToTextTree(transform);
        StyleQrShortcutButton();
    }

    private IEnumerator ApplyRepeatedly()
    {
        if (showLoadingFlash)
        {
            EnsureLoadingOverlay();
        }

        for (int i = 0; i < 16; i++)
        {
            ApplyNow();
            yield return new WaitForSecondsRealtime(i < 4 ? 0.15f : 0.45f);
        }
    }

    private void EnsureAssets()
    {
        if (roundedSprite == null)
        {
            roundedSprite = CreateRoundedSprite(128, 128, 14);
        }

        if (buttonSprite == null)
        {
            buttonSprite = CreateRoundedSprite(192, 96, 16);
        }

        if (circleSprite == null)
        {
            circleSprite = CreateCircleSprite(96);
        }

        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    private void StylePanels()
    {
        RectTransform topPanel = FindRect("TopPanel");
        if (topPanel != null)
        {
            topPanel.anchorMin = new Vector2(0.035f, 1f);
            topPanel.anchorMax = new Vector2(0.965f, 1f);
            topPanel.pivot = new Vector2(0.5f, 1f);
            topPanel.sizeDelta = new Vector2(0f, 124f);
            topPanel.anchoredPosition = new Vector2(0f, -24f);
            StyleImage(topPanel, GlassStrong, roundedSprite, true);
            EnsureShadow(topPanel, new Color(0f, 0f, 0f, 0.42f), new Vector2(0f, -8f));
            EnsureOutline(topPanel, GlassStroke, new Vector2(1f, -1f));
        }

        RectTransform cameraHintPanel = FindRect("CameraHintPanel");
        if (cameraHintPanel != null)
        {
            cameraHintPanel.anchorMin = new Vector2(0.5f, 1f);
            cameraHintPanel.anchorMax = new Vector2(0.5f, 1f);
            cameraHintPanel.pivot = new Vector2(0.5f, 1f);
            cameraHintPanel.sizeDelta = new Vector2(760f, 52f);
            cameraHintPanel.anchoredPosition = new Vector2(0f, -184f);
        }

        RectTransform centerBadge = FindRect("CenterBadge");
        if (centerBadge != null)
        {
            centerBadge.anchorMin = new Vector2(0.5f, 0.64f);
            centerBadge.anchorMax = new Vector2(0.5f, 0.64f);
            centerBadge.pivot = new Vector2(0.5f, 0.5f);
            centerBadge.sizeDelta = new Vector2(520f, 82f);
            centerBadge.anchoredPosition = Vector2.zero;
            StyleImage(centerBadge, GlassStrong, buttonSprite, true);
            EnsureShadow(centerBadge, new Color(0f, 0f, 0f, 0.48f), new Vector2(0f, -8f));
            EnsureOutline(centerBadge, new Color(1f, 1f, 1f, 0.14f), new Vector2(1f, -1f));
        }

        RectTransform bottomPanel = FindRect("BottomPanel");
        if (bottomPanel != null)
        {
            bottomPanel.anchorMin = new Vector2(0.035f, 0f);
            bottomPanel.anchorMax = new Vector2(0.965f, 0f);
            bottomPanel.pivot = new Vector2(0.5f, 0f);
            bottomPanel.sizeDelta = new Vector2(0f, 650f);
            bottomPanel.anchoredPosition = new Vector2(0f, 28f);
            StyleImage(bottomPanel, new Color(0.05f, 0.06f, 0.075f, 0.9f), roundedSprite, true);
            EnsureShadow(bottomPanel, new Color(0f, 0f, 0f, 0.5f), new Vector2(0f, -12f));
            EnsureOutline(bottomPanel, new Color(1f, 1f, 1f, 0.1f), new Vector2(1f, -1f));
        }

        RectTransform distanceBadge = FindRect("DistanceBadge");
        if (distanceBadge != null)
        {
            distanceBadge.sizeDelta = new Vector2(236f, 62f);
            distanceBadge.anchoredPosition = new Vector2(-22f, -24f);
            StyleImage(distanceBadge, Primary, buttonSprite, true);
            EnsureShadow(distanceBadge, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -5f));
        }

        RectTransform mapFrame = FindRect("MapFrame");
        if (mapFrame != null)
        {
            mapFrame.anchorMin = new Vector2(0f, 0f);
            mapFrame.anchorMax = new Vector2(1f, 0f);
            mapFrame.pivot = new Vector2(0.5f, 0f);
            mapFrame.sizeDelta = new Vector2(-48f, 366f);
            mapFrame.anchoredPosition = new Vector2(0f, 122f);
            StyleImage(mapFrame, new Color(0.98f, 0.992f, 1f, 0.98f), roundedSprite, true);
            EnsureShadow(mapFrame, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -9f));
            EnsureOutline(mapFrame, new Color(0f, 0f, 0f, 0.08f), new Vector2(1f, -1f));
        }

        RectTransform mapViewport = FindRect("MapViewport");
        if (mapViewport != null)
        {
            mapViewport.anchorMin = new Vector2(0f, 0f);
            mapViewport.anchorMax = new Vector2(1f, 1f);
            mapViewport.pivot = new Vector2(0.5f, 0.5f);
            mapViewport.offsetMin = new Vector2(18f, 18f);
            mapViewport.offsetMax = new Vector2(-18f, -58f);
            StyleImage(mapViewport, new Color32(0xF2, 0xF7, 0xFA, 0xFF), roundedSprite, true);
            EnsureOutline(mapViewport, new Color(0f, 0f, 0f, 0.05f), new Vector2(1f, -1f));

            if (mapViewport.GetComponent<RectMask2D>() == null)
            {
                mapViewport.gameObject.AddComponent<RectMask2D>();
            }
        }
    }

    private void StyleTexts()
    {
        StyleText("TitleText", 26, FontStyle.Bold, TextPrimary, TextAnchor.UpperLeft);
        StyleText("StatusText", 16, FontStyle.Normal, TextSecondary, TextAnchor.UpperLeft);
        StyleText("CenterBadgeText", 24, FontStyle.Bold, TextPrimary, TextAnchor.MiddleCenter);
        StyleText("InstructionHeader", 16, FontStyle.Bold, TextMuted, TextAnchor.UpperLeft, AppLanguage.Text("direction"));
        StyleText("InstructionText", 34, FontStyle.Bold, TextPrimary, TextAnchor.UpperLeft);
        StyleText("ProgressText", 18, FontStyle.Bold, Primary, TextAnchor.UpperLeft);
        StyleText("DistanceText", 21, FontStyle.Bold, TextPrimary, TextAnchor.MiddleCenter);
        StyleText("DescriptionText", 17, FontStyle.Normal, TextSecondary, TextAnchor.UpperLeft);
        StyleText("MapTitle", 16, FontStyle.Bold, new Color32(0x21, 0x2A, 0x33, 0xFF), TextAnchor.UpperLeft);
        StyleText("CameraHintText", 15, FontStyle.Bold, TextPrimary, TextAnchor.MiddleCenter);

        RectTransform title = FindRect("TitleText");
        if (title != null)
        {
            title.anchorMin = new Vector2(0f, 1f);
            title.anchorMax = new Vector2(0.48f, 1f);
            title.pivot = new Vector2(0f, 1f);
            title.sizeDelta = new Vector2(-24f, 48f);
            title.anchoredPosition = new Vector2(22f, -14f);
        }

        RectTransform status = FindRect("StatusText");
        if (status != null)
        {
            status.anchorMin = new Vector2(0f, 1f);
            status.anchorMax = new Vector2(0.48f, 1f);
            status.pivot = new Vector2(0f, 1f);
            status.sizeDelta = new Vector2(-24f, 36f);
            status.anchoredPosition = new Vector2(22f, -66f);
        }

        RectTransform instructionHeader = FindRect("InstructionHeader");
        if (instructionHeader != null)
        {
            instructionHeader.anchoredPosition = new Vector2(24f, -18f);
        }

        RectTransform instruction = FindRect("InstructionText");
        if (instruction != null)
        {
            instruction.sizeDelta = new Vector2(-320f, 58f);
            instruction.anchoredPosition = new Vector2(24f, -52f);
        }

        RectTransform progress = FindRect("ProgressText");
        if (progress != null)
        {
            progress.anchoredPosition = new Vector2(24f, -108f);
        }

        RectTransform description = FindRect("DescriptionText");
        if (description != null)
        {
            description.sizeDelta = new Vector2(-48f, 58f);
            description.anchoredPosition = new Vector2(24f, -142f);
        }

        RectTransform mapTitle = FindRect("MapTitle");
        if (mapTitle != null)
        {
            mapTitle.anchorMin = new Vector2(0f, 1f);
            mapTitle.anchorMax = new Vector2(1f, 1f);
            mapTitle.pivot = new Vector2(0f, 1f);
            mapTitle.sizeDelta = new Vector2(-40f, 26f);
            mapTitle.anchoredPosition = new Vector2(20f, -14f);
        }
    }

    private void StyleButtons()
    {
        StyleButton("ScanItemButton", Accent, TextPrimary, AppLanguage.Text("scan_item"));
        StyleButton("GalleryButton", Warning, new Color32(0x10, 0x15, 0x1C, 0xFF), AppLanguage.Text("gallery"));
        LayoutTopBarActions();
        StyleButton("BackButton", new Color32(0xE8, 0xEC, 0xF2, 0xFF), new Color32(0x16, 0x20, 0x2A, 0xFF), AppLanguage.Text("back"));
        StyleButton("NextButton", Primary, TextPrimary, AppLanguage.Text("next"));
        StyleButton("ExitButton", new Color32(0x16, 0x18, 0x1F, 0xFF), TextPrimary, "X " + AppLanguage.Text("exit"));
        StyleButton("CloseGalleryButton", new Color32(0x16, 0x18, 0x1F, 0xFF), TextPrimary, AppLanguage.Text("close"));
        StyleButton("PreviousPhotoButton", new Color32(0xE8, 0xEC, 0xF2, 0xFF), new Color32(0x16, 0x20, 0x2A, 0xFF), AppLanguage.Text("previous"));
        StyleButton("NextPhotoButton", Primary, TextPrimary, AppLanguage.Text("next"));
        StyleButton("QrScanButton", Primary, TextPrimary, AppLanguage.Text("qr_scan"));
        StyleButton("UseCameraButton", Primary, TextPrimary, AppLanguage.Text("resume_scan"));
        StyleButton("PickGalleryButton", Accent, TextPrimary, AppLanguage.Text("pick_gallery"));
        StyleButton("CancelScanButton", new Color32(0x16, 0x18, 0x1F, 0xFF), TextPrimary, AppLanguage.Text("cancel"));
        StyleButton("PlayPauseScanAudioButton", Primary, TextPrimary, AppLanguage.Text("replay_pause"));
        StyleButton("CloseScanResultButton", new Color32(0x16, 0x18, 0x1F, 0xFF), TextPrimary, AppLanguage.Text("close"));
        StyleQrShortcutButton();

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].GetComponent<PremiumButtonFeedback>() == null)
            {
                buttons[i].gameObject.AddComponent<PremiumButtonFeedback>();
            }
        }
    }

    private void LayoutTopBarActions()
    {
        LayoutTopButton("MuteToggleButton", new Vector2(42f, 42f), new Vector2(-470f, -2f));
        LayoutTopButton("CameraToggleButton", new Vector2(42f, 42f), new Vector2(-416f, -2f));
        LayoutTopButton("ScanItemButton", new Vector2(172f, 48f), new Vector2(-226f, -2f));
        LayoutTopButton("GalleryButton", new Vector2(172f, 48f), new Vector2(-38f, -2f));
    }

    private void LayoutTopButton(string name, Vector2 size, Vector2 position)
    {
        RectTransform rect = FindRect(name);
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.SetAsLastSibling();

        Image image = rect.GetComponent<Image>();
        if (image != null && (name == "MuteToggleButton" || name == "CameraToggleButton"))
        {
            image.sprite = circleSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }
    }

    private void StyleQrShortcutButton()
    {
        RectTransform rect = FindRect("QrScanShortcutButton");
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(126f, 126f);
        rect.anchoredPosition = new Vector2(-92f, -312f);
        rect.SetAsLastSibling();

        Image image = rect.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = circleSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = new Color(0.28f, 0.31f, 0.36f, 0.96f);
            image.raycastTarget = true;
        }

        Button button = rect.GetComponent<Button>();
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.28f, 0.31f, 0.36f, 0.96f);
            colors.highlightedColor = new Color(0.38f, 0.42f, 0.48f, 1f);
            colors.pressedColor = new Color(0.18f, 0.2f, 0.24f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.28f, 0.31f, 0.36f, 0.45f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        Text label = rect.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.font = uiFont;
            label.text = "QR";
            label.fontSize = 36;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            Stretch(label.rectTransform);
        }
    }

    private void StyleGallery()
    {
        StyleOverlay("GalleryOverlay", new Color(0.02f, 0.025f, 0.035f, 0.82f));
        RectTransform galleryCard = FindRect("GalleryCard");
        if (galleryCard != null)
        {
            galleryCard.sizeDelta = new Vector2(980f, 1660f);
            StyleImage(galleryCard, new Color(0.06f, 0.07f, 0.09f, 0.94f), roundedSprite, true);
            EnsureShadow(galleryCard, new Color(0f, 0f, 0f, 0.52f), new Vector2(0f, -16f));
            EnsureOutline(galleryCard, GlassStroke, new Vector2(1f, -1f));
        }

        StyleText("GalleryHeader", 30, FontStyle.Bold, TextPrimary, TextAnchor.UpperLeft);
        StyleText("GalleryCounterText", 19, FontStyle.Bold, Primary, TextAnchor.UpperRight);
        StyleText("GalleryPhotoTitleText", 28, FontStyle.Bold, TextPrimary, TextAnchor.UpperLeft);
        StyleText("GalleryDefinitionText", 18, FontStyle.Normal, TextSecondary, TextAnchor.UpperLeft);
        StyleText("ListHeader", 20, FontStyle.Bold, TextSecondary, TextAnchor.UpperLeft);

        StyleLightFrame("PreviewFrame");
        StyleLightFrame("ListFrame");
    }

    private void StyleScanUi()
    {
        StyleOverlay("ScanChoiceOverlay", new Color(0.02f, 0.025f, 0.035f, 0.76f));
        StyleOverlay("ScanResultOverlay", new Color(0.02f, 0.025f, 0.035f, 0.8f));

        StyleDarkCard("ScanChoiceCard");
        StyleDarkCard("ScanResultCard");
        StyleDarkCard("CameraHintPanel");

        StyleText("ScanChoiceHeader", 32, FontStyle.Bold, TextPrimary, TextAnchor.UpperLeft);
        StyleText("ScanChoiceBody", 20, FontStyle.Normal, TextSecondary, TextAnchor.UpperLeft);
        StyleText("ScanResultHeader", 30, FontStyle.Bold, TextPrimary, TextAnchor.UpperLeft);
        StyleText("ScanResultStatusText", 17, FontStyle.Bold, Primary, TextAnchor.UpperLeft);
        StyleText("ScanResultTitleText", 28, FontStyle.Bold, TextPrimary, TextAnchor.UpperLeft);
        StyleText("ScanResultDescriptionText", 19, FontStyle.Normal, TextSecondary, TextAnchor.UpperLeft);
        StyleLightFrame("ScanPreviewFrame");
        LayoutScanDialogButtons();
    }

    private void LayoutScanDialogButtons()
    {
        LayoutBottomButton("CancelScanButton", new Vector2(320f, 68f), new Vector2(0.5f, 0f), new Vector2(0f, 64f));
        LayoutBottomButton("PlayPauseScanAudioButton", new Vector2(250f, 72f), new Vector2(0.32f, 0f), new Vector2(0f, 98f));
        LayoutBottomButton("CloseScanResultButton", new Vector2(250f, 72f), new Vector2(0.68f, 0f), new Vector2(0f, 98f));
    }

    private void LayoutBottomButton(string name, Vector2 size, Vector2 anchor, Vector2 position)
    {
        RectTransform rect = FindRect(name);
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private void EnsureAudioIndicator()
    {
        Transform existing = transform.Find("PremiumAudioIndicator");
        if (existing != null)
        {
            return;
        }

        GameObject root = new GameObject("PremiumAudioIndicator", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(transform, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(160f, 44f);
        rect.anchoredPosition = new Vector2(0f, -246f);

        Image background = root.GetComponent<Image>();
        background.sprite = buttonSprite;
        background.type = Image.Type.Sliced;
        background.color = new Color(0.02f, 0.025f, 0.035f, 0.84f);
        background.raycastTarget = false;

        Image dot = CreateChildImage("Pulse", rect, circleSprite, Accent, new Vector2(24f, 24f), new Vector2(-52f, 0f));
        Text label = CreateChildText("Label", rect, AppLanguage.Text("audio"), 15, FontStyle.Bold, TextPrimary);
        Stretch(label.rectTransform, new Vector2(38f, 2f), new Vector2(-14f, -2f));

        PremiumAudioIndicator indicator = root.AddComponent<PremiumAudioIndicator>();
        indicator.Configure(dot, label);
        CanvasGroup group = root.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = root.AddComponent<CanvasGroup>();
        }

        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    private void EnsureLoadingOverlay()
    {
        Transform existing = transform.Find("PremiumLoadingOverlay");
        if (existing != null)
        {
            return;
        }

        GameObject overlay = new GameObject("PremiumLoadingOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        overlay.transform.SetParent(transform, false);
        RectTransform rect = overlay.GetComponent<RectTransform>();
        Stretch(rect);
        Image image = overlay.GetComponent<Image>();
        image.color = new Color(0.02f, 0.025f, 0.035f, 0.72f);
        image.raycastTarget = true;

        Text text = CreateChildText("Text", rect, AppLanguage.Text("loading"), 28, FontStyle.Bold, TextPrimary);
        text.alignment = TextAnchor.MiddleCenter;
        Stretch(text.rectTransform);
        overlay.AddComponent<PremiumLoadingFade>();
        overlay.transform.SetAsLastSibling();
    }

    private void StyleOverlay(string name, Color color)
    {
        RectTransform rect = FindRect(name);
        if (rect == null)
        {
            return;
        }

        StyleImage(rect, color, null, false);
        if (rect.GetComponent<PremiumPopupAnimator>() == null)
        {
            rect.gameObject.AddComponent<PremiumPopupAnimator>();
        }
    }

    private void StyleDarkCard(string name)
    {
        RectTransform rect = FindRect(name);
        if (rect == null)
        {
            return;
        }

        StyleImage(rect, GlassStrong, roundedSprite, true);
        EnsureShadow(rect, new Color(0f, 0f, 0f, 0.52f), new Vector2(0f, -14f));
        EnsureOutline(rect, GlassStroke, new Vector2(1f, -1f));
    }

    private void StyleLightFrame(string name)
    {
        RectTransform rect = FindRect(name);
        if (rect == null)
        {
            return;
        }

        StyleImage(rect, LightSurface, roundedSprite, true);
        EnsureOutline(rect, new Color(0f, 0f, 0f, 0.06f), new Vector2(1f, -1f));
    }

    private void StyleButton(string name, Color backgroundColor, Color textColor, string labelText)
    {
        RectTransform rect = FindRect(name);
        if (rect == null)
        {
            return;
        }

        Image image = rect.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = backgroundColor;
        }

        Button button = rect.GetComponent<Button>();
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = backgroundColor;
            colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.12f);
            colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.14f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0.38f);
            button.colors = colors;
        }

        Text label = rect.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.text = labelText;
            label.font = uiFont;
            label.color = textColor;
            label.fontSize = Mathf.Clamp(label.fontSize, 18, 24);
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = Mathf.Max(20, label.fontSize);
        }

        EnsureShadow(rect, new Color(0f, 0f, 0f, 0.24f), new Vector2(0f, -4f));
    }

    private void StyleText(string name, int fontSize, FontStyle style, Color color, TextAnchor alignment, string replacement = null)
    {
        RectTransform rect = FindRect(name);
        Text text = rect != null ? rect.GetComponent<Text>() : null;
        if (text == null)
        {
            return;
        }

        if (replacement != null)
        {
            text.text = replacement;
        }

        text.font = uiFont;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(10, fontSize - 8);
        text.resizeTextMaxSize = fontSize;
    }

    private RectTransform FindRect(string name)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == name)
            {
                return children[i] as RectTransform;
            }
        }

        return null;
    }

    private void StyleImage(RectTransform rect, Color color, Sprite sprite, bool sliced)
    {
        Image image = rect.GetComponent<Image>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<Image>();
        }

        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = true;
        if (sliced && sprite != null)
        {
            image.type = Image.Type.Sliced;
        }
    }

    private void EnsureShadow(RectTransform rect, Color color, Vector2 distance)
    {
        Shadow shadow = rect.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = rect.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }

    private void EnsureOutline(RectTransform rect, Color color, Vector2 distance)
    {
        Outline outline = rect.GetComponent<Outline>();
        if (outline == null)
        {
            outline = rect.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = color;
        outline.effectDistance = distance;
    }

    private Image CreateChildImage(string name, RectTransform parent, Sprite sprite, Color color, Vector2 size, Vector2 position)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(Image));
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = child.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Text CreateChildText(string name, RectTransform parent, string content, int size, FontStyle style, Color color)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(Text));
        child.transform.SetParent(parent, false);
        Text text = child.GetComponent<Text>();
        text.font = uiFont;
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        Stretch(rect, Vector2.zero, Vector2.zero);
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static Sprite CreateRoundedSprite(int width, int height, int radius)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1f, 1f, 1f, 0f);
        float feather = 2f;
        Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
        Vector2 half = new Vector2(width * 0.5f - 1f, height * 0.5f - 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f) - center;
                Vector2 q = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - half + new Vector2(radius, radius);
                Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
                float distance = outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                float alpha = Mathf.Clamp01((feather - distance) / feather);
                texture.SetPixel(x, y, alpha > 0f ? new Color(1f, 1f, 1f, alpha) : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    private static Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;
        Color clear = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - distance);
                texture.SetPixel(x, y, alpha > 0f ? new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)) : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private sealed class PremiumLoadingFade : MonoBehaviour
    {
        private IEnumerator Start()
        {
            CanvasGroup group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }

            yield return new WaitForSecondsRealtime(0.35f);
            float elapsed = 0f;
            float duration = 0.35f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
