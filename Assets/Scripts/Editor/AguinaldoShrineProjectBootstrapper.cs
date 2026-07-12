using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARCore;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;
using Object = UnityEngine.Object;

/// <summary>
/// Generates the beginner-friendly Unity scenes for the Aguinaldo Shrine AR tour.
/// The generated scenes use the exact runtime scripts required by the project brief.
/// </summary>
public static class AguinaldoShrineProjectBootstrapper
{
    private const string PromptKey = "AguinaldoShrineARTour.FirstOpenPrompt";
    private const string HomeScenePath = "Assets/Scenes/HomeScene.unity";
    private const string TourScenePath = "Assets/Scenes/AguinaldoShrineARTour.unity";
    private const string MapTexturePath = "Assets/Art/Generated/AguinaldoShrineMiniMap.png";
    private const string GalleryMetadataPath = "Assets/Art/Generated/AguinaldoGalleryMetadata.json";
    private const string GalleryPhotoFolder = "Assets/all pictures AR";
    private const string QrMarkerFolder = "Assets/Art/Generated/QRMarkers";
    private const int QrMarkerPixelSize = 512;
    private const int QrMarkerModuleCount = 33;
    private const int MaxTrackedScanItems = 256;
    private const string ArrowMaterialPath = "Assets/Art/Generated/AguinaldoArrow.mat";
    private const string PrimaryMaterialPath = "Assets/Art/Generated/AguinaldoGuidePrimary.mat";
    private const string AccentMaterialPath = "Assets/Art/Generated/AguinaldoGuideAccent.mat";
    private static readonly Vector2 QuickCameraButtonPosition = new Vector2(-100f, -88f);
    private static readonly Vector2 QuickMuteButtonPosition = new Vector2(-36f, -88f);
    private static readonly Vector2 RouteMapMin = new Vector2(-1f, -1f);
    private static readonly Vector2 RouteMapMax = new Vector2(7f, 12f);
    private static readonly string[] GalleryDefinitionTemplates =
    {
        "Archive image from the Aguinaldo Shrine AR collection. This photo is included in the application so visitors can review visual details while exploring the heritage tour.",
        "Reference photo preserved for the mobile AR experience. This image supports in-app storytelling by giving visitors another visual point of comparison inside the shrine.",
        "Curated visual from the Aguinaldo Shrine archive. Use this image as a gallery reference while moving between exhibits, rooms, and historical landmarks.",
        "Heritage documentation image included in the application. It helps connect the physical site with the tour's guided explanations and AR experience."
    };
    private static readonly string[] ScanDescriptionTemplates =
    {
        "Reference image used by the AR Scan Info System. When the app recognizes this image, it shows a short heritage summary and starts narration for visitors.",
        "Predefined scan marker from the Aguinaldo Shrine mobile guide. This item demonstrates how image recognition can open historical notes and voice explanations.",
        "AR scan reference included in the shrine project. Match this image from the camera or gallery to display information about the selected heritage item.",
        "Guided scan asset for the Aguinaldo Shrine experience. It connects image recognition, on-screen description, and narration playback in one mobile workflow."
    };

    [InitializeOnLoadMethod]
    private static void PromptOnFirstOpen()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (EditorPrefs.GetBool(PromptKey))
        {
            return;
        }

