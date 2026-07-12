using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws the tour mini-map as a large, readable illustrated 2D venue map.
/// The controller keeps the existing NavigationManager contract intact.
/// </summary>
public class MiniMapController : MonoBehaviour
{
    [Header("Map References")]
    [SerializeField] private RectTransform mapViewport;
    [SerializeField] private RectTransform mapArea;
    [SerializeField] private RectTransform playerDot;
    [SerializeField] private RectTransform targetDot;
    [SerializeField] private RectTransform legacyPathLine;
    [SerializeField] private List<RectTransform> pathSegments = new List<RectTransform>();

    [Header("Mapping")]
    [SerializeField] private Vector2 worldMin = new Vector2(-1f, -1f);
    [SerializeField] private Vector2 worldMax = new Vector2(7f, 12f);
    [SerializeField] private float mapPadding = 30f;
    [SerializeField] private float playerLerpSpeed = 14f;
    [SerializeField] private float pathThickness = 8f;
    [SerializeField] private float minZoom = 1f;
    [SerializeField] private float maxZoom = 1.22f;

    [Header("Colors")]
    [SerializeField] private Color activeColor = new Color32(0x1E, 0x8F, 0xFF, 0xFF);
    [SerializeField] private Color routeColor = new Color32(0x13, 0x83, 0xF6, 0xFF);
    [SerializeField] private Color completedColor = new Color32(0x22, 0xB8, 0x56, 0xFF);
    [SerializeField] private Color upcomingColor = new Color32(0x8E, 0x98, 0xA5, 0xFF);
    [SerializeField] private Color panelColor = new Color32(0xFA, 0xFC, 0xFF, 0xFA);
    [SerializeField] private Color mapSurfaceColor = new Color32(0xF2, 0xF7, 0xFA, 0xFF);
    [SerializeField] private Color labelColor = new Color32(0x24, 0x2B, 0x33, 0xFF);

    private const string ControlsRootName = "MiniMapChromeControls";

    private readonly List<StopMarkerView> stopMarkers = new List<StopMarkerView>();
    private readonly List<LocationTrigger> stops = new List<LocationTrigger>();

    private Sprite circleSprite;
    private Sprite roundedSprite;
    private Font labelFont;
    private RectTransform controlsRoot;
    private Vector2 smoothedPlayerPosition;
    private Vector2 lastViewportSize;
    private int lastTargetIndex = int.MinValue;
    private int lastCompletedIndex = int.MinValue;
    private float mapZoom = 1f;
    private bool hasPlayerPosition;

    private sealed class StopMarkerView
    {
        public RectTransform Root;
        public Image Ring;
        public Image Fill;
        public Text Label;
    }

    public void ConfigureReferences(
        RectTransform viewport,
        RectTransform area,
        RectTransform currentDot,
        RectTransform destinationDot,
        RectTransform singlePathLine,
        IReadOnlyList<RectTransform> routeSegments,
        Vector2 minWorld,
        Vector2 maxWorld)
    {
        mapViewport = viewport;
        mapArea = area;
        playerDot = currentDot;
        targetDot = destinationDot;
        legacyPathLine = singlePathLine;
        worldMin = minWorld;
        worldMax = maxWorld;

        pathSegments.Clear();
        if (routeSegments == null)
        {
            return;
        }

        for (int i = 0; i < routeSegments.Count; i++)
        {
            if (routeSegments[i] != null)
            {
                pathSegments.Add(routeSegments[i]);
            }
        }
    }

    public void Initialize(IReadOnlyList<LocationTrigger> orderedStops)
    {
        if (mapArea == null)
        {
            return;
        }

        if (mapViewport == null)
        {
            mapViewport = mapArea.parent as RectTransform;
        }

        labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        circleSprite = circleSprite != null ? circleSprite : CreateCircleSprite(96);
        roundedSprite = roundedSprite != null ? roundedSprite : CreateRoundedRectSprite(96, 96, 18);

        StyleMapChrome();
        FitMapAreaToViewport(true);
        EnsureRouteSegments();
        EnsurePlayerDot();
        EnsureMapControls();
        HideLegacyTargetDot();
        SetStops(orderedStops);
        HideRoute();
    }

