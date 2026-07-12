using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReviewUIController : MonoBehaviour
{
    [Header("Behavior")]
    [SerializeField] private bool showHomeSummary = true;
    [SerializeField] private bool buildUiOnStart = true;

    [Header("Runtime References")]
    [SerializeField] private ReviewManager reviewManager;
    [SerializeField] private Canvas targetCanvas;

    private RectTransform root;
    private RectTransform summaryPanel;
    private Text averageText;
    private Text totalText;
    private Text statusText;
    private RectTransform latestContent;
    private GameObject reviewPanel;
    private StarRatingController starRatingController;
    private InputField commentInput;
    private Button submitButton;
    private Button cancelButton;
    private bool uiBuilt;

    private static Font runtimeFont;
    private static Sprite roundedPanelSprite;

    private static readonly Color PanelColor = new Color(0.05f, 0.065f, 0.09f, 0.96f);
    private static readonly Color StrokeColor = new Color(1f, 1f, 1f, 0.14f);
    private static readonly Color PrimaryText = Color.white;
    private static readonly Color SecondaryText = new Color32(0xC4, 0xD2, 0xEC, 0xFF);
    private static readonly Color MutedText = new Color32(0x8A, 0x9C, 0xBE, 0xFF);
    private static readonly Color Accent = new Color32(0x3B, 0x82, 0xF6, 0xFF);

    public static ReviewUIController AttachToCanvas(Canvas canvas, bool includeHomeSummary)
    {
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("ReviewCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2340f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        EnsureEventSystem();

        Transform existing = canvas.transform.Find("ReviewUI");
        ReviewUIController controller;
        if (existing != null && existing.TryGetComponent(out controller))
        {
            controller.showHomeSummary = includeHomeSummary;
            controller.targetCanvas = canvas;
            controller.BuildUiIfNeeded();
            return controller;
        }

        GameObject rootObject = new GameObject("ReviewUI", typeof(RectTransform), typeof(ReviewUIController));
        rootObject.transform.SetParent(canvas.transform, false);
        controller = rootObject.GetComponent<ReviewUIController>();
        controller.showHomeSummary = includeHomeSummary;
        controller.targetCanvas = canvas;
        controller.BuildUiIfNeeded();
        return controller;
    }

    public static ReviewUIController EnsurePopupInScene()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        return AttachToCanvas(canvas, false);
    }

    private void Awake()
    {
        root = transform as RectTransform;
        if (reviewManager == null)
        {
            reviewManager = ReviewManager.EnsureExists();
        }
    }

    private void Start()
    {
        if (buildUiOnStart)
        {
            BuildUiIfNeeded();
        }

        Subscribe();
        if (reviewManager != null)
        {
            HandleSummaryUpdated(reviewManager.CurrentSummary);
            _ = reviewManager.RefreshReviewsAsync();
            _ = reviewManager.TrySyncPendingReviewAsync();
        }
    }

    private void OnDestroy()
    {
        if (reviewManager == null)
        {
            return;
        }

        reviewManager.SummaryUpdated -= HandleSummaryUpdated;
        reviewManager.StatusUpdated -= HandleStatusUpdated;
        reviewManager.ReviewPromptRequested -= ShowReviewPanel;
    }

    public void BuildUiIfNeeded()
    {
        if (uiBuilt)
        {
            if (summaryPanel != null)
            {
                summaryPanel.gameObject.SetActive(showHomeSummary);
            }

            return;
        }

        if (root == null)
        {
            root = transform as RectTransform;
        }

        if (root == null)
        {
            return;
        }

        Stretch(root);
        root.SetAsLastSibling();
        BuildSummaryPanel();
        BuildReviewPanel();
        uiBuilt = true;
        Subscribe();

        if (reviewManager != null)
        {
            HandleSummaryUpdated(reviewManager.CurrentSummary);
        }
    }

    public void ShowReviewPanel()
    {
        BuildUiIfNeeded();
        if (reviewPanel == null)
        {
            return;
        }

        starRatingController.SetRating(5);
        if (commentInput != null)
        {
            commentInput.text = string.Empty;
        }

        reviewPanel.SetActive(true);
        reviewPanel.transform.SetAsLastSibling();
    }

    public void HideReviewPanel()
    {
        if (reviewPanel != null)
        {
            reviewPanel.SetActive(false);
        }
    }

    private void Subscribe()
    {
        if (reviewManager == null)
        {
            reviewManager = ReviewManager.EnsureExists();
        }

        reviewManager.SummaryUpdated -= HandleSummaryUpdated;
        reviewManager.StatusUpdated -= HandleStatusUpdated;
        reviewManager.ReviewPromptRequested -= ShowReviewPanel;

        reviewManager.SummaryUpdated += HandleSummaryUpdated;
        reviewManager.StatusUpdated += HandleStatusUpdated;
        reviewManager.ReviewPromptRequested += ShowReviewPanel;
    }

    private void BuildSummaryPanel()
    {
        summaryPanel = CreatePanel("ReviewSummaryPanel", root, new Vector2(920f, 238f), new Vector2(0.5f, 0f), new Vector2(0f, 650f), PanelColor);
        summaryPanel.gameObject.SetActive(showHomeSummary);

        Text title = CreateText("Title", summaryPanel, "Visitor Reviews", 26, PrimaryText, FontStyle.Bold);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -18f), new Vector2(-64f, 34f), new Vector2(0f, 1f));

        averageText = CreateText("AverageText", summaryPanel, "0.0 / 5", 38, new Color32(0xFF, 0xC9, 0x33, 0xFF), FontStyle.Bold);
        SetRect(averageText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -60f), new Vector2(170f, 48f), new Vector2(0f, 1f));

        totalText = CreateText("TotalText", summaryPanel, "0 reviews", 18, SecondaryText, FontStyle.Bold);
        SetRect(totalText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(216f, -72f), new Vector2(230f, 32f), new Vector2(0f, 1f));

        Button writeButton = CreateButton("WriteReviewButton", summaryPanel, "Rate & Review", new Vector2(226f, 58f), new Vector2(1f, 1f), new Vector2(-148f, -52f), Accent);
        writeButton.onClick.AddListener(ShowReviewPanel);

        statusText = CreateText("StatusText", summaryPanel, "Reviews loading...", 16, MutedText, FontStyle.Normal);
        SetRect(statusText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -118f), new Vector2(-56f, 28f), new Vector2(0f, 1f));

        RectTransform listViewport = CreatePanel("LatestReviewsViewport", summaryPanel, new Vector2(866f, 78f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Color(1f, 1f, 1f, 0.045f));
        listViewport.gameObject.AddComponent<RectMask2D>();

        ScrollRect scrollRect = listViewport.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(listViewport, false);
        latestContent = contentObject.GetComponent<RectTransform>();
        latestContent.anchorMin = new Vector2(0f, 1f);
        latestContent.anchorMax = new Vector2(1f, 1f);
        latestContent.pivot = new Vector2(0.5f, 1f);
        latestContent.anchoredPosition = Vector2.zero;
        latestContent.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 7, 7);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = latestContent;
        scrollRect.viewport = listViewport;
    }

    private void BuildReviewPanel()
    {
        RectTransform overlay = CreatePanel("ReviewPanelOverlay", root, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Color(0.01f, 0.015f, 0.025f, 0.86f));
        Stretch(overlay);
        reviewPanel = overlay.gameObject;

        RectTransform card = CreatePanel("ReviewCard", overlay, new Vector2(850f, 760f), new Vector2(0.5f, 0.5f), Vector2.zero, new Color32(0x14, 0x1A, 0x24, 0xFF));

        Text title = CreateText("Title", card, "Rate Your Tour", 38, PrimaryText, FontStyle.Bold);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -34f), new Vector2(-72f, 54f), new Vector2(0f, 1f));

        Text body = CreateText("Body", card, "Your feedback helps improve the Digital Heritage Archive tour.", 22, SecondaryText, FontStyle.Normal);
        SetRect(body.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -98f), new Vector2(-72f, 64f), new Vector2(0f, 1f));

        RectTransform starsRoot = new GameObject("Stars", typeof(RectTransform), typeof(StarRatingController)).GetComponent<RectTransform>();
        starsRoot.SetParent(card, false);
        SetRect(starsRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(550f, 92f), new Vector2(0.5f, 0.5f));

        HorizontalLayoutGroup starsLayout = starsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        starsLayout.spacing = 14f;
        starsLayout.childAlignment = TextAnchor.MiddleCenter;
        starsLayout.childControlWidth = false;
        starsLayout.childControlHeight = false;

        Button[] starButtons = new Button[5];
        Text[] starLabels = new Text[5];
        for (int i = 0; i < 5; i++)
        {
            Button starButton = CreateButton("Star" + (i + 1), starsRoot, string.Empty, new Vector2(86f, 86f), new Vector2(0.5f, 0.5f), Vector2.zero, new Color(1f, 1f, 1f, 0.055f));
            Text starLabel = starButton.GetComponentInChildren<Text>();
            starLabel.text = "\u2605";
            starLabel.fontSize = 56;
            starLabel.alignment = TextAnchor.MiddleCenter;
            starButtons[i] = starButton;
            starLabels[i] = starLabel;
        }

        starRatingController = starsRoot.GetComponent<StarRatingController>();
        starRatingController.Initialize(starButtons, starLabels);
        starRatingController.SetRating(5);

        commentInput = CreateInputField(card);

        submitButton = CreateButton("SubmitButton", card, "Submit", new Vector2(250f, 68f), new Vector2(0.35f, 0f), new Vector2(0f, 78f), Accent);
        cancelButton = CreateButton("CancelButton", card, "Cancel", new Vector2(250f, 68f), new Vector2(0.65f, 0f), new Vector2(0f, 78f), new Color(0.1f, 0.12f, 0.16f, 1f));
        submitButton.onClick.AddListener(HandleSubmitClicked);
        cancelButton.onClick.AddListener(HideReviewPanel);

        reviewPanel.SetActive(false);
    }

    private async void HandleSubmitClicked()
    {
        if (reviewManager == null || starRatingController == null)
        {
            return;
        }

        if (submitButton != null)
        {
            submitButton.interactable = false;
        }

        string comment = commentInput != null ? commentInput.text : string.Empty;
        await reviewManager.SubmitReviewAsync(starRatingController.Rating, comment);

        if (submitButton != null)
        {
            submitButton.interactable = true;
        }

        HideReviewPanel();
    }

    private void HandleSummaryUpdated(ReviewSummary summary)
    {
        BuildUiIfNeeded();
        if (summary == null)
        {
            return;
        }

        if (averageText != null)
        {
            averageText.text = summary.totalReviews == 0 ? "0.0 / 5" : summary.averageRating.ToString("0.0") + " / 5";
        }

        if (totalText != null)
        {
            totalText.text = summary.totalReviews == 1 ? "1 review" : summary.totalReviews + " reviews";
        }

        if (statusText != null)
        {
            statusText.text = summary.statusMessage;
        }

        RenderLatestReviews(summary.latestReviews);
    }

    private void HandleStatusUpdated(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void RenderLatestReviews(IReadOnlyList<ReviewData> reviews)
    {
        if (latestContent == null)
        {
            return;
        }

        for (int i = latestContent.childCount - 1; i >= 0; i--)
        {
            Destroy(latestContent.GetChild(i).gameObject);
        }

        if (reviews == null || reviews.Count == 0)
        {
            Text empty = CreateText("EmptyReviews", latestContent, "No reviews yet. Tap Rate & Review.", 17, MutedText, FontStyle.Italic);
            empty.alignment = TextAnchor.MiddleLeft;
            LayoutElement emptyElement = empty.gameObject.AddComponent<LayoutElement>();
            emptyElement.preferredHeight = 30f;
            return;
        }

        int count = Mathf.Min(10, reviews.Count);
        for (int i = 0; i < count; i++)
        {
            ReviewData review = reviews[i];
            string comment = string.IsNullOrWhiteSpace(review.comment) ? "No comment." : review.comment;
            if (comment.Length > 72)
            {
                comment = comment.Substring(0, 69).TrimEnd() + "...";
            }

            string rowText = Mathf.Clamp(review.rating, 1, 5) + "/5 - " + comment + "\n" + review.DisplayName + " - " + review.DisplayDate;
            Text row = CreateText("Review_" + i, latestContent, rowText, 16, SecondaryText, FontStyle.Normal);
            row.alignment = TextAnchor.UpperLeft;

            LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 54f;
        }
    }

    private InputField CreateInputField(RectTransform parent)
    {
        RectTransform inputRoot = CreatePanel("CommentInput", parent, new Vector2(760f, 220f), new Vector2(0.5f, 1f), new Vector2(0f, -380f), new Color(1f, 1f, 1f, 0.075f));

        InputField input = inputRoot.gameObject.AddComponent<InputField>();
        input.lineType = InputField.LineType.MultiLineNewline;
        input.characterLimit = 500;

        RectTransform placeholderRect = new GameObject("Placeholder", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
        placeholderRect.SetParent(inputRoot, false);
        Stretch(placeholderRect, new Vector2(22f, 16f), new Vector2(-22f, -16f));
        Text placeholder = placeholderRect.GetComponent<Text>();
        ConfigureText(placeholder, "Write a short comment...", 22, MutedText, FontStyle.Normal);
        placeholder.alignment = TextAnchor.UpperLeft;

        RectTransform textRect = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
        textRect.SetParent(inputRoot, false);
        Stretch(textRect, new Vector2(22f, 16f), new Vector2(-22f, -16f));
        Text text = textRect.GetComponent<Text>();
        ConfigureText(text, string.Empty, 22, PrimaryText, FontStyle.Normal);
        text.alignment = TextAnchor.UpperLeft;

        input.textComponent = text;
        input.placeholder = placeholder;
        input.caretColor = PrimaryText;
        input.selectionColor = new Color(0.23f, 0.51f, 0.96f, 0.35f);
        return input;
    }

    private RectTransform CreatePanel(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 position, Color color)
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);
        RectTransform rect = panelObject.GetComponent<RectTransform>();
        SetRect(rect, anchor, anchor, position, size, new Vector2(0.5f, 0.5f));

        Image image = panelObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        if (!name.Contains("Overlay"))
        {
            image.sprite = GetRoundedPanelSprite();
            image.type = Image.Type.Sliced;
        }

        Outline outline = panelObject.AddComponent<Outline>();
        outline.effectColor = StrokeColor;
        outline.effectDistance = new Vector2(1f, -1f);
        return rect;
    }

    private static Sprite GetRoundedPanelSprite()
    {
        if (roundedPanelSprite != null)
        {
            return roundedPanelSprite;
        }

        const int size = 64;
        const int radius = 18;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "ReviewRoundedPanelSpriteTexture";
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color solid = Color.white;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int nearestX = Mathf.Clamp(x, radius, size - radius - 1);
                int nearestY = Mathf.Clamp(y, radius, size - radius - 1);
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
                texture.SetPixel(x, y, distance <= radius ? solid : clear);
            }
        }

        texture.Apply();
        roundedPanelSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(22f, 22f, 22f, 22f));
        roundedPanelSprite.name = "ReviewRoundedPanelSprite";
        roundedPanelSprite.hideFlags = HideFlags.HideAndDontSave;
        return roundedPanelSprite;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 size, Vector2 anchor, Vector2 position, Color color)
    {
        RectTransform buttonRect = CreatePanel(name, parent, size, anchor, position, color);
        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonRect.GetComponent<Image>();

        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.38f);
        button.colors = colors;

        Text text = CreateText("Label", buttonRect, label, 22, PrimaryText, FontStyle.Bold);
        Stretch(text.rectTransform);
        text.alignment = TextAnchor.MiddleCenter;
        return button;
    }

    private Text CreateText(string name, Transform parent, string value, int size, Color color, FontStyle style)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        ConfigureText(text, value, size, color, style);
        return text;
    }

    private static void ConfigureText(Text text, string value, int size, Color color, FontStyle style)
    {
        text.text = value;
        text.font = GetRuntimeFont();
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = false;
    }

    private static Font GetRuntimeFont()
    {
        if (runtimeFont != null)
        {
            return runtimeFont;
        }

        runtimeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (runtimeFont == null)
        {
            runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Roboto", "Noto Sans" }, 18);
        }

        return runtimeFont;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        Stretch(rect, Vector2.zero, Vector2.zero);
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
