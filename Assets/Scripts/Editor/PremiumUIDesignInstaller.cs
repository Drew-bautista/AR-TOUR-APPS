using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PremiumUIDesignInstaller
{
    private const string TourScenePath = "Assets/Scenes/AguinaldoShrineARTour.unity";

    [MenuItem("Tools/Aguinaldo Shrine AR Tour/Apply Full Premium UI Redesign")]
    public static void ApplyFullPremiumRedesign()
    {
        PremiumHomeSceneBuilder.CreateOrReplaceHomeScene();
        InstallTourStyler();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Applied full premium UI redesign to HomeScene and AguinaldoShrineARTour.");
    }

    private static void InstallTourStyler()
    {
        Sprite premiumMap = PremiumMiniMapArtworkGenerator.GenerateOrUpdateMapSprite();
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(TourScenePath);
        Canvas tourCanvas = Object.FindFirstObjectByType<Canvas>();
        if (tourCanvas == null)
        {
            Debug.LogError("TourCanvas was not found. Regenerate the tour scene before applying premium UI.");
            return;
        }

        if (tourCanvas.GetComponent<PremiumTourUIStyler>() == null)
        {
            tourCanvas.gameObject.AddComponent<PremiumTourUIStyler>();
        }

        PolishTopBar();
        PolishMiniMap(premiumMap);
        DisableLegacyGuideVisuals();
        EditorUtility.SetDirty(tourCanvas.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void PolishTopBar()
    {
        RectTransform topPanel = FindRectTransform("TopPanel");
        if (topPanel != null)
        {
            topPanel.anchorMin = new Vector2(0.035f, 1f);
            topPanel.anchorMax = new Vector2(0.965f, 1f);
            topPanel.pivot = new Vector2(0.5f, 1f);
            topPanel.sizeDelta = new Vector2(0f, 124f);
            topPanel.anchoredPosition = new Vector2(0f, -24f);
            EditorUtility.SetDirty(topPanel);
        }

        RectTransform title = FindRectTransform("TitleText");
        if (title != null)
        {
            title.anchorMin = new Vector2(0f, 1f);
            title.anchorMax = new Vector2(0.48f, 1f);
            title.pivot = new Vector2(0f, 1f);
            title.sizeDelta = new Vector2(-24f, 48f);
            title.anchoredPosition = new Vector2(22f, -14f);
            SetTextSize(title, 26, 16, 26);
            EditorUtility.SetDirty(title);
        }

        RectTransform status = FindRectTransform("StatusText");
        if (status != null)
        {
            status.anchorMin = new Vector2(0f, 1f);
            status.anchorMax = new Vector2(0.48f, 1f);
            status.pivot = new Vector2(0f, 1f);
            status.sizeDelta = new Vector2(-24f, 36f);
            status.anchoredPosition = new Vector2(22f, -66f);
            SetTextSize(status, 16, 10, 16);
            EditorUtility.SetDirty(status);
        }

        LayoutTopButton("MuteToggleButton", new Vector2(42f, 42f), new Vector2(-470f, -2f), false);
        LayoutTopButton("CameraToggleButton", new Vector2(42f, 42f), new Vector2(-416f, -2f), false);
        LayoutTopButton("ScanItemButton", new Vector2(172f, 48f), new Vector2(-226f, -2f), true);
        LayoutTopButton("GalleryButton", new Vector2(172f, 48f), new Vector2(-38f, -2f), true);

        RectTransform cameraHintPanel = FindRectTransform("CameraHintPanel");
        if (cameraHintPanel != null)
        {
            cameraHintPanel.anchorMin = new Vector2(0.5f, 1f);
            cameraHintPanel.anchorMax = new Vector2(0.5f, 1f);
            cameraHintPanel.pivot = new Vector2(0.5f, 1f);
            cameraHintPanel.sizeDelta = new Vector2(760f, 52f);
            cameraHintPanel.anchoredPosition = new Vector2(0f, -154f);
            EditorUtility.SetDirty(cameraHintPanel);
        }

        RectTransform cameraHintText = FindRectTransform("CameraHintText");
        if (cameraHintText != null)
        {
            SetTextSize(cameraHintText, 15, 10, 15);
            EditorUtility.SetDirty(cameraHintText);
        }
    }

    private static void LayoutTopButton(string name, Vector2 size, Vector2 position, bool pillButton)
    {
        RectTransform rect = FindRectTransform(name);
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

        if (pillButton)
        {
            SetTextSize(rect, 20, 12, 20);
        }

        EditorUtility.SetDirty(rect);
    }

    private static void SetTextSize(RectTransform rect, int size, int minSize, int maxSize)
    {
        Text text = rect != null ? rect.GetComponent<Text>() : null;
        if (text == null)
        {
            text = rect != null ? rect.GetComponentInChildren<Text>(true) : null;
        }

        if (text == null)
        {
            return;
        }

        text.fontSize = size;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = minSize;
        text.resizeTextMaxSize = maxSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        EditorUtility.SetDirty(text);
    }

    private static void PolishMiniMap(Sprite premiumMap)
    {
        RectTransform mapFrame = FindRectTransform("MapFrame");
        if (mapFrame != null)
        {
            mapFrame.anchorMin = new Vector2(0f, 0f);
            mapFrame.anchorMax = new Vector2(1f, 0f);
            mapFrame.pivot = new Vector2(0.5f, 0f);
            mapFrame.sizeDelta = new Vector2(-48f, 366f);
            mapFrame.anchoredPosition = new Vector2(0f, 122f);
            EditorUtility.SetDirty(mapFrame);
        }

        RectTransform mapTitle = FindRectTransform("MapTitle");
        if (mapTitle != null)
        {
            mapTitle.anchorMin = new Vector2(0f, 1f);
            mapTitle.anchorMax = new Vector2(1f, 1f);
            mapTitle.pivot = new Vector2(0f, 1f);
            mapTitle.sizeDelta = new Vector2(-40f, 26f);
            mapTitle.anchoredPosition = new Vector2(20f, -14f);
            EditorUtility.SetDirty(mapTitle);
        }

        RectTransform mapViewport = FindRectTransform("MapViewport");
        if (mapViewport != null)
        {
            mapViewport.anchorMin = new Vector2(0f, 0f);
            mapViewport.anchorMax = new Vector2(1f, 1f);
            mapViewport.pivot = new Vector2(0.5f, 0.5f);
            mapViewport.offsetMin = new Vector2(18f, 18f);
            mapViewport.offsetMax = new Vector2(-18f, -58f);

            if (mapViewport.GetComponent<RectMask2D>() == null)
            {
                mapViewport.gameObject.AddComponent<RectMask2D>();
            }

            EditorUtility.SetDirty(mapViewport);
        }

        RectTransform mapArea = FindRectTransform("MapArea");
        Image mapImage = mapArea != null ? mapArea.GetComponent<Image>() : null;
        if (mapImage != null)
        {
            if (premiumMap != null)
            {
                mapImage.sprite = premiumMap;
            }

            mapImage.preserveAspect = true;
            mapImage.raycastTarget = false;
            EditorUtility.SetDirty(mapImage);
        }

        MiniMapController miniMap = Object.FindFirstObjectByType<MiniMapController>();
        if (miniMap != null)
        {
            SerializedObject serializedMap = new SerializedObject(miniMap);
            SetSerializedFloat(serializedMap, "mapPadding", 30f);
            SetSerializedFloat(serializedMap, "pathThickness", 8f);
            SetSerializedFloat(serializedMap, "minZoom", 1f);
            SetSerializedFloat(serializedMap, "maxZoom", 1.22f);
            SetSerializedColor(serializedMap, "panelColor", new Color32(0xFA, 0xFC, 0xFF, 0xFA));
            SetSerializedColor(serializedMap, "mapSurfaceColor", new Color32(0xF2, 0xF7, 0xFA, 0xFF));
            SetSerializedColor(serializedMap, "activeColor", new Color32(0x1E, 0x8F, 0xFF, 0xFF));
            SetSerializedColor(serializedMap, "routeColor", new Color32(0x13, 0x83, 0xF6, 0xFF));
            serializedMap.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(miniMap);
        }
    }

    private static RectTransform FindRectTransform(string name)
    {
        RectTransform[] transforms = Resources.FindObjectsOfTypeAll<RectTransform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null &&
                transforms[i].name == name &&
                transforms[i].gameObject.scene.IsValid())
            {
                return transforms[i];
            }
        }

        return null;
    }

    private static void SetSerializedFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetSerializedColor(SerializedObject serializedObject, string propertyName, Color value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.colorValue = value;
        }
    }

    private static void DisableLegacyGuideVisuals()
    {
        NavigationManager[] managers = Object.FindObjectsByType<NavigationManager>(FindObjectsSortMode.None);
        for (int i = 0; i < managers.Length; i++)
        {
            SerializedObject serializedManager = new SerializedObject(managers[i]);
            SerializedProperty showCameraArrow = serializedManager.FindProperty("showCameraArrow");
            if (showCameraArrow != null)
            {
                showCameraArrow.boolValue = false;
            }

            SerializedProperty showLegacyWorldGuide = serializedManager.FindProperty("showLegacyWorldGuide");
            if (showLegacyWorldGuide != null)
            {
                showLegacyWorldGuide.boolValue = false;
            }

            DisableTransformReference(serializedManager, "arrowAnchor");
            DisableTransformReference(serializedManager, "destinationBeacon");
            DisableBreadcrumbs(serializedManager);
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(managers[i]);
        }
    }

    private static void DisableTransformReference(SerializedObject serializedObject, string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.objectReferenceValue is Transform target)
        {
            target.gameObject.SetActive(false);
            EditorUtility.SetDirty(target.gameObject);
        }
    }

    private static void DisableBreadcrumbs(SerializedObject serializedObject)
    {
        SerializedProperty breadcrumbs = serializedObject.FindProperty("breadcrumbDots");
        if (breadcrumbs == null)
        {
            return;
        }

        for (int i = 0; i < breadcrumbs.arraySize; i++)
        {
            if (breadcrumbs.GetArrayElementAtIndex(i).objectReferenceValue is Transform dot)
            {
                dot.gameObject.SetActive(false);
                EditorUtility.SetDirty(dot.gameObject);
            }
        }
    }
}