    public void StyleNavigationButtons(Button backButton, Button nextButton, Button exitButton)
    {
        StyleButton(backButton, new Color32(0xEA, 0xEF, 0xF5, 0xFF), new Color32(0x0B, 0x11, 0x1D, 0xFF));
        StyleButton(nextButton, activeColor, Color.white);
        StyleButton(exitButton, new Color32(0x10, 0x12, 0x16, 0xFF), Color.white);
    }

    public void UpdateMap(
        Vector3 playerWorldPosition,
        IReadOnlyList<Vector3> routePoints,
        int targetIndex,
        int completedIndex,
        bool routeComplete)
    {
        if (mapArea == null || playerDot == null)
        {
            return;
        }

        FitMapAreaToViewport(false);

        Vector2 targetPlayerPosition = WorldToMapPosition(playerWorldPosition);
        if (!hasPlayerPosition)
        {
            smoothedPlayerPosition = targetPlayerPosition;
            hasPlayerPosition = true;
        }
        else
        {
            float blend = 1f - Mathf.Exp(-playerLerpSpeed * Time.deltaTime);
            smoothedPlayerPosition = Vector2.Lerp(smoothedPlayerPosition, targetPlayerPosition, blend);
        }

        playerDot.anchoredPosition = smoothedPlayerPosition;
        UpdatePlayerPulse();
        UpdateRoute(routePoints, routeComplete);
        UpdateMarkerStates(targetIndex, completedIndex, routeComplete);
    }