        EditorPrefs.SetBool(PromptKey, true);
        if (File.Exists(HomeScenePath) && File.Exists(TourScenePath))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (EditorUtility.DisplayDialog(
                    "Aguinaldo Shrine AR Tour",
                    "Generate the Home scene and AR Tour scene now?",
                    "Generate Scenes",
                    "Later"))
            {
                GenerateStarterScenes();
            }
        };
    }

    [MenuItem("Tools/Aguinaldo Shrine AR Tour/Generate Starter Scenes")]
    public static void GenerateStarterScenes()
    {
        EnsureFolders();
        ApplyProjectSettings();
        EnsureDemoMaterials();

        Sprite mapSprite = LoadOrCreateMapSprite();
        List<GalleryPhotoImportData> galleryPhotos = LoadGalleryPhotoDefinitions();
        List<ScanItemImportData> scanItems = LoadScanItemDefinitions();
        CreateHomeScene();
        CreateTourScene(mapSprite, galleryPhotos, scanItems);
        ApplyBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!Application.isBatchMode)
        {
            EditorSceneManager.OpenScene(HomeScenePath);
            EditorUtility.DisplayDialog(
                "Generation Complete",
                "HomeScene and AguinaldoShrineARTour are ready for Android AR testing.",
                "OK");
        }
    }

    [MenuItem("Tools/Aguinaldo Shrine AR Tour/Refresh Scan Database From All Pictures AR")]
    public static void RefreshScanDatabaseFromAllPicturesAR()
    {
        EnsureFolders();
        List<ScanItemImportData> scanItems = LoadScanItemDefinitions();
        if (scanItems.Count == 0)
        {
            throw new BuildFailedException("No supported scan images were found in Assets/all pictures AR.");
        }

        EditorSceneManager.OpenScene(TourScenePath);

        ImageRecognitionManager imageRecognitionManager = Object.FindFirstObjectByType<ImageRecognitionManager>();
        XROrigin xrOrigin = Object.FindFirstObjectByType<XROrigin>();
        ARTrackedImageManager trackedImageManager = Object.FindFirstObjectByType<ARTrackedImageManager>();
        GalleryPicker galleryPicker = Object.FindFirstObjectByType<GalleryPicker>();
        ScanUIController scanUIController = Object.FindFirstObjectByType<ScanUIController>();
        AudioManager audioManager = Object.FindFirstObjectByType<AudioManager>();

        if (imageRecognitionManager == null)
        {
            throw new BuildFailedException("ImageRecognitionManager was not found in the AR tour scene.");
        }

        Button qrModeButton = EnsureQrScanButtonInScene(scanUIController);
        Button qrShortcutButton = EnsureFloatingQrShortcutButtonInScene(scanUIController);
        QrScanUi qrScanUi = EnsureQrScanOverlayInScene(scanUIController);

        ConfigureImageRecognitionManager(
            imageRecognitionManager,
            xrOrigin,
            trackedImageManager,
            galleryPicker,
            scanUIController,
            audioManager,
            scanItems);

        if (scanUIController != null)
        {
            SerializedObject serializedScanUi = new SerializedObject(scanUIController);
            serializedScanUi.FindProperty("qrModeButton").objectReferenceValue = qrModeButton;
            serializedScanUi.FindProperty("qrShortcutButton").objectReferenceValue = qrShortcutButton;
            serializedScanUi.FindProperty("qrCloseButton").objectReferenceValue = qrScanUi.CloseButton;
            serializedScanUi.FindProperty("qrScanOverlay").objectReferenceValue = qrScanUi.Overlay;
            serializedScanUi.FindProperty("qrScanHintText").objectReferenceValue = qrScanUi.HintText;
            serializedScanUi.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Aguinaldo Shrine scan database refreshed from Assets/all pictures AR. Scan items: " +
            scanItems.Count +
            ", tracked references: " +
            CountTrackedScanItems(scanItems));
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Art");
        Directory.CreateDirectory("Assets/Art/Generated");
        Directory.CreateDirectory(QrMarkerFolder);
        Directory.CreateDirectory("Assets/XR");
    }

    private static void ApplyProjectSettings()
    {
        PlayerSettings.companyName = "Aguinaldo Shrine";
        PlayerSettings.productName = "Digital Heritage Archive tour";
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.aguinaldoshrine.artour");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });

        EnsureAndroidXRConfiguration();
    }

    private static void ApplyBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(HomeScenePath, true),
            new EditorBuildSettingsScene(TourScenePath, true)
        };
    }

    private static void CreateHomeScene()
    {
        PremiumHomeSceneBuilder.CreateOrReplaceHomeScene();
    }

    private static void CreateTourScene(
        Sprite mapSprite,
        IReadOnlyList<GalleryPhotoImportData> galleryPhotos,
        IReadOnlyList<ScanItemImportData> scanItems)
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateEventSystem();
        EditorApplication.ExecuteMenuItem("GameObject/XR/AR Session");
        EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)");

        ARSession arSession = Object.FindFirstObjectByType<ARSession>();
        XROrigin xrOrigin = Object.FindFirstObjectByType<XROrigin>();
        Camera arCamera = xrOrigin != null && xrOrigin.Camera != null ? xrOrigin.Camera : Camera.main;

        if (arSession == null || xrOrigin == null || arCamera == null)
        {
            throw new BuildFailedException(
                "Unity could not create the AR Session and XR Origin.\n" +
                "Make sure AR Foundation and XR Plug-in Management finished importing, then run the generator again.");
        }

        EnsureARCameraSetup(arCamera);

        ARPlaneManager planeManager = xrOrigin.GetComponent<ARPlaneManager>();
        if (planeManager == null)
        {
            planeManager = xrOrigin.gameObject.AddComponent<ARPlaneManager>();
        }

        ARRaycastManager raycastManager = xrOrigin.GetComponent<ARRaycastManager>();
        if (raycastManager == null)
        {
            raycastManager = xrOrigin.gameObject.AddComponent<ARRaycastManager>();
        }

        ARAnchorManager anchorManager = xrOrigin.GetComponent<ARAnchorManager>();
        if (anchorManager == null)
        {
            anchorManager = xrOrigin.gameObject.AddComponent<ARAnchorManager>();
        }

        ARTrackedImageManager trackedImageManager = xrOrigin.GetComponent<ARTrackedImageManager>();
        if (trackedImageManager == null)
        {
            trackedImageManager = xrOrigin.gameObject.AddComponent<ARTrackedImageManager>();
        }

        trackedImageManager.enabled = false;
        trackedImageManager.requestedMaxNumberOfMovingImages = 1;

        planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;

        Material arrowMaterial = LoadOrCreateMaterial(ArrowMaterialPath, new Color(0.12f, 0.45f, 0.92f), new Color(0.06f, 0.22f, 0.46f));
        Material primaryMaterial = LoadOrCreateMaterial(PrimaryMaterialPath, new Color(0.06f, 0.12f, 0.18f), new Color(0f, 0f, 0f));
        Material accentMaterial = LoadOrCreateMaterial(AccentMaterialPath, new Color(0.96f, 0.68f, 0.26f), new Color(0.2f, 0.08f, 0f));

        GameObject tourLocationsRoot = new GameObject("TourLocations");
        TourLocationDefinition[] definitions = GetTourDefinitions();
        List<LocationTrigger> locations = new List<LocationTrigger>();
        for (int i = 0; i < definitions.Length; i++)
        {
            locations.Add(CreateLocationObject(tourLocationsRoot.transform, definitions[i]));
        }

        GameObject liveViewNavigationObject = new GameObject("GoogleMapsLiveViewNavigation", typeof(AudioSource));
        AudioSource liveViewAudioSource = liveViewNavigationObject.GetComponent<AudioSource>();
        liveViewAudioSource.playOnAwake = false;
        GoogleMapsStyleARNavigation liveViewNavigation = liveViewNavigationObject.AddComponent<GoogleMapsStyleARNavigation>();
        ConfigureGoogleMapsStyleNavigation(
            liveViewNavigation,
            arSession,
            xrOrigin,
            arCamera,
            anchorManager,
            planeManager,
            raycastManager,
            liveViewAudioSource,
            arrowMaterial,
            accentMaterial,
            definitions);

        GameObject guideRoot = new GameObject("ARGuideRoot");
        GameObject arrowAnchorObject = new GameObject("ArrowAnchor");
        arrowAnchorObject.transform.SetParent(guideRoot.transform, false);

        GameObject arrowVisualObject = new GameObject("ArrowVisual");
        arrowVisualObject.transform.SetParent(arrowAnchorObject.transform, false);
        ArrowController arrowController = arrowVisualObject.AddComponent<ArrowController>();
        BuildArrowModel(arrowVisualObject.transform, arrowMaterial, primaryMaterial, accentMaterial);

        BillboardLabel floatingLabel = CreateFloatingLabel(arrowAnchorObject.transform);

        Transform destinationBeacon = CreateDestinationBeacon(guideRoot.transform, accentMaterial, primaryMaterial);
        List<Transform> breadcrumbDots = CreateBreadcrumbDots(guideRoot.transform, accentMaterial);

        GameObject navigationManagerObject = new GameObject("TourManager", typeof(AudioSource));
        AudioSource narrationSource = navigationManagerObject.GetComponent<AudioSource>();
        narrationSource.playOnAwake = false;
        narrationSource.loop = false;
        TourManager navigationManager = navigationManagerObject.AddComponent<TourManager>();

        GameObject photoGalleryControllerObject = new GameObject("PhotoGalleryController");
        PhotoGalleryController photoGalleryController = photoGalleryControllerObject.AddComponent<PhotoGalleryController>();

        GameObject galleryPickerObject = new GameObject("GalleryPicker");
        GalleryPicker galleryPicker = galleryPickerObject.AddComponent<GalleryPicker>();

        GameObject scanAudioManagerObject = new GameObject("ScanAudioManager", typeof(AudioSource));
        AudioManager scanAudioManager = scanAudioManagerObject.AddComponent<AudioManager>();

        GameObject imageRecognitionManagerObject = new GameObject("ImageTrackingHandler");
        ImageTrackingHandler imageRecognitionManager = imageRecognitionManagerObject.AddComponent<ImageTrackingHandler>();

        GameObject scanUIControllerObject = new GameObject("UIController");
        UIController scanUIController = scanUIControllerObject.AddComponent<UIController>();

        Canvas canvas = CreateCanvas("TourCanvas", RenderMode.ScreenSpaceOverlay);
        canvas.sortingOrder = 10;
        canvas.gameObject.AddComponent<PremiumTourUIStyler>();
        Sprite uiSprite = GetDefaultUISprite();
        Sprite circleSprite = GetCircleUISprite(uiSprite);
        Font font = GetDefaultFont();

        RectTransform root = CreatePanel("UIRoot", canvas.transform, uiSprite, new Color(0f, 0f, 0f, 0f));
        StretchToParent(root);

        RectTransform topPanel = CreatePanel("TopPanel", root, uiSprite, new Color(0.03f, 0.04f, 0.06f, 0.84f));
        topPanel.anchorMin = new Vector2(0f, 1f);
        topPanel.anchorMax = new Vector2(1f, 1f);
        topPanel.pivot = new Vector2(0.5f, 1f);
        topPanel.sizeDelta = new Vector2(0f, 120f);
        topPanel.anchoredPosition = Vector2.zero;

        RectTransform titleText = CreateText("TitleText", topPanel, font, "Digital Heritage Archive tour", 30, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
        titleText.anchorMin = new Vector2(0f, 1f);
        titleText.anchorMax = new Vector2(1f, 1f);
        titleText.pivot = new Vector2(0f, 1f);
        titleText.sizeDelta = new Vector2(-640f, 40f);
        titleText.anchoredPosition = new Vector2(28f, -16f);

        RectTransform statusText = CreateText("StatusText", topPanel, font, "Proceed to next location.", 22, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.86f, 0.9f, 0.96f));
        statusText.anchorMin = new Vector2(0f, 1f);
        statusText.anchorMax = new Vector2(1f, 1f);
        statusText.pivot = new Vector2(0f, 1f);
        statusText.sizeDelta = new Vector2(-640f, 34f);
        statusText.anchoredPosition = new Vector2(28f, -62f);

        Button scanItemButton = CreateButton("ScanItemButton", topPanel, uiSprite, font, "Scan Item", new Color(0.17f, 0.58f, 0.49f));
        RectTransform scanItemButtonRect = scanItemButton.GetComponent<RectTransform>();
        scanItemButtonRect.anchorMin = new Vector2(1f, 0.5f);
        scanItemButtonRect.anchorMax = new Vector2(1f, 0.5f);
        scanItemButtonRect.pivot = new Vector2(1f, 0.5f);
        scanItemButtonRect.sizeDelta = new Vector2(190f, 56f);
        scanItemButtonRect.anchoredPosition = new Vector2(-226f, -2f);

        Button galleryButton = CreateButton("GalleryButton", topPanel, uiSprite, font, "Gallery", new Color(0.96f, 0.68f, 0.26f));
        RectTransform galleryButtonRect = galleryButton.GetComponent<RectTransform>();
        galleryButtonRect.anchorMin = new Vector2(1f, 0.5f);
        galleryButtonRect.anchorMax = new Vector2(1f, 0.5f);
        galleryButtonRect.pivot = new Vector2(1f, 0.5f);
        galleryButtonRect.sizeDelta = new Vector2(190f, 56f);
        galleryButtonRect.anchoredPosition = new Vector2(-24f, -2f);
        SetButtonLabelColor(galleryButton, new Color(0.08f, 0.11f, 0.16f));

        Button qrShortcutButton = CreateFloatingQrShortcutButton(root, circleSprite, font);

        Button muteToggleButton = CreateCircleIconButton("MuteToggleButton", topPanel, circleSprite, new Color(1f, 1f, 1f, 0.18f), QuickMuteButtonPosition);
        Image muteToggleBackground = muteToggleButton.GetComponent<Image>();
        GameObject muteSlash = AddMuteIcon(muteToggleButton.transform, uiSprite, circleSprite);

        Button cameraToggleButton = CreateCircleIconButton("CameraToggleButton", topPanel, circleSprite, new Color(1f, 1f, 1f, 0.18f), QuickCameraButtonPosition);
        Image cameraToggleBackground = cameraToggleButton.GetComponent<Image>();
        GameObject cameraSlash = AddCameraIcon(cameraToggleButton.transform, uiSprite, circleSprite);

        RectTransform centerBadge = CreatePanel("CenterBadge", root, uiSprite, new Color(0.05f, 0.06f, 0.08f, 0.86f));
        SetAnchoredRect(centerBadge, new Vector2(0.5f, 0.58f), new Vector2(460f, 78f), Vector2.zero);
        RectTransform centerBadgeText = CreateText("CenterBadgeText", centerBadge, font, "Go to Main Hall", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        StretchToParent(centerBadgeText);

        RectTransform bottomPanel = CreatePanel("BottomPanel", root, uiSprite, new Color(0.98f, 0.98f, 0.97f, 0.98f));
        bottomPanel.anchorMin = new Vector2(0f, 0f);
        bottomPanel.anchorMax = new Vector2(1f, 0f);
        bottomPanel.pivot = new Vector2(0.5f, 0f);
        bottomPanel.sizeDelta = new Vector2(0f, 690f);
        bottomPanel.anchoredPosition = Vector2.zero;

        RectTransform instructionHeader = CreateText("InstructionHeader", bottomPanel, font, "Direction", 22, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.28f, 0.34f, 0.42f));
        instructionHeader.anchorMin = new Vector2(0f, 1f);
        instructionHeader.anchorMax = new Vector2(1f, 1f);
        instructionHeader.pivot = new Vector2(0f, 1f);
        instructionHeader.sizeDelta = new Vector2(-48f, 30f);
        instructionHeader.anchoredPosition = new Vector2(24f, -20f);

        RectTransform instructionText = CreateText("InstructionText", bottomPanel, font, "Go Straight", 32, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.08f, 0.11f, 0.16f));
        instructionText.anchorMin = new Vector2(0f, 1f);
        instructionText.anchorMax = new Vector2(1f, 1f);
        instructionText.pivot = new Vector2(0f, 1f);
        instructionText.sizeDelta = new Vector2(-280f, 56f);
        instructionText.anchoredPosition = new Vector2(24f, -58f);

        RectTransform progressText = CreateText("ProgressText", bottomPanel, font, "STOP 1 / 9", 20, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.15f, 0.47f, 0.9f));
        progressText.anchorMin = new Vector2(0f, 1f);
        progressText.anchorMax = new Vector2(1f, 1f);
        progressText.pivot = new Vector2(0f, 1f);
        progressText.sizeDelta = new Vector2(200f, 28f);
        progressText.anchoredPosition = new Vector2(24f, -110f);

        RectTransform distanceBadge = CreatePanel("DistanceBadge", bottomPanel, uiSprite, new Color(0.16f, 0.46f, 0.88f, 1f));
        distanceBadge.anchorMin = new Vector2(1f, 1f);
        distanceBadge.anchorMax = new Vector2(1f, 1f);
        distanceBadge.pivot = new Vector2(1f, 1f);
        distanceBadge.sizeDelta = new Vector2(226f, 60f);
        distanceBadge.anchoredPosition = new Vector2(-24f, -28f);

        RectTransform distanceText = CreateText("DistanceText", distanceBadge, font, "0.0 m away", 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        StretchToParent(distanceText);

        RectTransform descriptionText = CreateText(
            "DescriptionText",
            bottomPanel,
            font,
            "Start at the Balcony. The AR arrow and mini-map will guide you through each heritage stop.",
            18,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            new Color(0.36f, 0.41f, 0.47f));
        descriptionText.anchorMin = new Vector2(0f, 1f);
        descriptionText.anchorMax = new Vector2(1f, 1f);
        descriptionText.pivot = new Vector2(0f, 1f);
        descriptionText.sizeDelta = new Vector2(-48f, 62f);
        descriptionText.anchoredPosition = new Vector2(24f, -144f);

        RectTransform mapFrame = CreatePanel("MapFrame", bottomPanel, uiSprite, new Color(0.96f, 0.97f, 0.92f, 0.97f));
        mapFrame.anchorMin = new Vector2(0.5f, 0f);
        mapFrame.anchorMax = new Vector2(0.5f, 0f);
        mapFrame.pivot = new Vector2(0.5f, 0f);
        mapFrame.sizeDelta = new Vector2(968f, 360f);
        mapFrame.anchoredPosition = new Vector2(0f, 164f);

        Shadow mapShadow = mapFrame.gameObject.AddComponent<Shadow>();
        mapShadow.effectColor = new Color(0f, 0f, 0f, 0.26f);
        mapShadow.effectDistance = new Vector2(0f, -10f);

        RectTransform mapTitle = CreateText("MapTitle", mapFrame, font, "Mini Map", 18, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.17f, 0.2f, 0.24f, 0.96f));
        mapTitle.anchorMin = new Vector2(0f, 1f);
        mapTitle.anchorMax = new Vector2(1f, 1f);
        mapTitle.pivot = new Vector2(0f, 1f);
        mapTitle.sizeDelta = new Vector2(-40f, 28f);
        mapTitle.anchoredPosition = new Vector2(16f, -12f);

        RectTransform mapViewport = CreatePanel("MapViewport", mapFrame, uiSprite, new Color(0.70f, 0.86f, 0.66f, 1f));
        mapViewport.anchorMin = new Vector2(0.5f, 0.5f);
        mapViewport.anchorMax = new Vector2(0.5f, 0.5f);
        mapViewport.pivot = new Vector2(0.5f, 0.5f);
        mapViewport.sizeDelta = new Vector2(920f, 288f);
        mapViewport.anchoredPosition = new Vector2(0f, -14f);
        mapViewport.gameObject.AddComponent<RectMask2D>();

        RectTransform mapArea = CreatePanel("MapArea", mapViewport, uiSprite, Color.white);
        mapArea.anchorMin = new Vector2(0.5f, 0.5f);
        mapArea.anchorMax = new Vector2(0.5f, 0.5f);
        mapArea.pivot = new Vector2(0.5f, 0.5f);
        mapArea.sizeDelta = new Vector2(884f, 254f);
        mapArea.anchoredPosition = Vector2.zero;

        Image mapImage = mapArea.GetComponent<Image>();
        mapImage.sprite = mapSprite;
        mapImage.type = Image.Type.Simple;

        RectTransform pathLine = CreatePanel("PathLine", mapArea, uiSprite, new Color(1f, 1f, 1f, 0.96f));
        pathLine.sizeDelta = new Vector2(160f, 12f);
        pathLine.anchoredPosition = Vector2.zero;

        RectTransform pathLineFill = CreatePanel("Fill", pathLine, uiSprite, new Color(0f, 0.57f, 0.68f, 1f));
        pathLineFill.anchorMin = Vector2.zero;
        pathLineFill.anchorMax = Vector2.one;
        pathLineFill.offsetMin = new Vector2(2f, 2f);
        pathLineFill.offsetMax = new Vector2(-2f, -2f);

        List<RectTransform> mapPathSegments = new List<RectTransform>();
        for (int i = 0; i < 8; i++)
        {
            RectTransform segment = CreatePanel("PathSegment_" + (i + 1), mapArea, uiSprite, new Color(1f, 1f, 1f, 0.96f));
            segment.sizeDelta = new Vector2(120f, 14f);
            segment.anchoredPosition = Vector2.zero;

            RectTransform segmentFill = CreatePanel("Fill", segment, uiSprite, new Color(0f, 0.57f, 0.68f, 1f));
            segmentFill.anchorMin = Vector2.zero;
            segmentFill.anchorMax = Vector2.one;
            segmentFill.offsetMin = new Vector2(2f, 2f);
            segmentFill.offsetMax = new Vector2(-2f, -2f);

            mapPathSegments.Add(segment);
        }

        RectTransform currentDot = CreatePanel("CurrentDot", mapViewport, uiSprite, Color.white);
        currentDot.sizeDelta = new Vector2(30f, 30f);
        currentDot.anchoredPosition = Vector2.zero;
        RectTransform currentDotInner = CreatePanel("Inner", currentDot, uiSprite, new Color(0f, 0.57f, 0.68f, 1f));
        SetAnchoredRect(currentDotInner, new Vector2(0.5f, 0.5f), new Vector2(16f, 16f), Vector2.zero);

        RectTransform targetDot = CreatePanel("TargetDot", mapArea, uiSprite, Color.white);
        targetDot.sizeDelta = new Vector2(34f, 34f);
        targetDot.anchoredPosition = new Vector2(60f, 0f);
        RectTransform targetDotInner = CreatePanel("Inner", targetDot, uiSprite, new Color(0.94f, 0.58f, 0.2f, 1f));
        SetAnchoredRect(targetDotInner, new Vector2(0.5f, 0.5f), new Vector2(18f, 18f), Vector2.zero);

        MiniMapController miniMapController = mapArea.gameObject.AddComponent<MiniMapController>();
        miniMapController.ConfigureReferences(
            mapViewport,
            mapArea,
            currentDot,
            targetDot,
            pathLine,
            mapPathSegments,
            RouteMapMin,
            RouteMapMax);

        Button backButton = CreateButton("BackButton", bottomPanel, uiSprite, font, "Back", new Color(0.86f, 0.89f, 0.92f));
        SetAnchoredRect(backButton.GetComponent<RectTransform>(), new Vector2(0.2f, 0f), new Vector2(220f, 62f), new Vector2(0f, 44f));
        SetButtonLabelColor(backButton, new Color(0.08f, 0.11f, 0.16f));

        Button nextButton = CreateButton("NextButton", bottomPanel, uiSprite, font, "Next", new Color(0.16f, 0.46f, 0.88f));
        SetAnchoredRect(nextButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(220f, 62f), new Vector2(0f, 44f));

        Button exitButton = CreateButton("ExitButton", bottomPanel, uiSprite, font, "Exit", new Color(0.1f, 0.12f, 0.15f));
        SetAnchoredRect(exitButton.GetComponent<RectTransform>(), new Vector2(0.8f, 0f), new Vector2(220f, 62f), new Vector2(0f, 44f));

        RectTransform galleryOverlay = CreatePanel("GalleryOverlay", root, uiSprite, new Color(0.03f, 0.04f, 0.06f, 0.94f));
        StretchToParent(galleryOverlay);

        RectTransform galleryCard = CreatePanel("GalleryCard", galleryOverlay, uiSprite, new Color(0.97f, 0.98f, 0.97f, 1f));
        galleryCard.anchorMin = new Vector2(0.5f, 0.5f);
        galleryCard.anchorMax = new Vector2(0.5f, 0.5f);
        galleryCard.pivot = new Vector2(0.5f, 0.5f);
        galleryCard.sizeDelta = new Vector2(1000f, 1700f);
        galleryCard.anchoredPosition = Vector2.zero;

        RectTransform galleryHeader = CreateText("GalleryHeader", galleryCard, font, "Aguinaldo Shrine Photo Archive", 30, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.08f, 0.11f, 0.16f));
        galleryHeader.anchorMin = new Vector2(0f, 1f);
        galleryHeader.anchorMax = new Vector2(1f, 1f);
        galleryHeader.pivot = new Vector2(0f, 1f);
        galleryHeader.sizeDelta = new Vector2(-240f, 40f);
        galleryHeader.anchoredPosition = new Vector2(28f, -24f);

        Button closeGalleryButton = CreateButton("CloseGalleryButton", galleryCard, uiSprite, font, "Close", new Color(0.16f, 0.2f, 0.25f));
        RectTransform closeGalleryButtonRect = closeGalleryButton.GetComponent<RectTransform>();
        closeGalleryButtonRect.anchorMin = new Vector2(1f, 1f);
        closeGalleryButtonRect.anchorMax = new Vector2(1f, 1f);
        closeGalleryButtonRect.pivot = new Vector2(1f, 1f);
        closeGalleryButtonRect.sizeDelta = new Vector2(180f, 56f);
        closeGalleryButtonRect.anchoredPosition = new Vector2(-24f, -18f);

        RectTransform galleryCounterText = CreateText("GalleryCounterText", galleryCard, font, "0 / 0", 20, FontStyle.Bold, TextAnchor.UpperRight, new Color(0.16f, 0.46f, 0.88f));
        galleryCounterText.anchorMin = new Vector2(0f, 1f);
        galleryCounterText.anchorMax = new Vector2(1f, 1f);
        galleryCounterText.pivot = new Vector2(1f, 1f);
        galleryCounterText.sizeDelta = new Vector2(-240f, 28f);
        galleryCounterText.anchoredPosition = new Vector2(-24f, -86f);

        RectTransform previewFrame = CreatePanel("PreviewFrame", galleryCard, uiSprite, new Color(0.9f, 0.93f, 0.95f, 1f));
        previewFrame.anchorMin = new Vector2(0.5f, 1f);
        previewFrame.anchorMax = new Vector2(0.5f, 1f);
        previewFrame.pivot = new Vector2(0.5f, 1f);
        previewFrame.sizeDelta = new Vector2(944f, 620f);
        previewFrame.anchoredPosition = new Vector2(0f, -128f);

        GameObject previewImageObject = new GameObject("PreviewImage", typeof(RectTransform), typeof(Image));
        previewImageObject.transform.SetParent(previewFrame, false);
        RectTransform previewImageRect = previewImageObject.GetComponent<RectTransform>();
        previewImageRect.anchorMin = Vector2.zero;
        previewImageRect.anchorMax = Vector2.one;
        previewImageRect.offsetMin = new Vector2(18f, 18f);
        previewImageRect.offsetMax = new Vector2(-18f, -18f);
        Image previewImage = previewImageObject.GetComponent<Image>();
        previewImage.color = Color.white;
        previewImage.preserveAspect = true;

        RectTransform galleryPhotoTitleText = CreateText("GalleryPhotoTitleText", galleryCard, font, "Archive Photo", 28, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.08f, 0.11f, 0.16f));
        galleryPhotoTitleText.anchorMin = new Vector2(0f, 1f);
        galleryPhotoTitleText.anchorMax = new Vector2(1f, 1f);
        galleryPhotoTitleText.pivot = new Vector2(0f, 1f);
        galleryPhotoTitleText.sizeDelta = new Vector2(-56f, 36f);
        galleryPhotoTitleText.anchoredPosition = new Vector2(28f, -776f);

        RectTransform galleryDefinitionText = CreateText(
            "GalleryDefinitionText",
            galleryCard,
            font,
            "Select any archive image below to read its in-app definition.",
            19,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            new Color(0.34f, 0.39f, 0.46f));
        galleryDefinitionText.anchorMin = new Vector2(0f, 1f);
        galleryDefinitionText.anchorMax = new Vector2(1f, 1f);
        galleryDefinitionText.pivot = new Vector2(0f, 1f);
        galleryDefinitionText.sizeDelta = new Vector2(-56f, 120f);
        galleryDefinitionText.anchoredPosition = new Vector2(28f, -824f);

        Button previousPhotoButton = CreateButton("PreviousPhotoButton", galleryCard, uiSprite, font, "Previous", new Color(0.86f, 0.89f, 0.92f));
        SetAnchoredRect(previousPhotoButton.GetComponent<RectTransform>(), new Vector2(0.33f, 1f), new Vector2(220f, 58f), new Vector2(0f, -948f));
        SetButtonLabelColor(previousPhotoButton, new Color(0.08f, 0.11f, 0.16f));

        Button nextPhotoButton = CreateButton("NextPhotoButton", galleryCard, uiSprite, font, "Next", new Color(0.16f, 0.46f, 0.88f));
        SetAnchoredRect(nextPhotoButton.GetComponent<RectTransform>(), new Vector2(0.67f, 1f), new Vector2(220f, 58f), new Vector2(0f, -948f));

        RectTransform listHeader = CreateText("ListHeader", galleryCard, font, "All Archive Photos", 22, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.08f, 0.11f, 0.16f));
        listHeader.anchorMin = new Vector2(0f, 1f);
        listHeader.anchorMax = new Vector2(1f, 1f);
        listHeader.pivot = new Vector2(0f, 1f);
        listHeader.sizeDelta = new Vector2(-56f, 30f);
        listHeader.anchoredPosition = new Vector2(28f, -1036f);

        RectTransform listFrame = CreatePanel("ListFrame", galleryCard, uiSprite, new Color(0.92f, 0.95f, 0.98f, 1f));
        listFrame.anchorMin = new Vector2(0.5f, 0f);
        listFrame.anchorMax = new Vector2(0.5f, 0f);
        listFrame.pivot = new Vector2(0.5f, 0f);
        listFrame.sizeDelta = new Vector2(944f, 610f);
        listFrame.anchoredPosition = new Vector2(0f, 28f);

        GameObject photoScrollViewObject = new GameObject("PhotoScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        photoScrollViewObject.transform.SetParent(listFrame, false);
        RectTransform photoScrollViewRect = photoScrollViewObject.GetComponent<RectTransform>();
        photoScrollViewRect.anchorMin = Vector2.zero;
        photoScrollViewRect.anchorMax = Vector2.one;
        photoScrollViewRect.offsetMin = new Vector2(14f, 14f);
        photoScrollViewRect.offsetMax = new Vector2(-14f, -14f);
        Image photoScrollViewImage = photoScrollViewObject.GetComponent<Image>();
        photoScrollViewImage.color = new Color(1f, 1f, 1f, 0f);
        ScrollRect galleryListScrollRect = photoScrollViewObject.GetComponent<ScrollRect>();
        galleryListScrollRect.horizontal = false;
        galleryListScrollRect.movementType = ScrollRect.MovementType.Clamped;
        galleryListScrollRect.scrollSensitivity = 28f;

        GameObject photoViewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        photoViewportObject.transform.SetParent(photoScrollViewObject.transform, false);
        RectTransform photoViewportRect = photoViewportObject.GetComponent<RectTransform>();
        photoViewportRect.anchorMin = Vector2.zero;
        photoViewportRect.anchorMax = Vector2.one;
        photoViewportRect.offsetMin = Vector2.zero;
        photoViewportRect.offsetMax = Vector2.zero;
        Image photoViewportImage = photoViewportObject.GetComponent<Image>();
        photoViewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        Mask photoViewportMask = photoViewportObject.GetComponent<Mask>();
        photoViewportMask.showMaskGraphic = false;

        GameObject photoContentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        photoContentObject.transform.SetParent(photoViewportObject.transform, false);
        RectTransform photoContentRect = photoContentObject.GetComponent<RectTransform>();
        photoContentRect.anchorMin = new Vector2(0f, 1f);
        photoContentRect.anchorMax = new Vector2(1f, 1f);
        photoContentRect.pivot = new Vector2(0.5f, 1f);
        photoContentRect.anchoredPosition = Vector2.zero;
        photoContentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup photoListLayout = photoContentObject.GetComponent<VerticalLayoutGroup>();
        photoListLayout.childAlignment = TextAnchor.UpperCenter;
        photoListLayout.childControlWidth = true;
        photoListLayout.childControlHeight = true;
        photoListLayout.childForceExpandWidth = true;
        photoListLayout.childForceExpandHeight = false;
        photoListLayout.spacing = 10f;
        photoListLayout.padding = new RectOffset(0, 0, 0, 10);

        ContentSizeFitter photoContentSizeFitter = photoContentObject.GetComponent<ContentSizeFitter>();
        photoContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        photoContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        galleryListScrollRect.viewport = photoViewportRect;
        galleryListScrollRect.content = photoContentRect;

        galleryOverlay.gameObject.SetActive(false);

        RectTransform scanChoiceOverlay = CreatePanel("ScanChoiceOverlay", root, uiSprite, new Color(0.03f, 0.04f, 0.06f, 0.76f));
        StretchToParent(scanChoiceOverlay);

        RectTransform scanChoiceCard = CreatePanel("ScanChoiceCard", scanChoiceOverlay, uiSprite, new Color(0.98f, 0.98f, 0.97f, 1f));
        scanChoiceCard.anchorMin = new Vector2(0.5f, 0.5f);
        scanChoiceCard.anchorMax = new Vector2(0.5f, 0.5f);
        scanChoiceCard.pivot = new Vector2(0.5f, 0.5f);
        scanChoiceCard.sizeDelta = new Vector2(860f, 600f);
        scanChoiceCard.anchoredPosition = Vector2.zero;

        RectTransform scanChoiceHeader = CreateText("ScanChoiceHeader", scanChoiceCard, font, "AR Scan Info System", 34, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.08f, 0.11f, 0.16f));
        scanChoiceHeader.anchorMin = new Vector2(0f, 1f);
        scanChoiceHeader.anchorMax = new Vector2(1f, 1f);
        scanChoiceHeader.pivot = new Vector2(0f, 1f);
        scanChoiceHeader.sizeDelta = new Vector2(-56f, 42f);
        scanChoiceHeader.anchoredPosition = new Vector2(28f, -26f);

        RectTransform scanChoiceBody = CreateText(
            "ScanChoiceBody",
            scanChoiceCard,
            font,
            "Use QR Scan for the most accurate item lookup, resume the live camera scan, or pick an image from the phone gallery.",
            22,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            new Color(0.34f, 0.39f, 0.46f));
        scanChoiceBody.anchorMin = new Vector2(0f, 1f);
        scanChoiceBody.anchorMax = new Vector2(1f, 1f);
        scanChoiceBody.pivot = new Vector2(0f, 1f);
        scanChoiceBody.sizeDelta = new Vector2(-56f, 110f);
        scanChoiceBody.anchoredPosition = new Vector2(28f, -88f);

        Button qrScanButton = CreateButton("QrScanButton", scanChoiceCard, uiSprite, font, "QR Scan", new Color(0.16f, 0.46f, 0.88f));
        SetAnchoredRect(qrScanButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(620f, 78f), new Vector2(0f, 60f));

        Button useCameraButton = CreateButton("UseCameraButton", scanChoiceCard, uiSprite, font, "Resume Auto Scan", new Color(0.17f, 0.58f, 0.49f));
        SetAnchoredRect(useCameraButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(620f, 78f), new Vector2(0f, -40f));

        Button pickGalleryButton = CreateButton("PickGalleryButton", scanChoiceCard, uiSprite, font, "Pick From Gallery", new Color(0.58f, 0.36f, 0.78f));
        SetAnchoredRect(pickGalleryButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(620f, 78f), new Vector2(0f, -140f));

        Button cancelScanButton = CreateButton("CancelScanButton", scanChoiceCard, uiSprite, font, "Cancel", new Color(0.18f, 0.21f, 0.25f));
        SetAnchoredRect(cancelScanButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(320f, 68f), new Vector2(0f, 34f));

        RectTransform cameraHintPanel = CreatePanel("CameraHintPanel", root, uiSprite, new Color(0.05f, 0.08f, 0.12f, 0.88f));
        cameraHintPanel.anchorMin = new Vector2(0.5f, 1f);
        cameraHintPanel.anchorMax = new Vector2(0.5f, 1f);
        cameraHintPanel.pivot = new Vector2(0.5f, 1f);
        cameraHintPanel.sizeDelta = new Vector2(760f, 72f);
        cameraHintPanel.anchoredPosition = new Vector2(0f, -132f);

        RectTransform cameraHintText = CreateText("CameraHintText", cameraHintPanel, font, "Smart scan is active. Point the camera at a registered shrine image.", 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        StretchToParent(cameraHintText);

        QrScanUi qrScanUi = CreateQrScanOverlay(root, uiSprite, font);

        RectTransform scanResultOverlay = CreatePanel("ScanResultOverlay", root, uiSprite, new Color(0.03f, 0.04f, 0.06f, 0.78f));
        StretchToParent(scanResultOverlay);

        RectTransform scanResultCard = CreatePanel("ScanResultCard", scanResultOverlay, uiSprite, new Color(0.98f, 0.98f, 0.97f, 1f));
        scanResultCard.anchorMin = new Vector2(0.5f, 0.5f);
        scanResultCard.anchorMax = new Vector2(0.5f, 0.5f);
        scanResultCard.pivot = new Vector2(0.5f, 0.5f);
        scanResultCard.sizeDelta = new Vector2(920f, 900f);
        scanResultCard.anchoredPosition = new Vector2(0f, 0f);

        RectTransform scanResultHeader = CreateText("ScanResultHeader", scanResultCard, font, "Scanned Heritage Item", 32, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.08f, 0.11f, 0.16f));
        scanResultHeader.anchorMin = new Vector2(0f, 1f);
        scanResultHeader.anchorMax = new Vector2(1f, 1f);
        scanResultHeader.pivot = new Vector2(0f, 1f);
        scanResultHeader.sizeDelta = new Vector2(-56f, 40f);
        scanResultHeader.anchoredPosition = new Vector2(28f, -24f);

        RectTransform scanResultStatusText = CreateText("ScanResultStatusText", scanResultCard, font, "Waiting for an auto-detected match", 18, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.16f, 0.46f, 0.88f));
        scanResultStatusText.anchorMin = new Vector2(0f, 1f);
        scanResultStatusText.anchorMax = new Vector2(1f, 1f);
        scanResultStatusText.pivot = new Vector2(0f, 1f);
        scanResultStatusText.sizeDelta = new Vector2(-56f, 28f);
        scanResultStatusText.anchoredPosition = new Vector2(28f, -74f);

        RectTransform scanPreviewFrame = CreatePanel("ScanPreviewFrame", scanResultCard, uiSprite, new Color(0.9f, 0.93f, 0.95f, 1f));
        scanPreviewFrame.anchorMin = new Vector2(0.5f, 1f);
        scanPreviewFrame.anchorMax = new Vector2(0.5f, 1f);
        scanPreviewFrame.pivot = new Vector2(0.5f, 1f);
        scanPreviewFrame.sizeDelta = new Vector2(864f, 360f);
        scanPreviewFrame.anchoredPosition = new Vector2(0f, -118f);

        GameObject scanPreviewImageObject = new GameObject("ScanPreviewImage", typeof(RectTransform), typeof(Image));
        scanPreviewImageObject.transform.SetParent(scanPreviewFrame, false);
        RectTransform scanPreviewImageRect = scanPreviewImageObject.GetComponent<RectTransform>();
        scanPreviewImageRect.anchorMin = Vector2.zero;
        scanPreviewImageRect.anchorMax = Vector2.one;
        scanPreviewImageRect.offsetMin = new Vector2(18f, 18f);
        scanPreviewImageRect.offsetMax = new Vector2(-18f, -18f);
        Image scanPreviewImage = scanPreviewImageObject.GetComponent<Image>();
        scanPreviewImage.color = Color.white;
        scanPreviewImage.preserveAspect = true;

        RectTransform scanResultTitleText = CreateText("ScanResultTitleText", scanResultCard, font, "No item matched yet", 30, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.08f, 0.11f, 0.16f));
        scanResultTitleText.anchorMin = new Vector2(0f, 1f);
        scanResultTitleText.anchorMax = new Vector2(1f, 1f);
        scanResultTitleText.pivot = new Vector2(0f, 1f);
        scanResultTitleText.sizeDelta = new Vector2(-56f, 38f);
        scanResultTitleText.anchoredPosition = new Vector2(28f, -504f);

        RectTransform scanResultDescriptionText = CreateText(
            "ScanResultDescriptionText",
            scanResultCard,
            font,
            "Scan with the live camera or choose an image from the gallery to see a short description and play narration.",
            20,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            new Color(0.34f, 0.39f, 0.46f));
        scanResultDescriptionText.anchorMin = new Vector2(0f, 1f);
        scanResultDescriptionText.anchorMax = new Vector2(1f, 1f);
        scanResultDescriptionText.pivot = new Vector2(0f, 1f);
        scanResultDescriptionText.sizeDelta = new Vector2(-56f, 176f);
        scanResultDescriptionText.anchoredPosition = new Vector2(28f, -560f);

        Button playPauseScanAudioButton = CreateButton("PlayPauseScanAudioButton", scanResultCard, uiSprite, font, "Replay / Pause", new Color(0.16f, 0.46f, 0.88f));
        SetAnchoredRect(playPauseScanAudioButton.GetComponent<RectTransform>(), new Vector2(0.32f, 0f), new Vector2(250f, 72f), new Vector2(0f, 42f));

        Button closeScanResultButton = CreateButton("CloseScanResultButton", scanResultCard, uiSprite, font, "Close", new Color(0.12f, 0.14f, 0.18f));
        SetAnchoredRect(closeScanResultButton.GetComponent<RectTransform>(), new Vector2(0.68f, 0f), new Vector2(250f, 72f), new Vector2(0f, 42f));

        scanChoiceOverlay.gameObject.SetActive(false);
        qrScanUi.Overlay.SetActive(false);
        cameraHintPanel.gameObject.SetActive(false);
        scanResultOverlay.gameObject.SetActive(false);

        ConfigureFloatingLabel(floatingLabel);
        ConfigureNavigationManager(
            navigationManager,
            arSession,
            xrOrigin,
            arCamera,
            arrowAnchorObject.transform,
            arrowController,
            floatingLabel,
            narrationSource,
            destinationBeacon,
            breadcrumbDots,
            titleText.GetComponent<Text>(),
            instructionText.GetComponent<Text>(),
            descriptionText.GetComponent<Text>(),
            progressText.GetComponent<Text>(),
            statusText.GetComponent<Text>(),
            distanceText.GetComponent<Text>(),
            centerBadgeText.GetComponent<Text>(),
            nextButton,
            backButton,
            exitButton,
            mapViewport,
            mapArea,
            miniMapController,
            currentDot,
            targetDot,
            pathLine,
            mapPathSegments,
            locations);
        ConfigurePhotoGalleryController(
            photoGalleryController,
            galleryButton,
            closeGalleryButton,
            previousPhotoButton,
            nextPhotoButton,
            galleryOverlay.gameObject,
            previewImage,
            galleryPhotoTitleText.GetComponent<Text>(),
            galleryDefinitionText.GetComponent<Text>(),
            galleryCounterText.GetComponent<Text>(),
            photoContentRect,
            galleryListScrollRect,
            galleryPhotos);
        ConfigureScanUIController(
            scanUIController,
            scanItemButton,
            qrScanButton,
            qrShortcutButton,
            useCameraButton,
            pickGalleryButton,
            cancelScanButton,
            qrScanUi.CloseButton,
            playPauseScanAudioButton,
            closeScanResultButton,
            scanChoiceOverlay.gameObject,
            qrScanUi.Overlay,
            cameraHintPanel.gameObject,
            scanResultOverlay.gameObject,
            qrScanUi.HintText,
            cameraHintText.GetComponent<Text>(),
            scanResultTitleText.GetComponent<Text>(),
            scanResultDescriptionText.GetComponent<Text>(),
            scanResultStatusText.GetComponent<Text>(),
            playPauseScanAudioButton.GetComponentInChildren<Text>(),
            scanPreviewImage);
        ConfigureImageRecognitionManager(
            imageRecognitionManager,
            xrOrigin,
            trackedImageManager,
            galleryPicker,
            scanUIController,
            scanAudioManager,
            scanItems);

        TourQuickControls quickControls = canvas.gameObject.AddComponent<TourQuickControls>();
        quickControls.Configure(
            cameraToggleButton,
            muteToggleButton,
            cameraToggleBackground,
            muteToggleBackground,
            cameraSlash,
            muteSlash,
            arCamera,
            arCamera.GetComponent<ARCameraManager>(),
            arCamera.GetComponent<ARCameraBackground>(),
            imageRecognitionManager,
            scanAudioManager,
            statusText.GetComponent<Text>());

        EditorSceneManager.SaveScene(scene, TourScenePath);
    }

    private static void ConfigureFloatingLabel(BillboardLabel floatingLabel)
    {
        TextMesh textMesh = floatingLabel.GetComponent<TextMesh>();
        MeshRenderer meshRenderer = floatingLabel.GetComponent<MeshRenderer>();

        SerializedObject serializedLabel = new SerializedObject(floatingLabel);
        serializedLabel.FindProperty("labelText").objectReferenceValue = textMesh;
        serializedLabel.FindProperty("labelRenderer").objectReferenceValue = meshRenderer;
        serializedLabel.ApplyModifiedPropertiesWithoutUndo();

        meshRenderer.enabled = false;
    }

    private static void ConfigureGoogleMapsStyleNavigation(
        GoogleMapsStyleARNavigation navigation,
        ARSession arSession,
        XROrigin xrOrigin,
        Camera arCamera,
        ARAnchorManager anchorManager,
        ARPlaneManager planeManager,
        ARRaycastManager raycastManager,
        AudioSource audioSource,
        Material arrowMaterial,
        Material accentMaterial,
        IReadOnlyList<TourLocationDefinition> definitions)
    {
        SerializedObject serializedNavigation = new SerializedObject(navigation);
        serializedNavigation.FindProperty("arSession").objectReferenceValue = arSession;
        serializedNavigation.FindProperty("xrOrigin").objectReferenceValue = xrOrigin;
        serializedNavigation.FindProperty("arCamera").objectReferenceValue = arCamera;
        serializedNavigation.FindProperty("anchorManager").objectReferenceValue = anchorManager;
        serializedNavigation.FindProperty("planeManager").objectReferenceValue = planeManager;
        serializedNavigation.FindProperty("raycastManager").objectReferenceValue = raycastManager;
        serializedNavigation.FindProperty("audioSource").objectReferenceValue = audioSource;
        serializedNavigation.FindProperty("arrowMaterial").objectReferenceValue = arrowMaterial;
        serializedNavigation.FindProperty("accentMaterial").objectReferenceValue = accentMaterial;
        serializedNavigation.FindProperty("arrivalThreshold").floatValue = 1.5f;
        serializedNavigation.FindProperty("arrowSpacing").floatValue = 1.25f;
        serializedNavigation.FindProperty("firstArrowDistance").floatValue = 1.1f;
        serializedNavigation.FindProperty("maxArrowCount").intValue = 16;
        serializedNavigation.FindProperty("arrowHeightOffset").floatValue = 1.05f;
        serializedNavigation.FindProperty("labelHeightOffset").floatValue = 1.55f;

        SerializedProperty waypoints = serializedNavigation.FindProperty("waypoints");
        waypoints.arraySize = definitions.Count;
        for (int i = 0; i < definitions.Count; i++)
        {
            SerializedProperty element = waypoints.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("locationName").stringValue = definitions[i].Name;
            element.FindPropertyRelative("localPosition").vector3Value = definitions[i].Position;
            element.FindPropertyRelative("reachDistance").floatValue = Mathf.Max(1.1f, definitions[i].ReachDistance);
            element.FindPropertyRelative("description").stringValue = definitions[i].Description;
        }

        serializedNavigation.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureNavigationManager(
        NavigationManager navigationManager,
        ARSession arSession,
        XROrigin xrOrigin,
        Camera arCamera,
        Transform arrowAnchor,
        ArrowController arrowController,
        BillboardLabel floatingLabel,
        AudioSource narrationSource,
        Transform destinationBeacon,
        IReadOnlyList<Transform> breadcrumbDots,
        Text titleText,
        Text instructionText,
        Text descriptionText,
        Text progressText,
        Text statusText,
        Text distanceText,
        Text centerBadgeText,
        Button nextButton,
        Button backButton,
        Button exitButton,
        RectTransform mapViewport,
        RectTransform mapArea,
        MiniMapController miniMapController,
        RectTransform currentDot,
        RectTransform targetDot,
        RectTransform pathLine,
        IReadOnlyList<RectTransform> mapPathSegments,
        IReadOnlyList<LocationTrigger> locations)
    {
        SerializedObject serializedManager = new SerializedObject(navigationManager);
        serializedManager.FindProperty("arSession").objectReferenceValue = arSession;
        serializedManager.FindProperty("xrOrigin").objectReferenceValue = xrOrigin;
        serializedManager.FindProperty("arCamera").objectReferenceValue = arCamera;
        serializedManager.FindProperty("arrowAnchor").objectReferenceValue = arrowAnchor;
        serializedManager.FindProperty("arrowController").objectReferenceValue = arrowController;
        serializedManager.FindProperty("floatingLabel").objectReferenceValue = floatingLabel;
        serializedManager.FindProperty("narrationSource").objectReferenceValue = narrationSource;
        serializedManager.FindProperty("destinationBeacon").objectReferenceValue = destinationBeacon;
        serializedManager.FindProperty("titleText").objectReferenceValue = titleText;
        serializedManager.FindProperty("instructionText").objectReferenceValue = instructionText;
        serializedManager.FindProperty("descriptionText").objectReferenceValue = descriptionText;
        serializedManager.FindProperty("progressText").objectReferenceValue = progressText;
        serializedManager.FindProperty("statusText").objectReferenceValue = statusText;
        serializedManager.FindProperty("distanceText").objectReferenceValue = distanceText;
        serializedManager.FindProperty("centerBadgeText").objectReferenceValue = centerBadgeText;
        serializedManager.FindProperty("nextButton").objectReferenceValue = nextButton;
        serializedManager.FindProperty("backButton").objectReferenceValue = backButton;
        serializedManager.FindProperty("exitButton").objectReferenceValue = exitButton;
        serializedManager.FindProperty("miniMapController").objectReferenceValue = miniMapController;
        serializedManager.FindProperty("mapViewport").objectReferenceValue = mapViewport;
        serializedManager.FindProperty("mapArea").objectReferenceValue = mapArea;
        serializedManager.FindProperty("currentDot").objectReferenceValue = currentDot;
        serializedManager.FindProperty("targetDot").objectReferenceValue = targetDot;
        serializedManager.FindProperty("pathLine").objectReferenceValue = pathLine;
        serializedManager.FindProperty("worldMin").vector2Value = RouteMapMin;
        serializedManager.FindProperty("worldMax").vector2Value = RouteMapMax;
        serializedManager.FindProperty("keepCurrentDotCentered").boolValue = false;
        serializedManager.FindProperty("miniMapFollowSmoothing").floatValue = 10f;
        serializedManager.FindProperty("autoAdvanceDistance").floatValue = 1.5f;
        serializedManager.FindProperty("autoAdvanceCooldown").floatValue = 0.75f;
        serializedManager.FindProperty("showCameraArrow").boolValue = false;
        serializedManager.FindProperty("showLegacyWorldGuide").boolValue = false;
        serializedManager.FindProperty("keepArrowCenteredOnScreen").boolValue = true;
        serializedManager.FindProperty("arrowViewportPosition").vector2Value = new Vector2(0.5f, 0.34f);
        serializedManager.FindProperty("homeSceneName").stringValue = "HomeScene";

        SerializedProperty breadcrumbArray = serializedManager.FindProperty("breadcrumbDots");
        breadcrumbArray.arraySize = breadcrumbDots.Count;
        for (int i = 0; i < breadcrumbDots.Count; i++)
        {
            breadcrumbArray.GetArrayElementAtIndex(i).objectReferenceValue = breadcrumbDots[i];
        }

        SerializedProperty mapSegments = serializedManager.FindProperty("mapPathSegments");
        mapSegments.arraySize = mapPathSegments.Count;
        for (int i = 0; i < mapPathSegments.Count; i++)
        {
            mapSegments.GetArrayElementAtIndex(i).objectReferenceValue = mapPathSegments[i];
        }

        SerializedProperty tourLocations = serializedManager.FindProperty("tourLocations");
        tourLocations.arraySize = locations.Count;
        for (int i = 0; i < locations.Count; i++)
        {
            tourLocations.GetArrayElementAtIndex(i).objectReferenceValue = locations[i];
        }

        serializedManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurePhotoGalleryController(
        PhotoGalleryController photoGalleryController,
        Button openButton,
        Button closeButton,
        Button previousButton,
        Button nextButton,
        GameObject overlayRoot,
        Image previewImage,
        Text photoTitleText,
        Text photoDefinitionText,
        Text counterText,
        RectTransform listContent,
        ScrollRect listScrollRect,
        IReadOnlyList<GalleryPhotoImportData> galleryPhotos)
    {
        SerializedObject serializedGallery = new SerializedObject(photoGalleryController);
        serializedGallery.FindProperty("overlayRoot").objectReferenceValue = overlayRoot;
        serializedGallery.FindProperty("openButton").objectReferenceValue = openButton;
        serializedGallery.FindProperty("closeButton").objectReferenceValue = closeButton;
        serializedGallery.FindProperty("previousButton").objectReferenceValue = previousButton;
        serializedGallery.FindProperty("nextButton").objectReferenceValue = nextButton;
        serializedGallery.FindProperty("previewImage").objectReferenceValue = previewImage;
        serializedGallery.FindProperty("photoTitleText").objectReferenceValue = photoTitleText;
        serializedGallery.FindProperty("photoDefinitionText").objectReferenceValue = photoDefinitionText;
        serializedGallery.FindProperty("counterText").objectReferenceValue = counterText;
        serializedGallery.FindProperty("listContent").objectReferenceValue = listContent;
        serializedGallery.FindProperty("listScrollRect").objectReferenceValue = listScrollRect;
        serializedGallery.FindProperty("previewStartIndex").intValue = 0;

        SerializedProperty photoArray = serializedGallery.FindProperty("photos");
        photoArray.arraySize = galleryPhotos.Count;
        for (int i = 0; i < galleryPhotos.Count; i++)
        {
            SerializedProperty element = photoArray.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("title").stringValue = galleryPhotos[i].Title;
            element.FindPropertyRelative("definition").stringValue = galleryPhotos[i].Definition;
            element.FindPropertyRelative("image").objectReferenceValue = galleryPhotos[i].Sprite;
        }

        serializedGallery.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Button EnsureQrScanButtonInScene(ScanUIController scanUIController)
    {
        if (scanUIController == null)
        {
            return null;
        }

        Transform scanChoiceCard = FindTransformInActiveScene("ScanChoiceCard");
        if (scanChoiceCard == null)
        {
            return null;
        }

        RectTransform cardRect = scanChoiceCard.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cardRect.sizeDelta = new Vector2(860f, 600f);
        }

        Sprite uiSprite = GetDefaultUISprite();
        Font font = GetDefaultFont();

        Transform qrButtonTransform = FindChildRecursive(scanChoiceCard, "QrScanButton");
        Button qrButton = qrButtonTransform != null
            ? qrButtonTransform.GetComponent<Button>()
            : null;

        if (qrButton == null)
        {
            qrButton = CreateButton("QrScanButton", scanChoiceCard, uiSprite, font, "QR Scan", new Color(0.16f, 0.46f, 0.88f));
        }

        SetAnchoredRect(qrButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(620f, 78f), new Vector2(0f, 60f));
        Text qrButtonText = qrButton.GetComponentInChildren<Text>();
        if (qrButtonText != null)
        {
            qrButtonText.text = "QR Scan";
        }

        RepositionScanChoiceButton(scanChoiceCard, "UseCameraButton", new Vector2(0f, -40f), new Color(0.17f, 0.58f, 0.49f));
        RepositionScanChoiceButton(scanChoiceCard, "PickGalleryButton", new Vector2(0f, -140f), new Color(0.58f, 0.36f, 0.78f));
        RepositionScanChoiceButton(scanChoiceCard, "CancelScanButton", new Vector2(0f, 34f), null);

        Transform bodyTransform = FindChildRecursive(scanChoiceCard, "ScanChoiceBody");
        Text bodyText = bodyTransform != null ? bodyTransform.GetComponent<Text>() : null;
        if (bodyText != null)
        {
            bodyText.text = "Use QR Scan for the most accurate item lookup, resume the live camera scan, or pick an image from the phone gallery.";
        }

        return qrButton;
    }

    private static Button EnsureFloatingQrShortcutButtonInScene(ScanUIController scanUIController)
    {
        if (scanUIController == null)
        {
            return null;
        }

        Transform root = FindTransformInActiveScene("UIRoot");
        if (root == null)
        {
            return null;
        }

        Sprite circleSprite = GetCircleUISprite(GetDefaultUISprite());
        Font font = GetDefaultFont();

        Transform existingButtonTransform = FindChildRecursive(root, "QrScanShortcutButton");
        Button button = existingButtonTransform != null
            ? existingButtonTransform.GetComponent<Button>()
            : null;

        if (button == null)
        {
            button = CreateFloatingQrShortcutButton(root, circleSprite, font);
        }
        else
        {
            ConfigureFloatingQrShortcutButton(button, font);
        }

        return button;
    }

    private static QrScanUi EnsureQrScanOverlayInScene(ScanUIController scanUIController)
    {
        QrScanUi qrScanUi = default;
        if (scanUIController == null)
        {
            return qrScanUi;
        }

        Transform root = FindTransformInActiveScene("UIRoot");
        if (root == null)
        {
            return qrScanUi;
        }

        Transform overlayTransform = FindChildRecursive(root, "QrScanOverlay");
        if (overlayTransform == null)
        {
            qrScanUi = CreateQrScanOverlay(root, GetDefaultUISprite(), GetDefaultFont());
            qrScanUi.Overlay.SetActive(false);
            return qrScanUi;
        }

        qrScanUi.Overlay = overlayTransform.gameObject;
        Transform closeTransform = FindChildRecursive(overlayTransform, "QrScanCloseButton");
        Transform hintTransform = FindChildRecursive(overlayTransform, "QrScanHintText");
        qrScanUi.CloseButton = closeTransform != null ? closeTransform.GetComponent<Button>() : null;
        qrScanUi.HintText = hintTransform != null ? hintTransform.GetComponent<Text>() : null;

        return qrScanUi;
    }

    private static QrScanUi CreateQrScanOverlay(Transform parent, Sprite uiSprite, Font font)
    {
        RectTransform overlay = CreatePanel("QrScanOverlay", parent, uiSprite, new Color(0.01f, 0.015f, 0.025f, 0.74f));
        StretchToParent(overlay);

        RectTransform headerPanel = CreatePanel("QrScanHeaderPanel", overlay, uiSprite, new Color(0.04f, 0.055f, 0.075f, 0.96f));
        headerPanel.anchorMin = new Vector2(0.06f, 1f);
        headerPanel.anchorMax = new Vector2(0.94f, 1f);
        headerPanel.pivot = new Vector2(0.5f, 1f);
        headerPanel.sizeDelta = new Vector2(0f, 132f);
        headerPanel.anchoredPosition = new Vector2(0f, -150f);

        RectTransform title = CreateText("QrScanTitleText", headerPanel, font, "QR Scanner", 36, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = new Vector2(1f, 1f);
        title.pivot = new Vector2(0f, 1f);
        title.sizeDelta = new Vector2(-280f, 46f);
        title.anchoredPosition = new Vector2(28f, -22f);

        RectTransform subtitle = CreateText("QrScanSubtitleText", headerPanel, font, "Point the camera at the item's QR marker.", 22, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.83f, 0.89f, 0.96f, 1f));
        subtitle.anchorMin = new Vector2(0f, 1f);
        subtitle.anchorMax = new Vector2(1f, 1f);
        subtitle.pivot = new Vector2(0f, 1f);
        subtitle.sizeDelta = new Vector2(-280f, 38f);
        subtitle.anchoredPosition = new Vector2(28f, -76f);

        Button closeButton = CreateButton("QrScanCloseButton", headerPanel, uiSprite, font, "Close", new Color(0.1f, 0.12f, 0.16f));
        SetAnchoredRect(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(210f, 68f), new Vector2(-126f, -2f));

        RectTransform frameRoot = CreatePanel("QrScanFrame", overlay, uiSprite, new Color(1f, 1f, 1f, 0.02f));
        frameRoot.anchorMin = new Vector2(0.5f, 0.56f);
        frameRoot.anchorMax = new Vector2(0.5f, 0.56f);
        frameRoot.pivot = new Vector2(0.5f, 0.5f);
        frameRoot.sizeDelta = new Vector2(640f, 640f);
        frameRoot.anchoredPosition = Vector2.zero;

        CreateQrFrameLine("Top", frameRoot, uiSprite, new Vector2(0.5f, 1f), new Vector2(640f, 14f), new Vector2(0f, -7f));
        CreateQrFrameLine("Bottom", frameRoot, uiSprite, new Vector2(0.5f, 0f), new Vector2(640f, 14f), new Vector2(0f, 7f));
        CreateQrFrameLine("Left", frameRoot, uiSprite, new Vector2(0f, 0.5f), new Vector2(14f, 640f), new Vector2(7f, 0f));
        CreateQrFrameLine("Right", frameRoot, uiSprite, new Vector2(1f, 0.5f), new Vector2(14f, 640f), new Vector2(-7f, 0f));

        RectTransform hintText = CreateText("QrScanHintText", overlay, font, "Point the camera at the item's QR marker.", 26, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        hintText.anchorMin = new Vector2(0.08f, 0.2f);
        hintText.anchorMax = new Vector2(0.92f, 0.2f);
        hintText.pivot = new Vector2(0.5f, 0.5f);
        hintText.sizeDelta = new Vector2(0f, 96f);
        hintText.anchoredPosition = Vector2.zero;

        return new QrScanUi
        {
            Overlay = overlay.gameObject,
            CloseButton = closeButton,
            HintText = hintText.GetComponent<Text>()
        };
    }

    private static void CreateQrFrameLine(string name, Transform parent, Sprite uiSprite, Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
    {
        RectTransform line = CreatePanel("QrScanFrame" + name, parent, uiSprite, new Color(0.18f, 0.62f, 1f, 1f));
        line.anchorMin = anchor;
        line.anchorMax = anchor;
        line.pivot = anchor;
        line.sizeDelta = size;
        line.anchoredPosition = anchoredPosition;
    }

    private static Button CreateFloatingQrShortcutButton(Transform parent, Sprite circleSprite, Font font)
    {
        GameObject buttonObject = new GameObject("QrScanShortcutButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Button button = buttonObject.GetComponent<Button>();
        ConfigureFloatingQrShortcutButton(button, font);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = circleSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;

        RectTransform labelRect = CreateText("Text", buttonObject.transform, font, "QR", 36, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        StretchToParent(labelRect);

        return button;
    }

    private static void ConfigureFloatingQrShortcutButton(Button button, Font font)
    {
        if (button == null)
        {
            return;
        }

        Color buttonColor = new Color(0.28f, 0.31f, 0.36f, 0.96f);
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(126f, 126f);
            rectTransform.anchoredPosition = new Vector2(-92f, -312f);
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
            image.color = buttonColor;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = new Color(0.38f, 0.42f, 0.48f, 1f);
        colors.pressedColor = new Color(0.18f, 0.2f, 0.24f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.45f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.transform.SetAsLastSibling();

        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.font = font;
            label.text = "QR";
            label.fontSize = 36;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }
    }

    private static void RepositionScanChoiceButton(Transform parent, string buttonName, Vector2 anchoredPosition, Color? color)
    {
        Transform buttonTransform = FindChildRecursive(parent, buttonName);
        if (buttonTransform == null)
        {
            return;
        }

        RectTransform rectTransform = buttonTransform.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            SetAnchoredRect(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(620f, 78f), anchoredPosition);
        }

        if (color.HasValue)
        {
            Image image = buttonTransform.GetComponent<Image>();
            if (image != null)
            {
                image.color = color.Value;
            }
        }
    }

    private static Transform FindTransformInActiveScene(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        GameObject[] rootObjects = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            if (rootObjects[i] == null)
            {
                continue;
            }

            Transform found = FindChildRecursive(rootObjects[i].transform, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        if (string.Equals(parent.name, childName, StringComparison.OrdinalIgnoreCase))
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void ConfigureScanUIController(
        ScanUIController scanUIController,
        Button scanItemButton,
        Button qrModeButton,
        Button qrShortcutButton,
        Button cameraModeButton,
        Button galleryModeButton,
        Button cancelChoiceButton,
        Button qrCloseButton,
        Button playPauseButton,
        Button closeResultButton,
        GameObject scanChoiceOverlay,
        GameObject qrScanOverlay,
        GameObject cameraHintPanel,
        GameObject resultOverlay,
        Text qrScanHintText,
        Text cameraHintText,
        Text resultTitleText,
        Text resultDescriptionText,
        Text resultStatusText,
        Text playPauseButtonText,
        Image resultPreviewImage)
    {
        SerializedObject serializedScanUi = new SerializedObject(scanUIController);
        serializedScanUi.FindProperty("scanItemButton").objectReferenceValue = scanItemButton;
        serializedScanUi.FindProperty("qrModeButton").objectReferenceValue = qrModeButton;
        serializedScanUi.FindProperty("qrShortcutButton").objectReferenceValue = qrShortcutButton;
        serializedScanUi.FindProperty("cameraModeButton").objectReferenceValue = cameraModeButton;
        serializedScanUi.FindProperty("galleryModeButton").objectReferenceValue = galleryModeButton;
        serializedScanUi.FindProperty("cancelChoiceButton").objectReferenceValue = cancelChoiceButton;
        serializedScanUi.FindProperty("qrCloseButton").objectReferenceValue = qrCloseButton;
        serializedScanUi.FindProperty("playPauseButton").objectReferenceValue = playPauseButton;
        serializedScanUi.FindProperty("closeResultButton").objectReferenceValue = closeResultButton;
        serializedScanUi.FindProperty("scanChoiceOverlay").objectReferenceValue = scanChoiceOverlay;
        serializedScanUi.FindProperty("qrScanOverlay").objectReferenceValue = qrScanOverlay;
        serializedScanUi.FindProperty("cameraHintPanel").objectReferenceValue = cameraHintPanel;
        serializedScanUi.FindProperty("resultOverlay").objectReferenceValue = resultOverlay;
        serializedScanUi.FindProperty("qrScanHintText").objectReferenceValue = qrScanHintText;
        serializedScanUi.FindProperty("cameraHintText").objectReferenceValue = cameraHintText;
        serializedScanUi.FindProperty("resultTitleText").objectReferenceValue = resultTitleText;
        serializedScanUi.FindProperty("resultDescriptionText").objectReferenceValue = resultDescriptionText;
        serializedScanUi.FindProperty("resultStatusText").objectReferenceValue = resultStatusText;
        serializedScanUi.FindProperty("playPauseButtonText").objectReferenceValue = playPauseButtonText;
        serializedScanUi.FindProperty("resultPreviewImage").objectReferenceValue = resultPreviewImage;
        serializedScanUi.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureImageRecognitionManager(
        ImageRecognitionManager imageRecognitionManager,
        XROrigin xrOrigin,
        ARTrackedImageManager trackedImageManager,
        GalleryPicker galleryPicker,
        ScanUIController scanUIController,
        AudioManager audioManager,
        IReadOnlyList<ScanItemImportData> scanItems)
    {
        SerializedObject serializedRecognitionManager = new SerializedObject(imageRecognitionManager);
        ARCameraManager arCameraManager = xrOrigin != null && xrOrigin.Camera != null
            ? xrOrigin.Camera.GetComponent<ARCameraManager>()
            : null;

        serializedRecognitionManager.FindProperty("xrOrigin").objectReferenceValue = xrOrigin;
        serializedRecognitionManager.FindProperty("trackedImageManager").objectReferenceValue = trackedImageManager;
        serializedRecognitionManager.FindProperty("arCameraManager").objectReferenceValue = arCameraManager;
        serializedRecognitionManager.FindProperty("galleryPicker").objectReferenceValue = galleryPicker;
        serializedRecognitionManager.FindProperty("scanUIController").objectReferenceValue = scanUIController;
        serializedRecognitionManager.FindProperty("audioManager").objectReferenceValue = audioManager;
        serializedRecognitionManager.FindProperty("maxMovingImages").intValue = 1;
        serializedRecognitionManager.FindProperty("qrMarkerPhysicalWidthMeters").floatValue = 0.08f;
        serializedRecognitionManager.FindProperty("galleryHashResolution").intValue = 8;
        serializedRecognitionManager.FindProperty("maxAcceptedHashDistance").intValue = 24;
        serializedRecognitionManager.FindProperty("maxAcceptedLiveCameraHashDistance").intValue = 30;
        serializedRecognitionManager.FindProperty("maxAcceptedAspectDifference").floatValue = 0.25f;
        serializedRecognitionManager.FindProperty("minimumBestMatchMargin").intValue = 6;
        serializedRecognitionManager.FindProperty("liveCameraSampleSize").intValue = 256;
        serializedRecognitionManager.FindProperty("liveCameraScanIntervalSeconds").floatValue = 0.55f;
        serializedRecognitionManager.FindProperty("minimumLiveCameraStableMatches").intValue = 2;
        serializedRecognitionManager.FindProperty("liveCameraFallbackMatchesTrackedImagesOnly").boolValue = false;
        serializedRecognitionManager.FindProperty("noMatchReminderInterval").intValue = 5;
        serializedRecognitionManager.FindProperty("startCameraScanningAutomatically").boolValue = true;
        serializedRecognitionManager.FindProperty("resumeAutomaticScanAfterClosingResult").boolValue = true;
        serializedRecognitionManager.FindProperty("repeatedCameraMatchCooldownSeconds").floatValue = 5f;
        serializedRecognitionManager.FindProperty("trackedLabelHeight").floatValue = 0.12f;
        serializedRecognitionManager.FindProperty("trackedLabelScale").floatValue = 0.05f;
        serializedRecognitionManager.FindProperty("trackedLabelFontSize").intValue = 34;

        SerializedProperty scanItemArray = serializedRecognitionManager.FindProperty("scanItems");
        scanItemArray.arraySize = scanItems.Count;
        for (int i = 0; i < scanItems.Count; i++)
        {
            SerializedProperty element = scanItemArray.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("id").stringValue = scanItems[i].Id;
            element.FindPropertyRelative("itemName").stringValue = scanItems[i].Name;
            element.FindPropertyRelative("previewImage").objectReferenceValue = scanItems[i].PreviewSprite;
            element.FindPropertyRelative("referenceTexture").objectReferenceValue = scanItems[i].ReferenceTexture;
            element.FindPropertyRelative("qrMarkerTexture").objectReferenceValue = scanItems[i].QrMarkerTexture;
            element.FindPropertyRelative("description").stringValue = scanItems[i].Description;
            element.FindPropertyRelative("audioClip").objectReferenceValue = scanItems[i].AudioClip;
            element.FindPropertyRelative("modelPrefab").objectReferenceValue = null;
            element.FindPropertyRelative("modelScale").floatValue = 1f;
            element.FindPropertyRelative("modelTint").colorValue = Color.HSVToRGB((i * 0.075f) % 1f, 0.62f, 1f);
            element.FindPropertyRelative("showGeneratedReliefModel").boolValue = true;
            element.FindPropertyRelative("physicalWidthMeters").floatValue = scanItems[i].PhysicalWidthMeters;
            element.FindPropertyRelative("averageHashHex").stringValue = scanItems[i].AverageHashHex;
            element.FindPropertyRelative("differenceHashHex").stringValue = scanItems[i].DifferenceHashHex;
            element.FindPropertyRelative("aspectRatio").floatValue = scanItems[i].AspectRatio;
            element.FindPropertyRelative("useForTrackedImage").boolValue = scanItems[i].UseForTrackedImage;
        }

        serializedRecognitionManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TourLocationDefinition[] GetTourDefinitions()
    {
        return new[]
        {
            new TourLocationDefinition(
                "Balcony",
                "Starting point of the route. Stand here first so the AR guide can establish the tour direction.",
                0,
                new Vector3(0f, 0f, 0f),
                1.5f),
            new TourLocationDefinition(
                "Sala",
                "Proceed to the Sala and follow the route as it opens into the main receiving area.",
                1,
                new Vector3(1.3f, 0f, 1.4f),
                1.5f,
                new Vector3(0.55f, 0f, 0.65f),
                new Vector3(1.05f, 0f, 1.05f)),
            new TourLocationDefinition(
                "Dining",
                "Continue toward the Dining area where the household and ceremonial route continues.",
                2,
                new Vector3(2.8f, 0f, 2.6f),
                1.5f,
                new Vector3(1.8f, 0f, 1.8f),
                new Vector3(2.45f, 0f, 2.25f)),
            new TourLocationDefinition(
                "Bedroom",
                "Move into the Bedroom stop and keep the arrow centered as the route narrows.",
                3,
                new Vector3(4.2f, 0f, 3.8f),
                1.5f,
                new Vector3(3.25f, 0f, 3.05f),
                new Vector3(3.85f, 0f, 3.45f)),
            new TourLocationDefinition(
                "Family Room",
                "Follow the guide to the Family Room and stay near the route line for the cleanest AR experience.",
                4,
                new Vector3(5.4f, 0f, 5.1f),
                1.5f,
                new Vector3(4.65f, 0f, 4.35f),
                new Vector3(5.15f, 0f, 4.75f)),
            new TourLocationDefinition(
                "Secret Areas",
                "Continue to the Secret Areas stop and let the guide lead you through the hidden route segment.",
                5,
                new Vector3(5.5f, 0f, 6.8f),
                1.5f,
                new Vector3(5.55f, 0f, 5.75f),
                new Vector3(5.55f, 0f, 6.35f)),
            new TourLocationDefinition(
                "War Memorabilia",
                "Turn toward the War Memorabilia stop for the collection-focused portion of the tour.",
                6,
                new Vector3(4.4f, 0f, 8.2f),
                1.5f,
                new Vector3(5.05f, 0f, 7.35f),
                new Vector3(4.75f, 0f, 7.85f)),
            new TourLocationDefinition(
                "Documents",
                "Proceed to Documents and follow the mini-map as the route bends back across the gallery.",
                7,
                new Vector3(2.8f, 0f, 9.5f),
                1.5f,
                new Vector3(3.85f, 0f, 8.75f),
                new Vector3(3.25f, 0f, 9.15f)),
            new TourLocationDefinition(
                "Garden",
                "Final destination. Complete the route at the Garden after reviewing the last heritage stop.",
                8,
                new Vector3(1.3f, 0f, 11.0f),
                1.5f,
                new Vector3(2.25f, 0f, 10.05f),
                new Vector3(1.65f, 0f, 10.55f))
        };
    }

    private static List<GalleryPhotoImportData> LoadGalleryPhotoDefinitions()
    {
        EnsureGalleryPhotoImports();

        List<GalleryPhotoImportData> galleryPhotos = new List<GalleryPhotoImportData>();
        Dictionary<string, GalleryMetadataEntry> metadataByFileName = LoadGalleryMetadataLookup();
        if (!AssetDatabase.IsValidFolder(GalleryPhotoFolder))
        {
            return galleryPhotos;
        }

        string absoluteFolderPath = Path.Combine(Application.dataPath, "all pictures AR");
        if (!Directory.Exists(absoluteFolderPath))
        {
            return galleryPhotos;
        }

        string[] imagePaths = Directory.GetFiles(absoluteFolderPath, "*.*", SearchOption.TopDirectoryOnly);
        Array.Sort(imagePaths, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < imagePaths.Length; i++)
        {
            string extension = Path.GetExtension(imagePaths[i]);
            if (!IsSupportedGalleryImageExtension(extension))
            {
                continue;
            }

            string assetPath = GalleryPhotoFolder + "/" + Path.GetFileName(imagePaths[i]);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                continue;
            }

            int photoNumber = galleryPhotos.Count + 1;
            string fileName = Path.GetFileName(imagePaths[i]);
            galleryPhotos.Add(new GalleryPhotoImportData
            {
                Title = BuildGalleryPhotoTitle(photoNumber),
                Definition = BuildGalleryPhotoDefinition(photoNumber, fileName, metadataByFileName),
                Sprite = sprite
            });
        }

        return galleryPhotos;
    }

    private static void EnsureGalleryPhotoImports()
    {
        if (!AssetDatabase.IsValidFolder(GalleryPhotoFolder))
        {
            return;
        }

        string absoluteFolderPath = Path.Combine(Application.dataPath, "all pictures AR");
        if (!Directory.Exists(absoluteFolderPath))
        {
            return;
        }

        string[] imagePaths = Directory.GetFiles(absoluteFolderPath, "*.*", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < imagePaths.Length; i++)
        {
            string extension = Path.GetExtension(imagePaths[i]);
            if (!IsSupportedGalleryImageExtension(extension))
            {
                continue;
            }

            string assetPath = GalleryPhotoFolder + "/" + Path.GetFileName(imagePaths[i]);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            bool needsReimport = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                needsReimport = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                needsReimport = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                needsReimport = true;
            }

            if (importer.isReadable)
            {
                importer.isReadable = false;
                needsReimport = true;
            }

            if (importer.maxTextureSize != 1024)
            {
                importer.maxTextureSize = 1024;
                needsReimport = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Compressed)
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
                needsReimport = true;
            }

            if (needsReimport)
            {
                importer.SaveAndReimport();
            }
        }
    }

    private static bool IsSupportedGalleryImageExtension(string extension)
    {
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".tif", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildGalleryPhotoTitle(int photoNumber)
    {
        return "Aguinaldo Shrine Archive Photo " + photoNumber.ToString("000");
    }

    private static string BuildGalleryPhotoDefinition(
        int photoNumber,
        string fileName,
        IReadOnlyDictionary<string, GalleryMetadataEntry> metadataByFileName)
    {
        if (TryGetGalleryMetadata(fileName, metadataByFileName, out GalleryMetadataEntry metadata) &&
            !string.IsNullOrWhiteSpace(metadata.description))
        {
            return metadata.description;
        }

        string template = GalleryDefinitionTemplates[(photoNumber - 1) % GalleryDefinitionTemplates.Length];
        return template + " Gallery item " + photoNumber.ToString("000") + " comes directly from the all pictures AR folder so the full image archive stays available inside the mobile application.";
    }

    private static List<ScanItemImportData> LoadScanItemDefinitions()
    {
        EnsureScanReferenceImports();

        List<ScanItemImportData> scanItems = new List<ScanItemImportData>();
        Dictionary<string, GalleryMetadataEntry> metadataByFileName = LoadGalleryMetadataLookup();
        if (!AssetDatabase.IsValidFolder(GalleryPhotoFolder))
        {
            return scanItems;
        }

        string absoluteFolderPath = Path.Combine(Application.dataPath, "all pictures AR");
        if (!Directory.Exists(absoluteFolderPath))
        {
            return scanItems;
        }

        string[] imagePaths = Directory.GetFiles(absoluteFolderPath, "*.*", SearchOption.TopDirectoryOnly);
        Array.Sort(imagePaths, StringComparer.OrdinalIgnoreCase);

        int trackedReferenceCount = 0;
        for (int i = 0; i < imagePaths.Length; i++)
        {
            string extension = Path.GetExtension(imagePaths[i]);
            if (!IsSupportedGalleryImageExtension(extension))
            {
                continue;
            }

            string assetPath = GalleryPhotoFolder + "/" + Path.GetFileName(imagePaths[i]);
            Sprite previewSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            bool useForTrackedImage = trackedReferenceCount < MaxTrackedScanItems;
            Texture2D referenceTexture = useForTrackedImage
                ? AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath)
                : null;

            if (previewSprite == null || !TryCreateScanFingerprintFromFile(imagePaths[i], out ScanFingerprintImportData fingerprintData))
            {
                continue;
            }

            if (useForTrackedImage)
            {
                trackedReferenceCount++;
            }

            int itemNumber = scanItems.Count + 1;
            string fileName = Path.GetFileName(imagePaths[i]);
            string itemId = BuildScanItemId(itemNumber);
            Texture2D qrMarkerTexture = LoadOrCreateQrMarkerTexture(itemNumber, itemId, fileName);
            scanItems.Add(new ScanItemImportData
            {
                Id = itemId,
                Name = BuildScanItemName(itemNumber),
                Description = BuildScanItemDescription(itemNumber, fileName, metadataByFileName),
                PreviewSprite = previewSprite,
                ReferenceTexture = referenceTexture,
                QrMarkerTexture = qrMarkerTexture,
                AudioClip = null,
                PhysicalWidthMeters = 0.18f,
                AverageHashHex = fingerprintData.AverageHashHex,
                DifferenceHashHex = fingerprintData.DifferenceHashHex,
                AspectRatio = fingerprintData.AspectRatio,
                UseForTrackedImage = useForTrackedImage && referenceTexture != null
            });
        }

        return scanItems;
    }

    private static void EnsureScanReferenceImports()
    {
        if (!AssetDatabase.IsValidFolder(GalleryPhotoFolder))
        {
            return;
        }

        string absoluteFolderPath = Path.Combine(Application.dataPath, "all pictures AR");
        if (!Directory.Exists(absoluteFolderPath))
        {
            return;
        }

        string[] imagePaths = Directory.GetFiles(absoluteFolderPath, "*.*", SearchOption.TopDirectoryOnly);
        Array.Sort(imagePaths, StringComparer.OrdinalIgnoreCase);

        int importedScanItems = 0;
        for (int i = 0; i < imagePaths.Length && importedScanItems < MaxTrackedScanItems; i++)
        {
            string extension = Path.GetExtension(imagePaths[i]);
            if (!IsSupportedGalleryImageExtension(extension))
            {
                continue;
            }

            importedScanItems++;

            string assetPath = GalleryPhotoFolder + "/" + Path.GetFileName(imagePaths[i]);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            bool needsReimport = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                needsReimport = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                needsReimport = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                needsReimport = true;
            }

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                needsReimport = true;
            }

            if (importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                needsReimport = true;
            }

            if (importer.maxTextureSize < 1024)
            {
                importer.maxTextureSize = 1024;
                needsReimport = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                needsReimport = true;
            }

            if (needsReimport)
            {
                importer.SaveAndReimport();
            }
        }
    }

    private static Texture2D LoadOrCreateQrMarkerTexture(int itemNumber, string itemId, string sourceFileName)
    {
        Directory.CreateDirectory(QrMarkerFolder);

        string assetPath = QrMarkerFolder + "/qr-" + itemId + ".png";
        Texture2D markerTexture = GenerateQrStyleMarkerTexture(itemNumber, itemId, sourceFileName);
        File.WriteAllBytes(assetPath, markerTexture.EncodeToPNG());
        Object.DestroyImmediate(markerTexture);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        ConfigureQrMarkerImport(assetPath);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static void ConfigureQrMarkerImport(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        bool needsReimport = false;
        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            needsReimport = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            needsReimport = true;
        }

        if (!importer.isReadable)
        {
            importer.isReadable = true;
            needsReimport = true;
        }

        if (importer.npotScale != TextureImporterNPOTScale.None)
        {
            importer.npotScale = TextureImporterNPOTScale.None;
            needsReimport = true;
        }

        if (importer.maxTextureSize != QrMarkerPixelSize)
        {
            importer.maxTextureSize = QrMarkerPixelSize;
            needsReimport = true;
        }

        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            needsReimport = true;
        }

        if (importer.filterMode != FilterMode.Point)
        {
            importer.filterMode = FilterMode.Point;
            needsReimport = true;
        }

        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            needsReimport = true;
        }

        if (needsReimport)
        {
            importer.SaveAndReimport();
        }
    }

    private static Texture2D GenerateQrStyleMarkerTexture(int itemNumber, string itemId, string sourceFileName)
    {
        Texture2D texture = new Texture2D(QrMarkerPixelSize, QrMarkerPixelSize, TextureFormat.RGBA32, false);
        FillTexture(texture, Color.white);

        bool[,] modules = new bool[QrMarkerModuleCount, QrMarkerModuleCount];
        bool[,] reserved = new bool[QrMarkerModuleCount, QrMarkerModuleCount];

        DrawQrFinder(modules, reserved, 0, 0);
        DrawQrFinder(modules, reserved, QrMarkerModuleCount - 7, 0);
        DrawQrFinder(modules, reserved, 0, QrMarkerModuleCount - 7);
        DrawQrAlignment(modules, reserved, QrMarkerModuleCount - 8, QrMarkerModuleCount - 8);
        DrawQrTiming(modules, reserved);
        FillQrDataModules(modules, reserved, itemNumber, itemId, sourceFileName);

        int quietModules = 4;
        int totalModules = QrMarkerModuleCount + quietModules + quietModules;
        int modulePixels = Mathf.Max(1, QrMarkerPixelSize / totalModules);
        int origin = (QrMarkerPixelSize - (totalModules * modulePixels)) / 2;

        for (int y = 0; y < QrMarkerModuleCount; y++)
        {
            for (int x = 0; x < QrMarkerModuleCount; x++)
            {
                if (!modules[x, y])
                {
                    continue;
                }

                DrawRect(
                    texture,
                    origin + ((x + quietModules) * modulePixels),
                    origin + ((y + quietModules) * modulePixels),
                    modulePixels,
                    modulePixels,
                    Color.black);
            }
        }

        texture.Apply(false, false);
        return texture;
    }

    private static void DrawQrFinder(bool[,] modules, bool[,] reserved, int startX, int startY)
    {
        for (int y = -1; y <= 7; y++)
        {
            for (int x = -1; x <= 7; x++)
            {
                int moduleX = startX + x;
                int moduleY = startY + y;
                if (!IsQrModuleInside(moduleX, moduleY))
                {
                    continue;
                }

                bool isInsideFinder = x >= 0 && x <= 6 && y >= 0 && y <= 6;
                bool isOuter = isInsideFinder && (x == 0 || x == 6 || y == 0 || y == 6);
                bool isCenter = isInsideFinder && x >= 2 && x <= 4 && y >= 2 && y <= 4;
                SetQrModule(modules, reserved, moduleX, moduleY, isOuter || isCenter, true);
            }
        }
    }

    private static void DrawQrAlignment(bool[,] modules, bool[,] reserved, int centerX, int centerY)
    {
        for (int y = -2; y <= 2; y++)
        {
            for (int x = -2; x <= 2; x++)
            {
                int moduleX = centerX + x;
                int moduleY = centerY + y;
                if (!IsQrModuleInside(moduleX, moduleY))
                {
                    continue;
                }

                bool isBorder = Mathf.Abs(x) == 2 || Mathf.Abs(y) == 2;
                bool isCenter = x == 0 && y == 0;
                SetQrModule(modules, reserved, moduleX, moduleY, isBorder || isCenter, true);
            }
        }
    }

    private static void DrawQrTiming(bool[,] modules, bool[,] reserved)
    {
        for (int i = 8; i < QrMarkerModuleCount - 8; i++)
        {
            bool value = i % 2 == 0;
            SetQrModule(modules, reserved, i, 6, value, true);
            SetQrModule(modules, reserved, 6, i, value, true);
        }
    }

    private static void FillQrDataModules(bool[,] modules, bool[,] reserved, int itemNumber, string itemId, string sourceFileName)
    {
        uint state = ComputeStableMarkerSeed(itemId + "|" + sourceFileName + "|" + itemNumber.ToString("000"));
        for (int y = 0; y < QrMarkerModuleCount; y++)
        {
            for (int x = 0; x < QrMarkerModuleCount; x++)
            {
                if (reserved[x, y])
                {
                    continue;
                }

                state = NextMarkerSeed(state);
                bool value = ((state >> ((x + y + itemNumber) % 23)) & 1U) == 1U;
                if (((x * 3) + (y * 5) + itemNumber) % 11 == 0)
                {
                    value = !value;
                }

                SetQrModule(modules, reserved, x, y, value, false);
            }
        }

        for (int bit = 0; bit < 16; bit++)
        {
            bool value = ((itemNumber >> bit) & 1) == 1;
            SetQrModule(modules, reserved, 8 + bit, QrMarkerModuleCount - 2, value, true);
            SetQrModule(modules, reserved, QrMarkerModuleCount - 2, 8 + bit, value, true);
        }
    }

    private static bool IsQrModuleInside(int x, int y)
    {
        return x >= 0 && x < QrMarkerModuleCount && y >= 0 && y < QrMarkerModuleCount;
    }

    private static void SetQrModule(bool[,] modules, bool[,] reserved, int x, int y, bool value, bool markReserved)
    {
        if (!IsQrModuleInside(x, y))
        {
            return;
        }

        modules[x, y] = value;
        if (markReserved)
        {
            reserved[x, y] = true;
        }
    }

    private static uint ComputeStableMarkerSeed(string value)
    {
        unchecked
        {
            uint hash = 2166136261U;
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619U;
                }
            }

            return hash == 0U ? 1U : hash;
        }
    }

    private static uint NextMarkerSeed(uint state)
    {
        unchecked
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state == 0U ? 1U : state;
        }
    }

    private static string BuildScanItemId(int itemNumber)
    {
        return "scan-item-" + itemNumber.ToString("000");
    }

    private static string BuildScanItemName(int itemNumber)
    {
        return "Aguinaldo Shrine Scan Item " + itemNumber.ToString("000");
    }

    private static string BuildScanItemDescription(
        int itemNumber,
        string fileName,
        IReadOnlyDictionary<string, GalleryMetadataEntry> metadataByFileName)
    {
        if (TryGetGalleryMetadata(fileName, metadataByFileName, out GalleryMetadataEntry metadata) &&
            !string.IsNullOrWhiteSpace(metadata.description))
        {
            return metadata.description;
        }

        string template = ScanDescriptionTemplates[(itemNumber - 1) % ScanDescriptionTemplates.Length];
        return template + " This entry is auto-loaded from the project image database as scan item " + itemNumber.ToString("000") + ".";
    }

    private static Dictionary<string, GalleryMetadataEntry> LoadGalleryMetadataLookup()
    {
        Dictionary<string, GalleryMetadataEntry> metadataByFileName = new Dictionary<string, GalleryMetadataEntry>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(GalleryMetadataPath))
        {
            return metadataByFileName;
        }

        try
        {
            GalleryMetadataCollection metadataCollection = JsonUtility.FromJson<GalleryMetadataCollection>(File.ReadAllText(GalleryMetadataPath));
            if (metadataCollection?.entries == null)
            {
                return metadataByFileName;
            }

            for (int i = 0; i < metadataCollection.entries.Length; i++)
            {
                GalleryMetadataEntry entry = metadataCollection.entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.fileName))
                {
                    continue;
                }

                metadataByFileName[entry.fileName] = entry;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Gallery metadata could not be loaded. Reason: " + exception.Message);
        }

        return metadataByFileName;
    }

    private static bool TryGetGalleryMetadata(
        string fileName,
        IReadOnlyDictionary<string, GalleryMetadataEntry> metadataByFileName,
        out GalleryMetadataEntry metadata)
    {
        metadata = null;
        if (string.IsNullOrWhiteSpace(fileName) || metadataByFileName == null)
        {
            return false;
        }

        return metadataByFileName.TryGetValue(fileName, out metadata);
    }

    private static bool TryCreateScanFingerprintFromFile(string absoluteImagePath, out ScanFingerprintImportData fingerprintData)
    {
        fingerprintData = default;
        if (string.IsNullOrWhiteSpace(absoluteImagePath) || !File.Exists(absoluteImagePath))
        {
            return false;
        }

        Texture2D sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        try
        {
            byte[] imageBytes = File.ReadAllBytes(absoluteImagePath);
            if (imageBytes == null || imageBytes.Length == 0 || !sourceTexture.LoadImage(imageBytes, false))
            {
                return false;
            }

            ImageFingerprintImport fingerprint = CreateFingerprintFromTexture(sourceTexture, 8);
            fingerprintData = new ScanFingerprintImportData
            {
                AverageHashHex = fingerprint.AverageHash.ToString("X16"),
                DifferenceHashHex = fingerprint.DifferenceHash.ToString("X16"),
                AspectRatio = fingerprint.AspectRatio
            };

            return fingerprint.AspectRatio > 0f;
        }
        finally
        {
            Object.DestroyImmediate(sourceTexture);
        }
    }

    private static int CountTrackedScanItems(IReadOnlyList<ScanItemImportData> scanItems)
    {
        if (scanItems == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < scanItems.Count; i++)
        {
            if (scanItems[i].UseForTrackedImage && scanItems[i].ReferenceTexture != null)
            {
                count++;
            }
        }

        return count;
    }

    private static ImageFingerprintImport CreateFingerprintFromTexture(Texture2D texture, int resolution)
    {
        int safeResolution = Mathf.Clamp(resolution, 4, 8);
        float[] brightnessSamples = new float[safeResolution * safeResolution];
        float brightnessSum = 0f;

        for (int y = 0; y < safeResolution; y++)
        {
            for (int x = 0; x < safeResolution; x++)
            {
                float sampleX = (x + 0.5f) / safeResolution;
                float sampleY = (y + 0.5f) / safeResolution;
                float brightness = GetTextureBrightness(texture, sampleX, sampleY);

                int index = y * safeResolution + x;
                brightnessSamples[index] = brightness;
                brightnessSum += brightness;
            }
        }

        float averageBrightness = brightnessSamples.Length > 0
            ? brightnessSum / brightnessSamples.Length
            : 0f;

        ulong averageHash = 0UL;
        for (int i = 0; i < brightnessSamples.Length; i++)
        {
            if (brightnessSamples[i] >= averageBrightness)
            {
                averageHash |= 1UL << i;
            }
        }

        ulong differenceHash = 0UL;
        int differenceHashBitIndex = 0;
        for (int y = 0; y < safeResolution; y++)
        {
            for (int x = 0; x < safeResolution; x++)
            {
                float leftSampleX = (x + 0.5f) / (safeResolution + 1f);
                float rightSampleX = (x + 1.5f) / (safeResolution + 1f);
                float sampleY = (y + 0.5f) / safeResolution;

                float leftBrightness = GetTextureBrightness(texture, leftSampleX, sampleY);
                float rightBrightness = GetTextureBrightness(texture, rightSampleX, sampleY);
                if (leftBrightness >= rightBrightness)
                {
                    differenceHash |= 1UL << differenceHashBitIndex;
                }

                differenceHashBitIndex++;
            }
        }

        return new ImageFingerprintImport
        {
            AverageHash = averageHash,
            DifferenceHash = differenceHash,
            AspectRatio = texture.height <= 0 ? 1f : texture.width / (float)texture.height
        };
    }

    private static float GetTextureBrightness(Texture2D texture, float normalizedX, float normalizedY)
    {
        Color pixel = texture.GetPixelBilinear(normalizedX, normalizedY);
        return (pixel.r * 0.299f) + (pixel.g * 0.587f) + (pixel.b * 0.114f);
    }

    private static LocationTrigger CreateLocationObject(Transform parent, TourLocationDefinition definition)
    {
        GameObject locationObject = new GameObject(definition.Name);
        locationObject.transform.SetParent(parent, false);
        locationObject.transform.localPosition = definition.Position;

        List<Transform> approachPoints = new List<Transform>();
        if (definition.ApproachPoints.Count > 0)
        {
            GameObject guideRoot = new GameObject("GuidePoints");
            guideRoot.transform.SetParent(locationObject.transform, false);

            for (int i = 0; i < definition.ApproachPoints.Count; i++)
            {
                GameObject point = new GameObject("GuidePoint_" + (i + 1));
                point.transform.SetParent(guideRoot.transform, false);
                point.transform.localPosition = definition.ApproachPoints[i] - definition.Position;
                approachPoints.Add(point.transform);
            }
        }

        LocationTrigger trigger = locationObject.AddComponent<LocationTrigger>();
        SerializedObject serializedTrigger = new SerializedObject(trigger);
        serializedTrigger.FindProperty("locationName").stringValue = definition.Name;
        serializedTrigger.FindProperty("description").stringValue = definition.Description;
        serializedTrigger.FindProperty("sequenceOrder").intValue = definition.Order;
        serializedTrigger.FindProperty("reachDistance").floatValue = definition.ReachDistance;

        SerializedProperty approachArray = serializedTrigger.FindProperty("approachPoints");
        approachArray.arraySize = approachPoints.Count;
        for (int i = 0; i < approachPoints.Count; i++)
        {
            approachArray.GetArrayElementAtIndex(i).objectReferenceValue = approachPoints[i];
        }

        serializedTrigger.ApplyModifiedPropertiesWithoutUndo();
        return trigger;
    }

    private static void BuildArrowModel(Transform parent, Material arrowMaterial, Material primaryMaterial, Material accentMaterial)
    {
        CreatePrimitive("BaseDisc", PrimitiveType.Cylinder, parent, new Vector3(0f, -0.02f, 0f), new Vector3(0.26f, 0.02f, 0.26f), Vector3.zero, primaryMaterial);
        CreatePrimitive("Body", PrimitiveType.Cube, parent, new Vector3(0f, 0.04f, 0.02f), new Vector3(0.12f, 0.05f, 0.42f), Vector3.zero, arrowMaterial);
        CreatePrimitive("HeadLeft", PrimitiveType.Cube, parent, new Vector3(-0.1f, 0.04f, 0.2f), new Vector3(0.12f, 0.05f, 0.24f), new Vector3(0f, 42f, 0f), arrowMaterial);
        CreatePrimitive("HeadRight", PrimitiveType.Cube, parent, new Vector3(0.1f, 0.04f, 0.2f), new Vector3(0.12f, 0.05f, 0.24f), new Vector3(0f, -42f, 0f), arrowMaterial);
        CreatePrimitive("Accent", PrimitiveType.Sphere, parent, new Vector3(0f, 0.12f, -0.1f), new Vector3(0.08f, 0.08f, 0.08f), Vector3.zero, accentMaterial);
    }

    private static BillboardLabel CreateFloatingLabel(Transform parent)
    {
        GameObject labelObject = new GameObject("FloatingLabel", typeof(TextMesh), typeof(BillboardLabel));
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        labelObject.transform.localScale = Vector3.one * 0.065f;

        TextMesh textMesh = labelObject.GetComponent<TextMesh>();
        textMesh.text = string.Empty;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.05f;
        textMesh.fontSize = 36;
        textMesh.lineSpacing = 0.85f;
        textMesh.color = Color.white;
        textMesh.font = GetDefaultFont();

        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = textMesh.font.material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return labelObject.GetComponent<BillboardLabel>();
    }

    private static Transform CreateDestinationBeacon(Transform parent, Material accentMaterial, Material primaryMaterial)
    {
        GameObject root = new GameObject("DestinationBeacon");
        root.transform.SetParent(parent, false);

        CreatePrimitive("Ring", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0f, 0f), new Vector3(0.28f, 0.015f, 0.28f), Vector3.zero, primaryMaterial);
        CreatePrimitive("Core", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.16f, 0f), new Vector3(0.18f, 0.18f, 0.18f), Vector3.zero, accentMaterial);
        return root.transform;
    }

    private static List<Transform> CreateBreadcrumbDots(Transform parent, Material accentMaterial)
    {
        List<Transform> dots = new List<Transform>();
        for (int i = 0; i < 5; i++)
        {
            GameObject dot = CreatePrimitive("Breadcrumb_" + (i + 1), PrimitiveType.Sphere, parent, Vector3.zero, Vector3.one * 0.08f, Vector3.zero, accentMaterial);
            dots.Add(dot.transform);
        }

        return dots;
    }

    private static GameObject CreatePrimitive(
        string name,
        PrimitiveType primitiveType,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Vector3 localEulerAngles,
        Material material)
    {
        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.name = name;
        primitive.transform.SetParent(parent, false);
        primitive.transform.localPosition = localPosition;
        primitive.transform.localScale = localScale;
        primitive.transform.localEulerAngles = localEulerAngles;
        ApplyPrimitiveVisual(primitive, material);
        return primitive;
    }

    private static void ApplyPrimitiveVisual(GameObject primitive, Material material)
    {
        Collider collider = primitive.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        MeshRenderer renderer = primitive.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private static void EnsureDemoMaterials()
    {
        LoadOrCreateMaterial(ArrowMaterialPath, new Color(0.12f, 0.45f, 0.92f), new Color(0.06f, 0.22f, 0.46f));
        LoadOrCreateMaterial(PrimaryMaterialPath, new Color(0.06f, 0.12f, 0.18f), new Color(0f, 0f, 0f));
        LoadOrCreateMaterial(AccentMaterialPath, new Color(0.96f, 0.68f, 0.26f), new Color(0.2f, 0.08f, 0f));
    }

    private static Material LoadOrCreateMaterial(string assetPath, Color baseColor, Color emissionColor)
    {
        Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (existingMaterial != null)
        {
            return existingMaterial;
        }

        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        Material material = new Material(shader)
        {
            color = baseColor
        };

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor);
        }

        AssetDatabase.CreateAsset(material, assetPath);
        return material;
    }

    private static Sprite LoadOrCreateMapSprite()
    {
        Texture2D texture = new Texture2D(1152, 360, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Color floorColor = new Color(0.955f, 0.97f, 0.985f);
        Color roomColor = new Color(0.89f, 0.92f, 0.95f);
        Color accentRoomColor = new Color(0.93f, 0.88f, 0.98f);
        Color gardenColor = new Color(0.87f, 0.94f, 0.88f);
        Color corridorColor = new Color(0.995f, 0.998f, 1f);
        Color wallColor = new Color(0.2f, 0.25f, 0.29f);
        Color lightWallColor = new Color(0.68f, 0.73f, 0.78f);

        FillTexture(texture, floorColor);

        DrawRect(texture, 24, 28, 260, 114, accentRoomColor);
        DrawRect(texture, 24, 150, 260, 84, accentRoomColor);
        DrawRect(texture, 316, 62, 198, 126, roomColor);
        DrawRect(texture, 546, 108, 220, 124, roomColor);
        DrawRect(texture, 792, 148, 202, 122, roomColor);
        DrawRect(texture, 924, 224, 184, 94, gardenColor);
        DrawRect(texture, 250, 126, 770, 64, corridorColor);
        DrawRect(texture, 670, 188, 84, 92, corridorColor);

        DrawLine(texture, new Vector2Int(24, 28), new Vector2Int(284, 28), wallColor, 3);
        DrawLine(texture, new Vector2Int(284, 28), new Vector2Int(284, 234), wallColor, 3);
        DrawLine(texture, new Vector2Int(24, 142), new Vector2Int(284, 142), lightWallColor, 2);
        DrawLine(texture, new Vector2Int(24, 234), new Vector2Int(284, 234), wallColor, 3);
        DrawLine(texture, new Vector2Int(316, 62), new Vector2Int(514, 62), wallColor, 3);
        DrawLine(texture, new Vector2Int(514, 62), new Vector2Int(514, 188), wallColor, 3);
        DrawLine(texture, new Vector2Int(316, 188), new Vector2Int(514, 188), wallColor, 3);
        DrawLine(texture, new Vector2Int(546, 108), new Vector2Int(766, 108), wallColor, 3);
        DrawLine(texture, new Vector2Int(766, 108), new Vector2Int(766, 232), wallColor, 3);
        DrawLine(texture, new Vector2Int(546, 232), new Vector2Int(766, 232), wallColor, 3);
        DrawLine(texture, new Vector2Int(792, 148), new Vector2Int(994, 148), wallColor, 3);
        DrawLine(texture, new Vector2Int(994, 148), new Vector2Int(994, 270), wallColor, 3);
        DrawLine(texture, new Vector2Int(792, 270), new Vector2Int(994, 270), wallColor, 3);
        DrawLine(texture, new Vector2Int(924, 224), new Vector2Int(1108, 224), wallColor, 3);
        DrawLine(texture, new Vector2Int(1108, 224), new Vector2Int(1108, 318), wallColor, 3);
        DrawLine(texture, new Vector2Int(924, 318), new Vector2Int(1108, 318), wallColor, 3);
        DrawLine(texture, new Vector2Int(250, 126), new Vector2Int(1020, 126), lightWallColor, 2);
        DrawLine(texture, new Vector2Int(250, 190), new Vector2Int(1020, 190), lightWallColor, 2);

        List<Vector2Int> routePoints = new List<Vector2Int>
        {
            MapRoutePointToTexture(texture, 0f, 0f),
            MapRoutePointToTexture(texture, 1.3f, 1.4f),
            MapRoutePointToTexture(texture, 2.8f, 2.6f),
            MapRoutePointToTexture(texture, 4.2f, 3.8f),
            MapRoutePointToTexture(texture, 5.4f, 5.1f),
            MapRoutePointToTexture(texture, 5.5f, 6.8f),
            MapRoutePointToTexture(texture, 4.4f, 8.2f),
            MapRoutePointToTexture(texture, 2.8f, 9.5f),
            MapRoutePointToTexture(texture, 1.3f, 11.0f)
        };

        DrawPolyline(texture, routePoints, new Color(1f, 1f, 1f, 0.94f), 18);
        DrawPolyline(texture, routePoints, new Color(0f, 0.57f, 0.68f), 10);

        for (int i = 0; i < routePoints.Count; i++)
        {
            DrawCircle(texture, routePoints[i], i == 0 ? 21 : 17, Color.white);
            DrawCircle(texture, routePoints[i], i == 0 ? 15 : 11, i == 0 ? new Color(0f, 0.57f, 0.68f) : new Color(0.62f, 0.68f, 0.74f));
        }

        texture.Apply(false, false);
        File.WriteAllBytes(MapTexturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(MapTexturePath);

        TextureImporter importer = AssetImporter.GetAtPath(MapTexturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(MapTexturePath);
    }

    private static void EnsureAndroidXRConfiguration()
    {
        XRGeneralSettingsPerBuildTarget buildTargetSettings = LoadOrCreateXRGeneralSettingsPerBuildTarget();

        if (!buildTargetSettings.HasSettingsForBuildTarget(BuildTargetGroup.Android))
        {
            buildTargetSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
        }

        if (!buildTargetSettings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
        {
            buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        }

        XRGeneralSettings generalSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
        XRManagerSettings managerSettings = buildTargetSettings.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);

        if (generalSettings == null || managerSettings == null)
        {
            throw new BuildFailedException("Failed to create Android XR general settings.");
        }

        generalSettings.InitManagerOnStart = true;
        generalSettings.AssignedSettings = managerSettings;
        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);

        bool loaderAssigned = XRPackageMetadataStore.AssignLoader(
            managerSettings,
            typeof(ARCoreLoader).FullName,
            BuildTargetGroup.Android);

        if (!loaderAssigned)
        {
            throw new BuildFailedException("Failed to assign the ARCore loader for Android.");
        }

        ARCoreSettings arCoreSettings = ARCoreSettings.GetOrCreateSettings();
        if (string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(arCoreSettings)))
        {
            AssetDatabase.CreateAsset(arCoreSettings, "Assets/XR/ARCoreSettings.asset");
        }

        arCoreSettings.requirement = ARCoreSettings.Requirement.Required;
        arCoreSettings.depth = ARCoreSettings.Requirement.Optional;
        ARCoreSettings.currentSettings = arCoreSettings;

        EditorUtility.SetDirty(buildTargetSettings);
        EditorUtility.SetDirty(generalSettings);
        EditorUtility.SetDirty(managerSettings);
        EditorUtility.SetDirty(arCoreSettings);
    }

    private static XRGeneralSettingsPerBuildTarget LoadOrCreateXRGeneralSettingsPerBuildTarget()
    {
        if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget existingSettings) &&
            existingSettings != null)
        {
            return existingSettings;
        }

        string[] assetGuids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
        if (assetGuids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[0]);
            XRGeneralSettingsPerBuildTarget loadedSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(assetPath);
            if (loadedSettings != null)
            {
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, loadedSettings, true);
                return loadedSettings;
            }
        }

        XRGeneralSettingsPerBuildTarget createdSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
        AssetDatabase.CreateAsset(createdSettings, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, createdSettings, true);
        return createdSettings;
    }

    private static void EnsureARCameraSetup(Camera arCamera)
    {
        if (arCamera == null)
        {
            return;
        }

        if (arCamera.GetComponent<ARCameraManager>() == null)
        {
            arCamera.gameObject.AddComponent<ARCameraManager>();
        }

        if (arCamera.GetComponent<ARCameraBackground>() == null)
        {
            arCamera.gameObject.AddComponent<ARCameraBackground>();
        }

        if (arCamera.GetComponent<AudioListener>() == null)
        {
            arCamera.gameObject.AddComponent<AudioListener>();
        }

        arCamera.clearFlags = CameraClearFlags.SolidColor;
        arCamera.backgroundColor = Color.black;
        arCamera.nearClipPlane = 0.01f;
        arCamera.farClipPlane = 40f;
    }

    private static void CreateFeatureCard(Transform parent, Sprite sprite, Font font, string title, string copy, Vector2 anchoredPosition)
    {
        RectTransform card = CreatePanel(title + "Card", parent, sprite, new Color(0.1f, 0.14f, 0.2f, 1f));
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(224f, 184f);
        card.anchoredPosition = anchoredPosition;

        RectTransform cardTitle = CreateText(title + "Title", card, font, title, 24, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
        cardTitle.anchorMin = new Vector2(0f, 1f);
        cardTitle.anchorMax = new Vector2(1f, 1f);
        cardTitle.pivot = new Vector2(0f, 1f);
        cardTitle.sizeDelta = new Vector2(-28f, 34f);
        cardTitle.anchoredPosition = new Vector2(16f, -18f);

        RectTransform cardCopy = CreateText(title + "Copy", card, font, copy, 18, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.82f, 0.89f, 0.96f));
        cardCopy.anchorMin = new Vector2(0f, 1f);
        cardCopy.anchorMax = new Vector2(1f, 1f);
        cardCopy.pivot = new Vector2(0f, 1f);
        cardCopy.sizeDelta = new Vector2(-28f, 108f);
        cardCopy.anchoredPosition = new Vector2(16f, -54f);
    }

    private static void CreateEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Canvas CreateCanvas(string name, RenderMode renderMode)
    {
        GameObject canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = renderMode;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        Image image = panelObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;

        return panelObject.GetComponent<RectTransform>();
    }

    private static RectTransform CreateText(string name, Transform parent, Font font, string content, int fontSize, FontStyle style, TextAnchor anchor, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return textObject.GetComponent<RectTransform>();
    }

    private static Button CreateButton(string name, Transform parent, Sprite sprite, Font font, string label, Color buttonColor)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = buttonColor;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonColor * 1.08f;
        colors.pressedColor = buttonColor * 0.92f;
        colors.selectedColor = buttonColor * 1.04f;
        colors.disabledColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.45f);
        button.colors = colors;

        RectTransform labelRect = CreateText("Text", buttonObject.transform, font, label, 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        StretchToParent(labelRect);

        return button;
    }

    private static Button CreateCircleIconButton(string name, Transform parent, Sprite circleSprite, Color buttonColor, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 0.5f);
        rectTransform.anchorMax = new Vector2(1f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(56f, 56f);
        rectTransform.anchoredPosition = anchoredPosition;

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = circleSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = buttonColor;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.28f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.42f);
        button.colors = colors;

        return button;
    }

    private static GameObject AddCameraIcon(Transform parent, Sprite uiSprite, Sprite circleSprite)
    {
        AddIconRect("CameraBody", parent, uiSprite, new Vector2(27f, 17f), new Vector2(0f, -2f), Color.white, 0f);
        AddIconRect("CameraTop", parent, uiSprite, new Vector2(13f, 5f), new Vector2(-6f, 10f), Color.white, 0f);
        AddIconCircle("CameraLens", parent, circleSprite, new Vector2(10f, 10f), new Vector2(0f, -2f), new Color(0.04f, 0.06f, 0.09f, 1f));
        return AddSlashIcon(parent, uiSprite);
    }

    private static GameObject AddMuteIcon(Transform parent, Sprite uiSprite, Sprite circleSprite)
    {
        AddIconRect("SpeakerBack", parent, uiSprite, new Vector2(8f, 14f), new Vector2(-10f, -1f), Color.white, 0f);
        AddIconRect("SpeakerCone", parent, uiSprite, new Vector2(15f, 15f), new Vector2(-2f, -1f), Color.white, 45f);
        AddIconRect("SoundWaveTop", parent, uiSprite, new Vector2(3f, 14f), new Vector2(13f, 5f), Color.white, -28f);
        AddIconRect("SoundWaveBottom", parent, uiSprite, new Vector2(3f, 14f), new Vector2(13f, -7f), Color.white, 28f);
        return AddSlashIcon(parent, uiSprite);
    }

    private static GameObject AddSlashIcon(Transform parent, Sprite uiSprite)
    {
        GameObject slashObject = new GameObject("OffSlash", typeof(RectTransform));
        slashObject.transform.SetParent(parent, false);

        RectTransform slashRoot = slashObject.GetComponent<RectTransform>();
        slashRoot.anchorMin = new Vector2(0.5f, 0.5f);
        slashRoot.anchorMax = new Vector2(0.5f, 0.5f);
        slashRoot.pivot = new Vector2(0.5f, 0.5f);
        slashRoot.sizeDelta = new Vector2(44f, 44f);
        slashRoot.anchoredPosition = Vector2.zero;

        AddIconRect("SlashLine", slashRoot, uiSprite, new Vector2(43f, 5f), Vector2.zero, new Color(1f, 0.24f, 0.28f, 1f), -45f);
        slashObject.SetActive(false);
        return slashObject;
    }

    private static Image AddIconRect(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 position, Color color, float zRotation)
    {
        RectTransform rect = CreatePanel(name, parent, sprite, color);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localEulerAngles = new Vector3(0f, 0f, zRotation);
        Image image = rect.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static Image AddIconCircle(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 position, Color color)
    {
        GameObject iconObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(parent, false);

        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = iconObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void SetButtonLabelColor(Button button, Color color)
    {
        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.color = color;
        }
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void SetAnchoredRect(RectTransform rectTransform, Vector2 anchor, Vector2 size, Vector2 offset)
    {
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = offset;
    }

    private static Sprite GetDefaultUISprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static Sprite GetCircleUISprite(Sprite fallback)
    {
        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        return circleSprite != null ? circleSprite : fallback;
    }

    private static Font GetDefaultFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void FillTexture(Texture2D texture, Color color)
    {
        Color[] pixels = new Color[texture.width * texture.height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels(pixels);
    }

    private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        int minX = Mathf.Clamp(x, 0, texture.width - 1);
        int minY = Mathf.Clamp(y, 0, texture.height - 1);
        int maxX = Mathf.Clamp(x + width, 0, texture.width);
        int maxY = Mathf.Clamp(y + height, 0, texture.height);

        for (int px = minX; px < maxX; px++)
        {
            for (int py = minY; py < maxY; py++)
            {
                texture.SetPixel(px, py, color);
            }
        }
    }

    private static void DrawPolyline(Texture2D texture, IReadOnlyList<Vector2Int> points, Color color, int thickness)
    {
        for (int i = 0; i < points.Count - 1; i++)
        {
            DrawLine(texture, points[i], points[i + 1], color, thickness);
        }
    }

    private static void DrawLine(Texture2D texture, Vector2Int start, Vector2Int end, Color color, int thickness)
    {
        int steps = Mathf.Max(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y));
        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0f : i / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t));

            for (int ox = -thickness; ox <= thickness; ox++)
            {
                for (int oy = -thickness; oy <= thickness; oy++)
                {
                    int drawX = Mathf.Clamp(x + ox, 0, texture.width - 1);
                    int drawY = Mathf.Clamp(y + oy, 0, texture.height - 1);
                    texture.SetPixel(drawX, drawY, color);
                }
            }
        }
    }

    private static void DrawCircle(Texture2D texture, Vector2Int center, int radius, Color color)
    {
        int radiusSquared = radius * radius;
        int minX = Mathf.Max(0, center.x - radius);
        int maxX = Mathf.Min(texture.width - 1, center.x + radius);
        int minY = Mathf.Max(0, center.y - radius);
        int maxY = Mathf.Min(texture.height - 1, center.y + radius);

        for (int x = minX; x <= maxX; x++)
        {
            int dx = x - center.x;
            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - center.y;
                if ((dx * dx) + (dy * dy) <= radiusSquared)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static Vector2Int MapRoutePointToTexture(Texture2D texture, float localX, float localZ)
    {
        float normalizedX = Mathf.InverseLerp(RouteMapMin.x, RouteMapMax.x, localX);
        float normalizedY = Mathf.InverseLerp(RouteMapMin.y, RouteMapMax.y, localZ);
        return new Vector2Int(
            Mathf.RoundToInt(normalizedX * (texture.width - 1)),
            Mathf.RoundToInt(normalizedY * (texture.height - 1)));
    }

    private struct GalleryPhotoImportData
    {
        public string Title;
        public string Definition;
        public Sprite Sprite;
    }

    private struct ScanItemImportData
    {
        public string Id;
        public string Name;
        public string Description;
        public Sprite PreviewSprite;
        public Texture2D ReferenceTexture;
        public Texture2D QrMarkerTexture;
        public AudioClip AudioClip;
        public float PhysicalWidthMeters;
        public string AverageHashHex;
        public string DifferenceHashHex;
        public float AspectRatio;
        public bool UseForTrackedImage;
    }

    private struct QrScanUi
    {
        public GameObject Overlay;
        public Button CloseButton;
        public Text HintText;
    }

    private struct ScanFingerprintImportData
    {
        public string AverageHashHex;
        public string DifferenceHashHex;
        public float AspectRatio;
    }

    private struct ImageFingerprintImport
    {
        public ulong AverageHash;
        public ulong DifferenceHash;
        public float AspectRatio;
    }

    [Serializable]
    private sealed class GalleryMetadataCollection
    {
        public GalleryMetadataEntry[] entries = Array.Empty<GalleryMetadataEntry>();
    }

    [Serializable]
    private sealed class GalleryMetadataEntry
    {
        public string fileName;
        public string title;
        public string description;
    }

    private readonly struct TourLocationDefinition
    {
        public TourLocationDefinition(string name, string description, int order, Vector3 position, float reachDistance, params Vector3[] approachPoints)
        {
            Name = name;
            Description = description;
            Order = order;
            Position = position;
            ReachDistance = reachDistance;
            ApproachPoints = approachPoints ?? System.Array.Empty<Vector3>();
        }

        public string Name { get; }
        public string Description { get; }
        public int Order { get; }
        public Vector3 Position { get; }
        public float ReachDistance { get; }
        public IReadOnlyList<Vector3> ApproachPoints { get; }
    }
}
