using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// =====================================================================================
//  Aguinaldo Shrine AR Tour — Premium Home UI (commercial-grade)
//  ----------------------------------------------------------------
//  Builds a high-end, dark-mode AR app home screen at runtime.
//  Layout (mobile-first 1080 x 2340 reference):
//      ┌──────────────────────────┐
//      │  status pill (top safe)  │
//      │                          │
//      │  AR HERITAGE EXPERIENCE  │   <- eyebrow label
//      │  Aguinaldo Shrine        │   <- main title (2 lines)
//      │  AR Tour                 │
//      │  ─── accent gradient ─── │
//      │  Subtitle copy …         │
//      │                          │
//      │  ╭──────────────────╮    │
//      │  │ 🧭 Live AR Nav   │    │   <- glassmorphism card 1
//      │  ╰──────────────────╯    │
//      │  ╭──────────────────╮    │
//      │  │ 📷 Smart Scan    │    │   <- glassmorphism card 2
//      │  ╰──────────────────╯    │
//      │  ╭──────────────────╮    │
//      │  │ 🗺  Mini Map      │    │   <- glassmorphism card 3
//      │  ╰──────────────────╯    │
//      │                          │
//      │  ╔════ Start Tour ════╗  │   <- gradient CTA + glow + pulse
//      │       Exit              │   <- ghost secondary
//      │  Begin tour at Main Hall │   <- footer
//      └──────────────────────────┘
//
//  All visuals are procedurally built and animated via the helper scripts:
//      UICascadeReveal, AmbientGlowDrift, GradientCTAPulse, CardButton (existing)
//
//  Designed to coexist with the legacy HomeScreenUIController — choose ONE per scene.
// =====================================================================================

[DisallowMultipleComponent]
public class PremiumHomeUIController : MonoBehaviour
{
    public const string OpenGalleryOnLoadPrefKey = "AguinaldoShrine.OpenGalleryOnLoad";

    // ---------------- Sprite & font slots (assign in inspector OR via the editor tool) ----------------
    [Header("Background")]
    public Sprite gradientBackground;     // vertical navy -> black
    public Sprite vignetteSprite;         // radial dark vignette
    public Sprite ambientBloomSprite;     // soft accent bloom
    public Sprite shrineSilhouette;       // optional blurred shrine

    [Header("UI Sprites")]
    public Sprite roundedCardSprite;      // 9-sliced rounded rect (white)
    public Sprite roundedPillSprite;      // 9-sliced rounded pill for CTA
    public Sprite roundedBoxSprite;       // 9-sliced low-radius rect for compact controls
    public Sprite ctaGradientSprite;      // horizontal blue gradient
    public Sprite accentLineSprite;       // 1px gradient line
    public Sprite glowRadialSprite;       // additive blue glow

    [Header("Icons")]
    public Sprite iconNavigation;
    public Sprite iconScan;
    public Sprite iconMap;
    public Sprite iconChevron;

    [Header("Typography")]
    public Font titleFont;                // bold display
    public Font bodyFont;                 // regular UI font

    [Header("Behavior")]
    public string arSceneName = "AguinaldoShrineARTour";
    public bool buildOnStart = true;

    // ---------------- Color system ----------------
    static readonly Color C_PrimaryNavy   = new Color32 (0x0A, 0x1F, 0x44, 0xFF);
    static readonly Color C_DeepBlack     = new Color32(0x03, 0x07, 0x12, 0xFF);
    static readonly Color C_Accent        = new Color32(0x3B, 0x82, 0xF6, 0xFF);
    static readonly Color C_AccentLight   = new Color32(0x60, 0xA5, 0xFA, 0xFF);
    static readonly Color C_TextPrimary   = Color.white;
    static readonly Color C_TextSecondary = new Color32(0xC4, 0xD2, 0xEC, 0xFF);
    static readonly Color C_TextMuted     = new Color32(0x8A, 0x9C, 0xBE, 0xFF);
    static readonly Color C_GlassFill     = new Color(1f, 1f, 1f, 0.055f);
    static readonly Color C_GlassStroke   = new Color(1f, 1f, 1f, 0.12f);

    // ---------------- Internal references ----------------
    const string LanguagePrefKey = AppLanguage.PreferenceKey;

    enum HomeLanguage
    {
        English,
        Filipino
    }

    sealed class LocalizedTextBinding
    {
        public Text text;
        public string key;
        public bool upper;
    }