    public Vector2 WorldToMapPosition(Vector3 worldPosition)
    {
        if (mapArea == null)
        {
            return Vector2.zero;
        }

        float normalizedX = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPosition.x);
        float normalizedY = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPosition.z);
        Vector2 mapSize = mapArea.rect.size;
        float usableWidth = Mathf.Max(1f, mapSize.x - (mapPadding * 2f));
        float usableHeight = Mathf.Max(1f, mapSize.y - (mapPadding * 2f));

        return new Vector2(
            (Mathf.Clamp01(normalizedX) - 0.5f) * usableWidth,
            (Mathf.Clamp01(normalizedY) - 0.5f) * usableHeight);
    }

    private void SetStops(IReadOnlyList<LocationTrigger> orderedStops)
    {
        stops.Clear();
        if (orderedStops != null)
        {
            for (int i = 0; i < orderedStops.Count; i++)
            {
                if (orderedStops[i] != null)
                {
                    stops.Add(orderedStops[i]);
                }
            }
        }

        while (stopMarkers.Count < stops.Count)
        {
            stopMarkers.Add(CreateStopMarker(stopMarkers.Count));
        }

        for (int i = 0; i < stopMarkers.Count; i++)
        {
            bool active = i < stops.Count;
            stopMarkers[i].Root.gameObject.SetActive(active);
            if (active)
            {
                stopMarkers[i].Label.text = GetShortLabel(stops[i].LocationName, i);
            }
        }

        PositionStopMarkers();
        lastTargetIndex = int.MinValue;
        lastCompletedIndex = int.MinValue;
    }

    private StopMarkerView CreateStopMarker(int index)
    {
        GameObject rootObject = new GameObject("MiniMapStop_" + (index + 1), typeof(RectTransform));
        rootObject.transform.SetParent(mapArea, false);
        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = index == 0 ? new Vector2(70f, 54f) : new Vector2(62f, 50f);

        Image ring = CreateImage("Ring", root, circleSprite, Color.white);
        RectTransform ringRect = ring.rectTransform;
        ringRect.anchorMin = new Vector2(0.5f, 0.5f);
        ringRect.anchorMax = new Vector2(0.5f, 0.5f);
        ringRect.pivot = new Vector2(0.5f, 0.5f);
        ringRect.sizeDelta = index == 0 ? new Vector2(30f, 30f) : new Vector2(24f, 24f);
        ringRect.anchoredPosition = new Vector2(0f, 8f);

        Outline outline = ring.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.28f);
        outline.effectDistance = new Vector2(2f, -2f);

        Image fill = CreateImage("Fill", ringRect, circleSprite, activeColor);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = new Vector2(0.5f, 0.5f);
        fillRect.anchorMax = new Vector2(0.5f, 0.5f);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.sizeDelta = index == 0 ? new Vector2(16f, 16f) : new Vector2(12f, 12f);
        fillRect.anchoredPosition = Vector2.zero;

        Text label = CreateText("Label", root, GetShortLabel(string.Empty, index), 10, FontStyle.Bold, labelColor);
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = new Vector2(82f, 16f);
        labelRect.anchoredPosition = new Vector2(0f, -6f);

        return new StopMarkerView
        {
            Root = root,
            Ring = ring,
            Fill = fill,
            Label = label
        };
    }

    private void PositionStopMarkers()
    {
        for (int i = 0; i < stops.Count && i < stopMarkers.Count; i++)
        {
            stopMarkers[i].Root.anchoredPosition = WorldToMapPosition(stops[i].transform.position);
        }
    }

    private void UpdateMarkerStates(int targetIndex, int completedIndex, bool routeComplete)
    {
        if (targetIndex == lastTargetIndex && completedIndex == lastCompletedIndex && !routeComplete)
        {
            return;
        }

        lastTargetIndex = targetIndex;
        lastCompletedIndex = completedIndex;

        for (int i = 0; i < stopMarkers.Count && i < stops.Count; i++)
        {
            bool completed = routeComplete || i <= completedIndex;
            bool active = !routeComplete && i == targetIndex;
            Color stateColor = active ? activeColor : completed ? completedColor : upcomingColor;

            StopMarkerView marker = stopMarkers[i];
            marker.Ring.color = Color.white;
            marker.Fill.color = stateColor;
            marker.Label.color = active ? new Color32(0x0B, 0x42, 0x74, 0xFF) : labelColor;
            marker.Root.localScale = active ? Vector3.one * 1.12f : Vector3.one;
        }
    }

    private void UpdateRoute(IReadOnlyList<Vector3> routePoints, bool routeComplete)
    {
        if (routeComplete || routePoints == null || routePoints.Count < 2)
        {
            HideRoute();
            return;
        }

        int segmentIndex = 0;
        for (int i = 0; i < routePoints.Count - 1 && segmentIndex < pathSegments.Count; i++)
        {
            Vector2 start = i == 0 ? smoothedPlayerPosition : WorldToMapPosition(routePoints[i]);
            Vector2 end = WorldToMapPosition(routePoints[i + 1]);
            Vector2 direction = end - start;
            if (direction.sqrMagnitude < 1f)
            {
                continue;
            }

            RectTransform segment = pathSegments[segmentIndex];
            if (segment == null)
            {
                continue;
            }

            segment.gameObject.SetActive(true);
            segment.SetParent(mapArea, false);
            segment.SetAsFirstSibling();
            segment.anchoredPosition = start + direction * 0.5f;
            segment.sizeDelta = new Vector2(direction.magnitude, pathThickness);
            segment.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            TintSegment(segment, routeColor);
            segmentIndex++;
        }

        for (int i = segmentIndex; i < pathSegments.Count; i++)
        {
            if (pathSegments[i] != null)
            {
                pathSegments[i].gameObject.SetActive(false);
            }
        }

        for (int i = 0; i < stopMarkers.Count; i++)
        {
            stopMarkers[i].Root.SetAsLastSibling();
        }

        playerDot.SetAsLastSibling();
    }

    private void HideRoute()
    {
        if (legacyPathLine != null)
        {
            legacyPathLine.gameObject.SetActive(false);
        }

        for (int i = 0; i < pathSegments.Count; i++)
        {
            if (pathSegments[i] != null)
            {
                pathSegments[i].gameObject.SetActive(false);
            }
        }
    }

    private void EnsureRouteSegments()
    {
        while (pathSegments.Count < 12)
        {
            RectTransform segment = CreateSegment("PathSegment_Runtime_" + (pathSegments.Count + 1));
            pathSegments.Add(segment);
        }

        for (int i = 0; i < pathSegments.Count; i++)
        {
            RectTransform segment = pathSegments[i];
            if (segment == null)
            {
                continue;
            }

            segment.SetParent(mapArea, false);
            segment.pivot = new Vector2(0.5f, 0.5f);
            Image image = segment.GetComponent<Image>();
            if (image == null)
            {
                image = segment.gameObject.AddComponent<Image>();
            }

            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;

            Transform fill = segment.Find("Fill");
            if (fill == null)
            {
                Image fillImage = CreateImage("Fill", segment, roundedSprite, routeColor);
                Stretch(fillImage.rectTransform, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            }
        }
    }

    private RectTransform CreateSegment(string name)
    {
        Image image = CreateImage(name, mapArea, roundedSprite, Color.white);
        RectTransform rect = image.rectTransform;
        rect.sizeDelta = new Vector2(120f, pathThickness);
        return rect;
    }

    private void EnsurePlayerDot()
    {
        if (playerDot == null)
        {
            Image dotImage = CreateImage("PlayerDot", mapArea, circleSprite, Color.white);
            playerDot = dotImage.rectTransform;
        }
        else
        {
            playerDot.SetParent(mapArea, false);
            playerDot.gameObject.SetActive(true);
        }

        playerDot.anchorMin = new Vector2(0.5f, 0.5f);
        playerDot.anchorMax = new Vector2(0.5f, 0.5f);
        playerDot.pivot = new Vector2(0.5f, 0.5f);
        playerDot.sizeDelta = new Vector2(46f, 46f);

        Image image = playerDot.GetComponent<Image>();
        if (image == null)
        {
            image = playerDot.gameObject.AddComponent<Image>();
        }

        image.sprite = circleSprite;
        image.color = Color.white;
        image.raycastTarget = false;

        EnsurePlayerChild("Glow", new Vector2(74f, 74f), new Color(activeColor.r, activeColor.g, activeColor.b, 0.22f), true);
        EnsurePlayerChild("Inner", new Vector2(20f, 20f), activeColor, false);
        playerDot.SetAsLastSibling();
    }

    private void EnsurePlayerChild(string name, Vector2 size, Color color, bool firstSibling)
    {
        Transform child = playerDot.Find(name);
        Image childImage;
        RectTransform childRect;
        if (child == null)
        {
            childImage = CreateImage(name, playerDot, circleSprite, color);
            childRect = childImage.rectTransform;
        }
        else
        {
            childRect = child as RectTransform;
            childImage = child.GetComponent<Image>();
            if (childImage == null)
            {
                childImage = child.gameObject.AddComponent<Image>();
            }
        }

        childImage.sprite = circleSprite;
        childImage.color = color;
        childImage.raycastTarget = false;
        childRect.anchorMin = new Vector2(0.5f, 0.5f);
        childRect.anchorMax = new Vector2(0.5f, 0.5f);
        childRect.pivot = new Vector2(0.5f, 0.5f);
        childRect.sizeDelta = size;
        childRect.anchoredPosition = Vector2.zero;

        if (firstSibling)
        {
            childRect.SetAsFirstSibling();
        }
    }

    private void UpdatePlayerPulse()
    {
        Transform glow = playerDot != null ? playerDot.Find("Glow") : null;
        if (glow == null)
        {
            return;
        }

        float pulse = 1f + Mathf.Sin(Time.time * 4.1f) * 0.08f;
        glow.localScale = Vector3.one * pulse;
    }

    private void HideLegacyTargetDot()
    {
        if (targetDot != null)
        {
            targetDot.gameObject.SetActive(false);
        }
    }

    private void StyleMapChrome()
    {
        RectTransform frame = mapViewport != null ? mapViewport.parent as RectTransform : null;
        if (frame != null)
        {
            Image frameImage = frame.GetComponent<Image>();
            if (frameImage != null)
            {
                frameImage.sprite = roundedSprite;
                frameImage.type = Image.Type.Sliced;
                frameImage.color = panelColor;
            }

            Outline outline = frame.GetComponent<Outline>();
            if (outline == null)
            {
                outline = frame.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.16f);
            outline.effectDistance = new Vector2(1f, -1f);

            Shadow shadow = frame.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = frame.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.24f);
            shadow.effectDistance = new Vector2(0f, -8f);

            Text title = frame.Find("MapTitle") != null ? frame.Find("MapTitle").GetComponent<Text>() : null;
            if (title != null)
            {
                title.text = AppLanguage.Text("mini_map");
                title.color = labelColor;
                title.fontSize = 16;
            }
        }

        if (mapViewport != null)
        {
            Image viewportImage = mapViewport.GetComponent<Image>();
            if (viewportImage != null)
            {
                viewportImage.sprite = roundedSprite;
                viewportImage.type = Image.Type.Sliced;
                viewportImage.color = mapSurfaceColor;
            }
        }

        Image mapImage = mapArea.GetComponent<Image>();
        if (mapImage != null)
        {
            mapImage.color = Color.white;
            mapImage.preserveAspect = true;
            mapImage.raycastTarget = false;
        }
    }

    private void FitMapAreaToViewport(bool force)
    {
        if (mapViewport == null || mapArea == null)
        {
            return;
        }

        Vector2 viewportSize = mapViewport.rect.size;
        if (viewportSize.x <= 1f || viewportSize.y <= 1f)
        {
            return;
        }

        if (!force && viewportSize == lastViewportSize)
        {
            return;
        }

        lastViewportSize = viewportSize;
        const float innerPadding = 10f;
        Vector2 availableSize = new Vector2(
            Mathf.Max(1f, viewportSize.x - innerPadding),
            Mathf.Max(1f, viewportSize.y - innerPadding));
        Vector2 fittedSize = availableSize;

        float mapAspect = GetMapAspectRatio();
        if (mapAspect > 0.01f)
        {
            float availableAspect = availableSize.x / availableSize.y;
            if (availableAspect > mapAspect)
            {
                fittedSize.x = availableSize.y * mapAspect;
            }
            else
            {
                fittedSize.y = availableSize.x / mapAspect;
            }
        }

        mapArea.anchorMin = new Vector2(0.5f, 0.5f);
        mapArea.anchorMax = new Vector2(0.5f, 0.5f);
        mapArea.pivot = new Vector2(0.5f, 0.5f);
        mapArea.sizeDelta = new Vector2(Mathf.Round(fittedSize.x), Mathf.Round(fittedSize.y));
        mapArea.anchoredPosition = Vector2.zero;
        mapArea.localScale = Vector3.one * mapZoom;
        PositionStopMarkers();
    }

    private float GetMapAspectRatio()
    {
        Image mapImage = mapArea != null ? mapArea.GetComponent<Image>() : null;
        Sprite sprite = mapImage != null ? mapImage.sprite : null;
        if (sprite == null || sprite.rect.height <= 0f)
        {
            return 0f;
        }

        return sprite.rect.width / sprite.rect.height;
    }

    private void EnsureMapControls()
    {
        if (mapViewport == null)
        {
            return;
        }

        Transform existing = mapViewport.Find(ControlsRootName);
        if (existing != null)
        {
            DestroyUnityObject(existing.gameObject);
        }

        GameObject controlsObject = new GameObject(ControlsRootName, typeof(RectTransform));
        controlsObject.transform.SetParent(mapViewport, false);
        controlsRoot = controlsObject.GetComponent<RectTransform>();
        controlsRoot.anchorMin = Vector2.zero;
        controlsRoot.anchorMax = Vector2.one;
        controlsRoot.offsetMin = Vector2.zero;
        controlsRoot.offsetMax = Vector2.zero;
        controlsRoot.SetAsLastSibling();

        Button zoomIn = CreateControlButton("ZoomInButton", controlsRoot, new Vector2(1f, 0f), new Vector2(34f, 34f), new Vector2(-24f, 82f), "+", () => ZoomMap(0.08f));
        Button zoomOut = CreateControlButton("ZoomOutButton", controlsRoot, new Vector2(1f, 0f), new Vector2(34f, 34f), new Vector2(-24f, 46f), "-", () => ZoomMap(-0.08f));

        SetButtonTextSize(zoomIn, 22);
        SetButtonTextSize(zoomOut, 24);
    }

    private Button CreateControlButton(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = roundedSprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 1f, 1f, 0.96f);

        Shadow shadow = buttonObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.16f);
        shadow.effectDistance = new Vector2(0f, -3f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.96f, 0.98f, 1f, 0.98f);
        colors.pressedColor = new Color(0.86f, 0.92f, 1f, 0.98f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        if (!string.IsNullOrEmpty(label))
        {
            Text buttonText = CreateText("Text", rect, label, 13, FontStyle.Bold, labelColor);
            Stretch(buttonText.rectTransform);
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.resizeTextForBestFit = true;
            buttonText.resizeTextMinSize = 10;
            buttonText.resizeTextMaxSize = 13;
        }

        return button;
    }

    private void ZoomMap(float delta)
    {
        mapZoom = Mathf.Clamp(mapZoom + delta, minZoom, maxZoom);
        if (mapArea != null)
        {
            mapArea.localScale = Vector3.one * mapZoom;
        }
    }

    private void StyleButton(Button button, Color background, Color textColor)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = background;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = background;
        colors.highlightedColor = Color.Lerp(background, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(background, Color.black, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(background.r, background.g, background.b, 0.42f);
        button.colors = colors;

        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.color = textColor;
        }
    }

    private Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Text CreateText(string name, Transform parent, string content, int size, FontStyle style, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = labelFont != null ? labelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void SetButtonTextSize(Button button, int fontSize)
    {
        Text text = button != null ? button.GetComponentInChildren<Text>() : null;
        if (text != null)
        {
            text.fontSize = fontSize;
        }
    }

    private void TintSegment(RectTransform segment, Color color)
    {
        Image image = segment.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(1f, 1f, 1f, 0.96f);
        }

        Transform fill = segment.Find("Fill");
        Image fillImage = fill != null ? fill.GetComponent<Image>() : null;
        if (fillImage != null)
        {
            fillImage.sprite = roundedSprite;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = new Color(color.r, color.g, color.b, 0.98f);
        }
    }

    private static string GetShortLabel(string locationName, int index)
    {
        if (index == 0)
        {
            return "BAL";
        }

        if (string.IsNullOrWhiteSpace(locationName))
        {
            return "S" + (index + 1);
        }

        string normalized = locationName.ToLowerInvariant();
        if (normalized.Contains("sala")) return "SALA";
        if (normalized.Contains("dining")) return "DIN";
        if (normalized.Contains("bedroom")) return "BED";
        if (normalized.Contains("family")) return "FAM";
        if (normalized.Contains("secret")) return "SEC";
        if (normalized.Contains("war")) return "WAR";
        if (normalized.Contains("document")) return "DOC";
        if (normalized.Contains("garden")) return "GDN";
        if (normalized.Contains("balcony")) return "BAL";

        string compact = locationName.Replace(" ", string.Empty);
        return compact.Length <= 4 ? compact.ToUpperInvariant() : compact.Substring(0, 4).ToUpperInvariant();
    }

    private static Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;
        float feather = 2.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01((radius - distance) / feather);
                texture.SetPixel(x, y, alpha <= 0f ? clear : new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateRoundedRectSprite(int width, int height, int radius)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1f, 1f, 1f, 0f);
        float feather = 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float innerX = Mathf.Clamp(x, radius, width - radius - 1);
                float innerY = Mathf.Clamp(y, radius, height - radius - 1);
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(innerX, innerY));
                float alpha = distance <= radius ? 1f : Mathf.Clamp01((radius + feather - distance) / feather);
                texture.SetPixel(x, y, alpha <= 0f ? clear : new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
    }

    private static void Stretch(RectTransform rectTransform)
    {
        Stretch(rectTransform, Vector2.zero, Vector2.zero);
    }

    private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private static void DestroyUnityObject(Object unityObject)
    {
        if (unityObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(unityObject);
        }
        else
        {
            DestroyImmediate(unityObject);
        }
    }
}
