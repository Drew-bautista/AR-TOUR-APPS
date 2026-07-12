using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

// Home screen runtime builder for Aguinaldo Shrine AR Tour
// Creates a clean, dark, glassmorphism-styled HUD and wires basic animations.
// Configure sprites & fonts in the inspector after adding to a GameObject.

public class HomeScreenUIController : MonoBehaviour
{
    [Header("Asset references (assign in inspector)")]
    public Canvas parentCanvas; // optional: assign existing canvas
    public Sprite backgroundGradientSprite;
    public Sprite shrineBlurSprite; // will be used as RawImage.texture
    public Sprite glowSprite; // radial glow
    public Sprite iconArrow;
    public Sprite iconCamera;
    public Sprite iconMap;
    public TMP_FontAsset tmpFont;
    public string arSceneName = ""; // set to your AR scene name to auto-load

    [Header("Colors")]
    public Color primaryColor = new Color32(0x0A, 0x1F, 0x44, 0xFF); // #0A1F44
    public Color accentColor = new Color32(0x3B, 0x82, 0xF6, 0xFF);  // #3B82F6
    public Color mutedTextColor = new Color(0.75f, 0.80f, 0.93f, 1f);  // ~#BFCFED

    // Internal refs
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        CreateOrFindCanvas();
    }

    void Start()
    {
        BuildLayout();
        StartCoroutine(EntranceSequence());
    }

    void CreateOrFindCanvas()
    {
        if (parentCanvas != null)
        {
            canvas = parentCanvas;
        }
        else
        {
            Canvas existing = FindObjectOfType<Canvas>();
            if (existing != null)
                canvas = existing;
            else
            {
                GameObject cg = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = cg.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var cs = cg.GetComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1080, 2340);
                cs.matchWidthOrHeight = 0.5f;
            }
        }

        canvasGroup = canvas.gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
    }

    GameObject NewUI(string name, Transform parent, out RectTransform rt)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        rt = go.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        return go;
    }

    TextMeshProUGUI NewTMP(string name, Transform parent, string text, int size, Color color, bool uppercase = false)
    {
        RectTransform rt;
        GameObject go = NewUI(name, parent, out rt);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = uppercase ? text.ToUpperInvariant() : text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        if (tmpFont != null) tmp.font = tmpFont;
        rt.sizeDelta = new Vector2(900, size * 2);
        return tmp;
    }

    void BuildLayout()
    {
        // Background root (stretch)
        RectTransform bgRT;
        GameObject bgRoot = NewUI("BackgroundRoot", canvas.transform, out bgRT);
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        Image bgImg = bgRoot.AddComponent<Image>();
        if (backgroundGradientSprite != null)
        {
            bgImg.sprite = backgroundGradientSprite;
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;
        }
        else
        {
            bgImg.color = primaryColor;
        }
        bgImg.raycastTarget = false;

        // Shrine blurred overlay (optional)
        if (shrineBlurSprite != null)
        {
            RectTransform sRT;
            GameObject sGO = NewUI("ShrineOverlayBlur", bgRoot.transform, out sRT);
            sRT.anchorMin = Vector2.zero; sRT.anchorMax = Vector2.one; sRT.offsetMin = Vector2.zero; sRT.offsetMax = Vector2.zero;
            var raw = sGO.AddComponent<RawImage>();
            raw.texture = shrineBlurSprite.texture;
            var c = Color.white; c.a = 0.12f; raw.color = c;
            raw.raycastTarget = false;
        }

        // Header
        RectTransform headerRT;
        GameObject headerGO = NewUI("Header", canvas.transform, out headerRT);
        headerRT.anchorMin = new Vector2(0.5f, 1f); headerRT.anchorMax = new Vector2(0.5f, 1f); headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.sizeDelta = new Vector2(900, 220); headerRT.anchoredPosition = new Vector2(0, -80);
        var headerCG = headerGO.AddComponent<CanvasGroup>();

        var label = NewTMP("Label_SMALL", headerGO.transform, "AR HERITAGE EXPERIENCE", 14, mutedTextColor, true);
        var labelRT = label.GetComponent<RectTransform>(); labelRT.anchoredPosition = new Vector2(0, 60);

        var title = NewTMP("Title_MAIN", headerGO.transform, "Digital Heritage Archive tour", 36, Color.white);
        var titleRT = title.GetComponent<RectTransform>(); titleRT.anchoredPosition = new Vector2(0, 12);
        title.fontStyle = FontStyles.Bold;

        var subtitle = NewTMP("Subtitle", headerGO.transform, "Explore history through immersive augmented reality navigation", 16, mutedTextColor);
        var subRT = subtitle.GetComponent<RectTransform>(); subRT.anchoredPosition = new Vector2(0, -28);

        // Feature cards
        RectTransform cardsRT;
        GameObject cardsGO = NewUI("FeatureCards", canvas.transform, out cardsRT);
        cardsRT.anchorMin = new Vector2(0.5f, 0.5f); cardsRT.anchorMax = new Vector2(0.5f, 0.5f); cardsRT.pivot = new Vector2(0.5f, 0.5f);
        cardsRT.anchoredPosition = new Vector2(0, 140); cardsRT.sizeDelta = new Vector2(960, 360);
        var cardsCG = cardsGO.AddComponent<CanvasGroup>();
        var hlg = cardsGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 18; hlg.childControlHeight = true; hlg.childControlWidth = true; hlg.childForceExpandHeight = false; hlg.childForceExpandWidth = true;

        CreateCard(cardsGO.transform, "Live AR Navigation", "Follow real-time arrows to explore the shrine", iconArrow);
        CreateCard(cardsGO.transform, "Smart Object Scan", "Automatically detect artifacts and view details", iconCamera);
        CreateCard(cardsGO.transform, "Mini Map Guide", "Track your route with a live indoor map", iconMap);

        // Main buttons (bottom center)
        RectTransform btnsRT;
        GameObject btnsGO = NewUI("MainButtons", canvas.transform, out btnsRT);
        btnsRT.anchorMin = new Vector2(0.5f, 0f); btnsRT.anchorMax = new Vector2(0.5f, 0f); btnsRT.pivot = new Vector2(0.5f, 0f);
        btnsRT.sizeDelta = new Vector2(1000, 240); btnsRT.anchoredPosition = new Vector2(0, 60);
        var btnsCG = btnsGO.AddComponent<CanvasGroup>();

        // Start button
        RectTransform startRT;
        GameObject startBtnGO = new GameObject("Btn_StartTour", typeof(RectTransform), typeof(Image), typeof(Button));
        startBtnGO.transform.SetParent(btnsGO.transform, false);
        startRT = startBtnGO.GetComponent<RectTransform>(); startRT.sizeDelta = new Vector2(820, 100);
        Image startImg = startBtnGO.GetComponent<Image>(); startImg.color = Color.white; startImg.raycastTarget = true;
        Button startBtn = startBtnGO.GetComponent<Button>();

        // Glow backdrop
        if (glowSprite != null)
        {
            RectTransform glowRT; GameObject glowGO = NewUI("Glow_Backdrop", startBtnGO.transform, out glowRT);
            glowRT.anchorMin = new Vector2(0.5f, 0.5f); glowRT.anchorMax = new Vector2(0.5f, 0.5f); glowRT.pivot = new Vector2(0.5f, 0.5f);
            glowRT.anchoredPosition = Vector2.zero; glowRT.sizeDelta = new Vector2(920, 160);
            var glowImg = glowGO.AddComponent<Image>(); glowImg.sprite = glowSprite; glowImg.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.18f);
            glowImg.raycastTarget = false; glowGO.transform.SetSiblingIndex(0);
            var pulse = glowGO.AddComponent<StartButtonPulse>(); pulse.glowRect = glowRT;
        }

        var startText = NewTMP("StartText", startBtnGO.transform, "Start Tour", 20, Color.white);
        var startTextRT = startText.GetComponent<RectTransform>(); startTextRT.anchoredPosition = Vector2.zero;
        startBtn.onClick.AddListener(() => OnStartTourClicked());

        // Exit button
        RectTransform exitRT;
        GameObject exitBtnGO = new GameObject("Btn_Exit", typeof(RectTransform), typeof(Image), typeof(Button));
        exitBtnGO.transform.SetParent(btnsGO.transform, false);
        exitRT = exitBtnGO.GetComponent<RectTransform>(); exitRT.sizeDelta = new Vector2(220, 56); exitRT.anchoredPosition = new Vector2(0, -70);
        Image exitImg = exitBtnGO.GetComponent<Image>(); exitImg.color = new Color(1, 1, 1, 0.04f);
        Button exitBtn = exitBtnGO.GetComponent<Button>();
        var exitText = NewTMP("ExitText", exitBtnGO.transform, "Exit", 16, Color.white);
        var exitTextRT = exitText.GetComponent<RectTransform>(); exitTextRT.anchoredPosition = Vector2.zero;
        exitBtn.onClick.AddListener(() => OnExitClicked());

        // Footer note
        var footer = NewTMP("FooterNote", canvas.transform, "Start at the Main Hall for the best AR experience", 12, new Color(1, 1, 1, 0.65f));
        var footerRT = footer.GetComponent<RectTransform>(); footerRT.anchorMin = new Vector2(0.5f, 0f); footerRT.anchorMax = new Vector2(0.5f, 0f); footerRT.pivot = new Vector2(0.5f, 0f); footerRT.anchoredPosition = new Vector2(0, 18);
    }

    void CreateCard(Transform parent, string title, string desc, Sprite icon)
    {
        RectTransform rt;
        GameObject card = new GameObject("Card_" + title, typeof(RectTransform), typeof(Image), typeof(Button));
        card.transform.SetParent(parent, false);
        rt = card.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(300, 300);
        var img = card.GetComponent<Image>(); img.color = new Color(1, 1, 1, 0.06f);

        if (icon != null)
        {
            RectTransform iRT; GameObject iconGO = NewUI("Icon", card.transform, out iRT);
            iRT.anchorMin = new Vector2(0.5f, 1f); iRT.anchorMax = new Vector2(0.5f, 1f); iRT.pivot = new Vector2(0.5f, 1f);
            iRT.anchoredPosition = new Vector2(0, -18); iRT.sizeDelta = new Vector2(64, 64);
            var iImg = iconGO.AddComponent<Image>(); iImg.sprite = icon; iImg.color = Color.white; iImg.raycastTarget = false;
        }

        var t = NewTMP("Title", card.transform, title, 18, Color.white);
        var tRT = t.GetComponent<RectTransform>(); tRT.anchoredPosition = new Vector2(0, -80);
        var d = NewTMP("Desc", card.transform, desc, 14, new Color(1, 1, 1, 0.85f));
        var dRT = d.GetComponent<RectTransform>(); dRT.anchoredPosition = new Vector2(0, -120);

        card.AddComponent<CardButton>();
    }

    IEnumerator EntranceSequence()
    {
        // Simple fade-in for canvas
        canvasGroup.alpha = 0f;
        float dur = 0.6f; float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t / dur);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    public void OnStartTourClicked()
    {
        StartCoroutine(DoStartTransition());
    }

    IEnumerator DoStartTransition()
    {
        float dur = 0.4f; float t = 0f; float start = canvasGroup.alpha;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 0f, t / dur);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        if (!string.IsNullOrEmpty(arSceneName))
        {
            SceneManager.LoadScene(arSceneName);
        }
        else
        {
            Debug.Log("Start Tour clicked — AR scene name not set. Assign 'arSceneName' on HomeScreenUIController.");
        }
    }

    public void OnExitClicked()
    {
        Debug.Log("Exit clicked — Application.Quit() called.");
        Application.Quit();
    }

    // Build UI in editor or at runtime on demand
    [ContextMenu("Build UI Now")]
    public void BuildNow()
    {
        // Remove previous auto-generated children to avoid duplicates
        string[] names = new string[] { "BackgroundRoot", "ShrineOverlayBlur", "Header", "FeatureCards", "MainButtons", "FooterNote" };
        for (int i = canvas.transform.childCount - 1; i >= 0; --i)
        {
            var child = canvas.transform.GetChild(i);
            if (System.Array.Exists(names, n => child.name.StartsWith(n)))
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
                else
#endif
                    Destroy(child.gameObject);
            }
        }

        BuildLayout();
        if (Application.isPlaying)
        {
            StartCoroutine(EntranceSequence());
        }
    }
}