    sealed class LanguageOptionVisual
    {
        public HomeLanguage language;
        public Image background;
        public Image stroke;
        public Text label;
    }

    Canvas canvas;
    CanvasGroup rootCG;
    RectTransform canvasRT;
    GameObject aboutOverlay;
    HomeLanguage currentLanguage = HomeLanguage.English;
    readonly List<CanvasGroup> revealGroups = new List<CanvasGroup>();
    readonly List<LocalizedTextBinding> localizedTextBindings = new List<LocalizedTextBinding>();
    readonly List<LanguageOptionVisual> languageOptionVisuals = new List<LanguageOptionVisual>();

    // =========================================================================
    //  Lifecycle
    // =========================================================================
    void Awake() { EnsureCanvas(); }

    void Start()
    {
        if (!buildOnStart) return;
        BuildAll();
    }

    [ContextMenu("Rebuild Premium Home UI")]
    public void BuildAll()
    {
        EnsureCanvas();
        LoadLanguagePreference();
        ClearGenerated();
        revealGroups.Clear();
        localizedTextBindings.Clear();
        languageOptionVisuals.Clear();

        BuildBackground();
        BuildTopStatusPill();
        var headerCG = BuildHeader();
        var languageCG = BuildLanguageSelector();
        var cardsCG = BuildFeatureCards();
        var ctaCG = BuildPrimaryCTA();
        var exitCG = BuildExitButton();
        var footerCG = BuildFooter();
        BuildAboutOverlay();
        ReviewUIController.AttachToCanvas(canvas, true);

        // Cascade reveal — staggered slide+fade-in
        var cascade = canvas.gameObject.GetComponent<UICascadeReveal>();
        if (cascade == null) cascade = canvas.gameObject.AddComponent<UICascadeReveal>();
        cascade.targets = new List<CanvasGroup> { headerCG, languageCG, cardsCG, ctaCG, exitCG, footerCG };

        if (Application.isPlaying)
        {
            cascade.Restart();
        }
        else
        {
            for (int i = 0; i < cascade.targets.Count; i++)
            {
                CanvasGroup group = cascade.targets[i];
                if (group == null) continue;
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
        }
    }

    // =========================================================================
    //  Canvas setup
    // =========================================================================
    void EnsureCanvas()
    {
        canvas = GetComponentInChildren<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var cg = new GameObject("PremiumHomeCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cg.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = cg.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2340);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
        canvasRT = canvas.GetComponent<RectTransform>();
        rootCG = canvas.gameObject.GetComponent<CanvasGroup>();
        if (rootCG == null) rootCG = canvas.gameObject.AddComponent<CanvasGroup>();
    }

    void ClearGenerated()
    {
        // strip our previously generated layer (tagged via name prefix "PHUI_")
        for (int i = canvas.transform.childCount - 1; i >= 0; --i)
        {
            var ch = canvas.transform.GetChild(i);
            if (ch.name.StartsWith("PHUI_"))
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(ch.gameObject);
                else Destroy(ch.gameObject);
#else
                Destroy(ch.gameObject);
#endif
            }
        }
    }

    // =========================================================================
    //  Section 1 — Background (gradient + vignette + ambient bloom + silhouette)
    // =========================================================================
    void BuildBackground()
    {
        var bg = NewRT("PHUI_Background", canvas.transform);
        Stretch(bg);

        // Solid base (in case sprite missing)
        AddImage(bg, null, C_DeepBlack, false);

        // Gradient
        var grad = NewRT("Gradient", bg);
        Stretch(grad);
        AddImage(grad, gradientBackground, Color.white, false);

        // Optional shrine silhouette (very faint, top half)
        if (shrineSilhouette != null)
        {
            var sil = NewRT("ShrineSilhouette", bg);
            Stretch(sil);
            var img = AddImage(sil, shrineSilhouette, new Color(1f, 1f, 1f, 0.10f), false);
            img.preserveAspect = true;
        }

        // Ambient drifting bloom (top-left)
        var bloomA = NewRT("BloomA", bg);
        Anchor(bloomA, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        bloomA.sizeDelta = new Vector2(1400, 1400);
        bloomA.anchoredPosition = new Vector2(-200, 200);
        var bloomAImg = AddImage(bloomA, glowRadialSprite, new Color(C_Accent.r, C_Accent.g, C_Accent.b, 0.22f), false);
        bloomAImg.raycastTarget = false;
        var driftA = bloomA.gameObject.AddComponent<AmbientGlowDrift>();
        driftA.amplitude = new Vector2(60, 80);
        driftA.period = 11f;
        driftA.scaleRange = new Vector2(0.95f, 1.10f);

        // Ambient drifting bloom (bottom-right, warmer / lighter accent)
        var bloomB = NewRT("BloomB", bg);
        Anchor(bloomB, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
        bloomB.sizeDelta = new Vector2(1500, 1500);
        bloomB.anchoredPosition = new Vector2(200, -100);
        var bloomBImg = AddImage(bloomB, glowRadialSprite, new Color(C_AccentLight.r, C_AccentLight.g, C_AccentLight.b, 0.18f), false);
        bloomBImg.raycastTarget = false;
        var driftB = bloomB.gameObject.AddComponent<AmbientGlowDrift>();
        driftB.amplitude = new Vector2(80, 60);
        driftB.period = 14f;
        driftB.phase = Mathf.PI;
        driftB.scaleRange = new Vector2(0.92f, 1.08f);

        // Vignette overlay (always on top of bloom for depth)
        var vig = NewRT("Vignette", bg);
        Stretch(vig);
        var vigImg = AddImage(vig, vignetteSprite, new Color(0f, 0f, 0f, 0.85f), false);
        vigImg.raycastTarget = false;
    }

    // =========================================================================
    //  Section 2 — Top status pill (small premium app touch)
    // =========================================================================
    void BuildTopStatusPill()
    {
        var pill = NewRT("PHUI_StatusPill", canvas.transform);
        Anchor(pill, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        pill.sizeDelta = new Vector2(430, 58);
        pill.anchoredPosition = new Vector2(0f, -90f);

        var bg = AddImage(pill, BoxSprite, new Color(1f, 1f, 1f, 0.065f), true);
        bg.type = Image.Type.Sliced;

        var stroke = NewRT("Stroke", pill);
        Stretch(stroke, -1.5f);
        var strokeImg = AddImage(stroke, BoxSprite, new Color(1f, 1f, 1f, 0.12f), false);
        strokeImg.type = Image.Type.Sliced;
        stroke.SetAsFirstSibling();

        // Live dot
        var dot = NewRT("LiveDot", pill);
        Anchor(dot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        dot.sizeDelta = new Vector2(14, 14);
        dot.anchoredPosition = new Vector2(28, 0);
        AddImage(dot, glowRadialSprite, C_Accent, false);

        // Text
        var t = NewLocalizedText("Status", pill, "status", 18, C_TextSecondary, true);
        var trt = t.rectTransform;
        Anchor(trt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        trt.sizeDelta = new Vector2(360, 30);
        trt.anchoredPosition = new Vector2(14, 0);
        t.fontStyle = FontStyle.Bold;
    }

    // =========================================================================
    //  Section 3 — Header
    // =========================================================================
    CanvasGroup BuildHeader()
    {
        var header = NewRT("PHUI_Header", canvas.transform);
        Anchor(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        header.sizeDelta = new Vector2(960, 380);
        header.anchoredPosition = new Vector2(0f, -180f);
        var cg = header.gameObject.AddComponent<CanvasGroup>();
        revealGroups.Add(cg);

        // Eyebrow label
        var eyebrow = NewLocalizedText("Eyebrow", header, "eyebrow", 22, C_Accent, true);
        var ert = eyebrow.rectTransform;
        Anchor(ert, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        ert.sizeDelta = new Vector2(900, 30);
        ert.anchoredPosition = new Vector2(0f, 0f);
        eyebrow.fontStyle = FontStyle.Bold;

        // Main title (large)
        var title = NewLocalizedText("Title", header, "title", 76, C_TextPrimary);
        title.font = titleFont != null ? titleFont : title.font;
        var trt = title.rectTransform;
        Anchor(trt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        trt.sizeDelta = new Vector2(960, 220);
        trt.anchoredPosition = new Vector2(0f, -50f);
        title.fontStyle = FontStyle.Bold;
        title.resizeTextForBestFit = true;
        title.resizeTextMinSize = 58;
        title.resizeTextMaxSize = 84;
        title.lineSpacing = 0.88f;
        title.alignment = TextAnchor.MiddleCenter;

        // Accent gradient line
        var line = NewRT("AccentLine", header);
        Anchor(line, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        line.sizeDelta = new Vector2(140, 4);
        line.anchoredPosition = new Vector2(0f, -280f);
        AddImage(line, accentLineSprite, C_Accent, false);

        // Subtitle
        var sub = NewLocalizedText("Subtitle", header, "subtitle", 26, C_TextSecondary);
        var srt = sub.rectTransform;
        Anchor(srt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        srt.sizeDelta = new Vector2(820, 90);
        srt.anchoredPosition = new Vector2(0f, -315f);
        sub.alignment = TextAnchor.MiddleCenter;
        sub.lineSpacing = 1.15f;

        return cg;
    }

    CanvasGroup BuildLanguageSelector()
    {
        var holder = NewRT("PHUI_Language", canvas.transform);
        Anchor(holder, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        holder.sizeDelta = new Vector2(620, 72);
        holder.anchoredPosition = new Vector2(0f, -610f);
        var cg = holder.gameObject.AddComponent<CanvasGroup>();
        revealGroups.Add(cg);

        var bg = AddImage(holder, BoxSprite, new Color(1f, 1f, 1f, 0.055f), true);
        bg.type = Image.Type.Sliced;

        var stroke = NewRT("Stroke", holder);
        Stretch(stroke, -1.5f);
        var strokeImg = AddImage(stroke, BoxSprite, new Color(1f, 1f, 1f, 0.12f), false);
        strokeImg.type = Image.Type.Sliced;
        stroke.SetAsFirstSibling();

        var label = NewLocalizedText("Label", holder, "language_label", 20, C_TextSecondary, true);
        label.fontStyle = FontStyle.Bold;
        var labelRect = label.rectTransform;
        Anchor(labelRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        labelRect.sizeDelta = new Vector2(180, 32);
        labelRect.anchoredPosition = new Vector2(92f, 0f);
        label.alignment = TextAnchor.MiddleLeft;

        BuildLanguageOption(holder, HomeLanguage.English, "EN", new Vector2(330f, 0f));
        BuildLanguageOption(holder, HomeLanguage.Filipino, "FIL", new Vector2(480f, 0f));
        RefreshLanguageButtons();
        return cg;
    }

    void BuildLanguageOption(RectTransform parent, HomeLanguage language, string labelText, Vector2 position)
    {
        var btnGO = new GameObject("Language_" + labelText, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);
        var brt = btnGO.GetComponent<RectTransform>();
        Anchor(brt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f));
        brt.sizeDelta = new Vector2(118, 46);
        brt.anchoredPosition = position;

        var img = btnGO.GetComponent<Image>();
        img.sprite = BoxSprite;
        img.type = Image.Type.Sliced;

        var stroke = NewRT("Stroke", brt);
        Stretch(stroke, -1f);
        var strokeImg = AddImage(stroke, BoxSprite, Color.white, false);
        strokeImg.type = Image.Type.Sliced;
        stroke.SetAsFirstSibling();

        var label = NewText("Label", brt, labelText, 20, Color.white, true);
        label.fontStyle = FontStyle.Bold;
        Stretch(label.rectTransform);
        label.alignment = TextAnchor.MiddleCenter;

        languageOptionVisuals.Add(new LanguageOptionVisual
        {
            language = language,
            background = img,
            stroke = strokeImg,
            label = label
        });

        btnGO.AddComponent<CardButton>();
        btnGO.GetComponent<Button>().onClick.AddListener(() => SetLanguage(language));
    }

    // =========================================================================
    //  Section 4 — Feature cards (vertical glassmorphism)
    // =========================================================================
    CanvasGroup BuildFeatureCards()
    {
        var cards = NewRT("PHUI_FeatureCards", canvas.transform);
        Anchor(cards, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        cards.sizeDelta = new Vector2(960, 720);
        cards.anchoredPosition = new Vector2(0f, -40f);
        var cg = cards.gameObject.AddComponent<CanvasGroup>();
        revealGroups.Add(cg);

        var vlg = cards.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 24f;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;

        BuildCard(cards, "feature_nav_title", "feature_nav_desc", iconNavigation);
        BuildCard(cards, "feature_scan_title", "feature_scan_desc", iconScan);
        BuildCard(cards, "feature_map_title", "feature_map_desc", iconMap);

        return cg;
    }

    void BuildCard(RectTransform parent, string titleKey, string descKey, Sprite icon)
    {
        var card = NewRT("Card_" + titleKey, parent);
        card.sizeDelta = new Vector2(900, 220);

        // Glass body
        var body = AddImage(card, roundedCardSprite, C_GlassFill, true);
        body.type = Image.Type.Sliced;
        body.raycastTarget = true;

        // Stroke (slightly larger, behind, gives "border")
        var stroke = NewRT("Stroke", card);
        Stretch(stroke, -2);
        var strokeImg = AddImage(stroke, roundedCardSprite, C_GlassStroke, false);
        strokeImg.type = Image.Type.Sliced;
        strokeImg.raycastTarget = false;
        stroke.SetAsFirstSibling();

        // Icon disc (left)
        var iconBg = NewRT("IconDisc", card);
        Anchor(iconBg, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        iconBg.sizeDelta = new Vector2(120, 120);
        iconBg.anchoredPosition = new Vector2(70f, 0f);
        var discImg = AddImage(iconBg, glowRadialSprite,
            new Color(C_Accent.r, C_Accent.g, C_Accent.b, 0.55f), false);
        discImg.raycastTarget = false;

        // Icon foreground
        var iconRT = NewRT("Icon", iconBg);
        Anchor(iconRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        iconRT.sizeDelta = new Vector2(56, 56);
        var iconImg = AddImage(iconRT, icon, Color.white, false);
        iconImg.preserveAspect = true;

        // Title
        var titleTMP = NewLocalizedText("Title", card, titleKey, 30, C_TextPrimary);
        titleTMP.fontStyle = FontStyle.Bold;
        var trt = titleTMP.rectTransform;
        Anchor(trt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        trt.sizeDelta = new Vector2(640, 40);
        trt.anchoredPosition = new Vector2(160f, -50f);
        titleTMP.alignment = TextAnchor.MiddleLeft;

        // Description
        var descTMP = NewLocalizedText("Desc", card, descKey, 22, C_TextSecondary);
        var drt = descTMP.rectTransform;
        Anchor(drt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        drt.sizeDelta = new Vector2(640, 80);
        drt.anchoredPosition = new Vector2(160f, -100f);
        descTMP.alignment = TextAnchor.UpperLeft;
        descTMP.lineSpacing = 1.12f;

        // Chevron (right) — affordance
        if (iconChevron != null)
        {
            var chev = NewRT("Chevron", card);
            Anchor(chev, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            chev.sizeDelta = new Vector2(24, 24);
            chev.anchoredPosition = new Vector2(-40f, 0f);
            var ch = AddImage(chev, iconChevron, C_TextMuted, false);
            ch.raycastTarget = false;
        }

        // Interaction
        card.gameObject.AddComponent<CardButton>();
    }

    // =========================================================================
    //  Section 5 — Primary CTA (Start Tour)
    // =========================================================================
    CanvasGroup BuildPrimaryCTA()
    {
        var holder = NewRT("PHUI_StartCTA", canvas.transform);
        Anchor(holder, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        holder.sizeDelta = new Vector2(900, 200);
        holder.anchoredPosition = new Vector2(0f, 280f);
        var cg = holder.gameObject.AddComponent<CanvasGroup>();
        revealGroups.Add(cg);

        // Glow backdrop (large, behind)
        var glow = NewRT("Glow", holder);
        Anchor(glow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        glow.sizeDelta = new Vector2(1100, 380);
        var glowImg = AddImage(glow, glowRadialSprite,
            new Color(C_Accent.r, C_Accent.g, C_Accent.b, 0.45f), false);
        glowImg.raycastTarget = false;

        // Button
        var btnGO = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(holder, false);
        var brt = btnGO.GetComponent<RectTransform>();
        Anchor(brt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        brt.sizeDelta = new Vector2(820, 130);

        // Rounded rectangle base, matching the compact Gallery/About/Exit controls.
        var buttonImage = btnGO.GetComponent<Image>();
        buttonImage.sprite = BoxSprite;
        buttonImage.color = C_PrimaryNavy;
        buttonImage.type = Image.Type.Sliced;

        var mask = btnGO.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // Gradient fill child, clipped by the rounded button mask.
        var grad = NewRT("Gradient", brt);
        Stretch(grad);
        var gradImg = AddImage(grad, ctaGradientSprite, Color.white, true);
        gradImg.raycastTarget = false;

        // Inner highlight (top sheen)
        var sheen = NewRT("Sheen", brt);
        Anchor(sheen, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        sheen.sizeDelta = new Vector2(800, 40);
        sheen.anchoredPosition = new Vector2(0f, -10f);
        var sheenImg = AddImage(sheen, glowRadialSprite, new Color(1f, 1f, 1f, 0.18f), false);
        sheenImg.raycastTarget = false;

        // Label
        var label = NewLocalizedText("Label", brt, "start_tour", 38, Color.white);
        label.fontStyle = FontStyle.Bold;
        var lrt = label.rectTransform;
        Anchor(lrt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        lrt.sizeDelta = new Vector2(700, 60);
        lrt.anchoredPosition = new Vector2(-20f, 0f);

        // Arrow
        if (iconChevron != null)
        {
            var ar = NewRT("Arrow", brt);
            Anchor(ar, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            ar.sizeDelta = new Vector2(28, 28);
            ar.anchoredPosition = new Vector2(170f, 0f);
            AddImage(ar, iconChevron, Color.white, false);
        }

        // Behaviors
        var pulse = btnGO.AddComponent<GradientCTAPulse>();
        pulse.glowRect = glow;
        pulse.buttonRect = brt;

        btnGO.GetComponent<Button>().onClick.AddListener(OnStartTourClicked);

        return cg;
    }

    // =========================================================================
    //  Section 6 - Secondary actions
    // =========================================================================
    CanvasGroup BuildExitButton()
    {
        var holder = NewRT("PHUI_Exit", canvas.transform);
        Anchor(holder, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        holder.sizeDelta = new Vector2(840, 76);
        holder.anchoredPosition = new Vector2(0f, 166f);
        var cg = holder.gameObject.AddComponent<CanvasGroup>();
        revealGroups.Add(cg);

        BuildSecondaryButton(holder, "GalleryButton", "gallery", new Vector2(-280f, 0f), OnGalleryClicked);
        BuildSecondaryButton(holder, "AboutButton", "about", Vector2.zero, OnAboutClicked);
        BuildSecondaryButton(holder, "ExitButton", "exit", new Vector2(280f, 0f), OnExitClicked);
        return cg;
    }

    void BuildSecondaryButton(RectTransform parent, string name, string labelKey, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);
        var brt = btnGO.GetComponent<RectTransform>();
        Anchor(brt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        brt.sizeDelta = new Vector2(240f, 62f);
        brt.anchoredPosition = position;

        var img = btnGO.GetComponent<Image>();
        img.sprite = BoxSprite;
        img.color = new Color(1f, 1f, 1f, 0.06f);
        img.type = Image.Type.Sliced;

        var stroke = NewRT("Stroke", brt);
        Stretch(stroke, -1.5f);
        var strokeImg = AddImage(stroke, BoxSprite, new Color(1f, 1f, 1f, 0.10f), false);
        strokeImg.type = Image.Type.Sliced;
        stroke.SetAsFirstSibling();

        var label = NewLocalizedText("Label", brt, labelKey, 24, C_TextMuted);
        label.fontStyle = FontStyle.Bold;
        var lrt = label.rectTransform;
        Stretch(lrt);
        label.alignment = TextAnchor.MiddleCenter;

        btnGO.AddComponent<CardButton>();
        btnGO.GetComponent<Button>().onClick.AddListener(onClick);
    }

    void BuildAboutOverlay()
    {
        var overlay = NewRT("PHUI_AboutOverlay", canvas.transform);
        Stretch(overlay);
        aboutOverlay = overlay.gameObject;
        aboutOverlay.AddComponent<PremiumPopupAnimator>();
        var dim = AddImage(overlay, null, new Color(0.02f, 0.03f, 0.05f, 0.9f), true);
        dim.raycastTarget = true;

        var card = NewRT("AboutCard", overlay);
        Anchor(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        card.sizeDelta = new Vector2(860f, 560f);
        card.anchoredPosition = Vector2.zero;
        var cardImage = AddImage(card, roundedCardSprite, new Color32(0x14, 0x1A, 0x24, 0xFF), true);
        cardImage.type = Image.Type.Sliced;

        var title = NewLocalizedText("Title", card, "about_title", 40, C_TextPrimary);
        title.fontStyle = FontStyle.Bold;
        var titleRect = title.rectTransform;
        Anchor(titleRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
        titleRect.sizeDelta = new Vector2(-64f, 60f);
        titleRect.anchoredPosition = new Vector2(32f, -34f);
        title.alignment = TextAnchor.UpperLeft;

        var body = NewLocalizedText("Body", card, "about_body", 25, C_TextSecondary);
        var bodyRect = body.rectTransform;
        Anchor(bodyRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
        bodyRect.sizeDelta = new Vector2(-64f, 250f);
        bodyRect.anchoredPosition = new Vector2(32f, -126f);
        body.alignment = TextAnchor.UpperLeft;
        body.lineSpacing = 1.12f;

        BuildSecondaryButton(card, "CloseAboutButton", "close", new Vector2(0f, -205f), OnCloseAboutClicked);
        aboutOverlay.SetActive(false);
    }

    // =========================================================================
    //  Section 7 — Footer
    // =========================================================================
    CanvasGroup BuildFooter()
    {
        var f = NewRT("PHUI_Footer", canvas.transform);
        Anchor(f, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        f.sizeDelta = new Vector2(900, 50);
        f.anchoredPosition = new Vector2(0f, 90f);
        var cg = f.gameObject.AddComponent<CanvasGroup>();
        revealGroups.Add(cg);

        var t = NewLocalizedText("FooterText", f, "footer", 20, C_TextMuted);
        Stretch(t.rectTransform);
        t.alignment = TextAnchor.MiddleCenter;
        return cg;
    }

    // =========================================================================
    //  Button handlers
    // =========================================================================
    public void OnStartTourClicked()
    {
        StartCoroutine(FadeOutAndLoad(false));
    }

    public void OnGalleryClicked()
    {
        StartCoroutine(FadeOutAndLoad(true));
    }

    public void OnAboutClicked()
    {
        if (aboutOverlay != null)
        {
            aboutOverlay.SetActive(true);
        }
    }

    public void OnCloseAboutClicked()
    {
        if (aboutOverlay != null)
        {
            aboutOverlay.SetActive(false);
        }
    }

    IEnumerator FadeOutAndLoad(bool openGalleryOnLoad)
    {
        rootCG.interactable = false;
        rootCG.blocksRaycasts = false;
        float dur = 0.45f, t = 0f, start = rootCG.alpha;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            rootCG.alpha = Mathf.Lerp(start, 0f, t / dur);
            yield return null;
        }
        rootCG.alpha = 0f;
        if (!string.IsNullOrEmpty(arSceneName) &&
            Application.CanStreamedLevelBeLoaded(arSceneName))
        {
            if (openGalleryOnLoad)
            {
                PlayerPrefs.SetInt(OpenGalleryOnLoadPrefKey, 1);
                PlayerPrefs.Save();
            }

            SceneManager.LoadScene(arSceneName);
        }
        else
        {
            Debug.Log("[PremiumHomeUI] Start Tour pressed — set 'arSceneName' to load AR scene.");
            rootCG.alpha = 1f;
            rootCG.interactable = true;
            rootCG.blocksRaycasts = true;
        }
    }

    public void OnExitClicked()
    {
        Debug.Log("[PremiumHomeUI] Exit pressed.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void LoadLanguagePreference()
    {
        string code = PlayerPrefs.GetString(LanguagePrefKey, "en");
        currentLanguage = code == "fil" ? HomeLanguage.Filipino : HomeLanguage.English;
    }

    void SetLanguage(HomeLanguage language)
    {
        currentLanguage = language;
        AppLanguage.Set(language == HomeLanguage.Filipino ? AppLanguageCode.Filipino : AppLanguageCode.English);
        RefreshLocalizedTexts();
        RefreshLanguageButtons();
    }

    void RefreshLocalizedTexts()
    {
        for (int i = 0; i < localizedTextBindings.Count; i++)
        {
            LocalizedTextBinding binding = localizedTextBindings[i];
            if (binding == null || binding.text == null) continue;
            string value = GetLocalizedString(binding.key);
            binding.text.text = binding.upper ? value.ToUpperInvariant() : value;
        }
    }

    void RefreshLanguageButtons()
    {
        for (int i = 0; i < languageOptionVisuals.Count; i++)
        {
            LanguageOptionVisual visual = languageOptionVisuals[i];
            if (visual == null) continue;
            bool active = visual.language == currentLanguage;

            if (visual.background != null)
            {
                visual.background.color = active
                    ? new Color(C_Accent.r, C_Accent.g, C_Accent.b, 0.95f)
                    : new Color(1f, 1f, 1f, 0.06f);
            }

            if (visual.stroke != null)
            {
                visual.stroke.color = active
                    ? new Color(C_AccentLight.r, C_AccentLight.g, C_AccentLight.b, 0.55f)
                    : new Color(1f, 1f, 1f, 0.10f);
            }

            if (visual.label != null)
            {
                visual.label.color = active ? Color.white : C_TextMuted;
            }
        }
    }

    string GetLocalizedString(string key)
    {
        if (currentLanguage == HomeLanguage.Filipino)
        {
            switch (key)
            {
                case "status": return "AR HANDA - SIMULAN";
                case "eyebrow": return "GABAY SA PAMANA GAMIT ANG AR";
                case "title": return "Digital Heritage Archive tour";
                case "subtitle": return "Tuklasin ang kasaysayan gamit ang immersive AR navigation";
                case "language_label": return "Wika";
                case "feature_nav_title": return "Live AR Navigation";
                case "feature_nav_desc": return "Sundan ang real-time na gabay para libutin ang shrine";
                case "feature_scan_title": return "Smart Object Scan";
                case "feature_scan_desc": return "Awtomatikong makita ang artifacts at mga detalye";
                case "feature_map_title": return "Gabay sa Mini Map";
                case "feature_map_desc": return "Subaybayan ang ruta gamit ang live indoor map";
                case "start_tour": return "Simulan ang Tour";
                case "gallery": return "Larawan";
                case "about": return "Tungkol";
                case "exit": return "Lumabas";
                case "about_title": return "Tungkol";
                case "about_body": return "Pinagsasama ng Digital Heritage Archive tour ang indoor navigation, image scanning, narration, at gallery archive sa isang mobile guide para sa mga bisita.";
                case "close": return "Isara";
                case "footer": return "Magsimula sa Main Hall para sa mas maayos na AR experience";
            }
        }

        switch (key)
        {
            case "status": return "AR READY - TAP TO BEGIN";
            case "eyebrow": return "AR HERITAGE EXPERIENCE";
            case "title": return "Digital Heritage Archive tour";
            case "subtitle": return "Explore history through immersive augmented reality navigation";
            case "language_label": return "Language";
            case "feature_nav_title": return "Live AR Navigation";
            case "feature_nav_desc": return "Follow real-time arrows to explore the shrine";
            case "feature_scan_title": return "Smart Object Scan";
            case "feature_scan_desc": return "Automatically detect artifacts and view details";
            case "feature_map_title": return "Mini Map Guide";
            case "feature_map_desc": return "Track your route with a live indoor map";
            case "start_tour": return "Start Tour";
            case "gallery": return "Gallery";
            case "about": return "About";
            case "exit": return "Exit";
            case "about_title": return "About";
            case "about_body": return "Digital Heritage Archive tour combines indoor navigation, image scanning, narration, and an archive gallery into one mobile guide for visitors.";
            case "close": return "Close";
            case "footer": return "Start at the Main Hall for the best AR experience";
            default: return key;
        }
    }

    // =========================================================================
    //  Helpers
    // =========================================================================
    static RectTransform NewRT(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.localScale = Vector3.one;
        return rt;
    }

    Sprite BoxSprite
    {
        get
        {
            if (roundedBoxSprite != null) return roundedBoxSprite;
            if (roundedCardSprite != null) return roundedCardSprite;
            return roundedPillSprite;
        }
    }

    static Image AddImage(RectTransform rt, Sprite sprite, Color color, bool raycastTarget)
    {
        var img = rt.gameObject.GetComponent<Image>();
        if (img == null) img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = raycastTarget;
        if (sprite == null) img.color = color; // solid fill
        return img;
    }

    Text NewLocalizedText(string name, Transform parent, string key, int size, Color color, bool upper = false)
    {
        Text uiText = NewText(name, parent, GetLocalizedString(key), size, color, upper);
        localizedTextBindings.Add(new LocalizedTextBinding
        {
            text = uiText,
            key = key,
            upper = upper
        });

        return uiText;
    }

    Text NewText(string name, Transform parent, string text, int size, Color color, bool upper = false)
    {
        var rt = NewRT(name, parent);
        var uiText = rt.gameObject.AddComponent<Text>();
        uiText.text = upper ? text.ToUpperInvariant() : text;
        uiText.fontSize = size;
        uiText.color = color;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.raycastTarget = false;
        uiText.font = bodyFont != null ? bodyFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        uiText.resizeTextForBestFit = false;
        uiText.supportRichText = false;
        rt.sizeDelta = new Vector2(600, size * 1.6f);
        return uiText;
    }

    static void Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
    }
}
