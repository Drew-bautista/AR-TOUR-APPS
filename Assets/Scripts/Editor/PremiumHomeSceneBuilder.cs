#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PremiumHomeSceneBuilder
{
    private const string HomeScenePath = "Assets/Scenes/HomeScene.unity";
    private const string GalleryPhotoFolder = "Assets/all pictures AR";
    private const string GeneratedFolder = "Assets/Art/Generated/HomeUI";
    private const string GradientPath = GeneratedFolder + "/HomeGradient.png";
    private const string VignettePath = GeneratedFolder + "/HomeVignette.png";
    private const string GlowPath = GeneratedFolder + "/HomeGlow.png";
    private const string RoundedCardPath = GeneratedFolder + "/RoundedCard.png";
    private const string RoundedPillPath = GeneratedFolder + "/RoundedPill.png";
    private const string RoundedBoxPath = GeneratedFolder + "/RoundedBox.png";
    private const string CtaGradientPath = GeneratedFolder + "/CtaGradient.png";
    private const string AccentLinePath = GeneratedFolder + "/AccentLine.png";
    private const string NavigationIconPath = GeneratedFolder + "/IconNavigation.png";
    private const string ScanIconPath = GeneratedFolder + "/IconScan.png";
    private const string MapIconPath = GeneratedFolder + "/IconMap.png";
    private const string ChevronIconPath = GeneratedFolder + "/IconChevron.png";
    private const string ShrineBlurPath = GeneratedFolder + "/ShrineBlur.png";
    private const string TitleFontPath = GeneratedFolder + "/AguinaldoTitle.ttf";
    private const string BodyFontPath = GeneratedFolder + "/AguinaldoBody.ttf";
    private const string CanvasFadeClipPath = GeneratedFolder + "/HomeCanvasFadeIn.anim";
    private const string AnimatorControllerPath = GeneratedFolder + "/HomeCanvasAnimator.controller";

    private static readonly Color32 PrimaryBlue = new Color32(0x0A, 0x1F, 0x44, 0xFF);
    private static readonly Color32 AccentBlue = new Color32(0x3B, 0x82, 0xF6, 0xFF);
    private static readonly Color32 AccentBlueSoft = new Color32(0x60, 0xA5, 0xFA, 0xFF);
    private static readonly Color32 DeepBlack = new Color32(0x03, 0x07, 0x12, 0xFF);

    private struct HomeUiAssets
    {
        public Sprite Gradient;
        public Sprite Vignette;
        public Sprite Glow;
        public Sprite RoundedCard;
        public Sprite RoundedPill;
        public Sprite RoundedBox;
        public Sprite CtaGradient;
        public Sprite AccentLine;
        public Sprite NavigationIcon;
        public Sprite ScanIcon;
        public Sprite MapIcon;
        public Sprite ChevronIcon;
        public Sprite ShrineBlur;
        public Font TitleFont;
        public Font BodyFont;
        public RuntimeAnimatorController CanvasAnimator;
    }

    [MenuItem("Tools/Aguinaldo Shrine AR Tour/Apply Premium Home Screen")]
    public static void CreateOrReplaceHomeScene()
    {
        EnsureFolder("Assets/Art");
        EnsureFolder("Assets/Art/Generated");
        EnsureFolder(GeneratedFolder);
        EnsureFolder("Assets/Scenes");

        HomeUiAssets assets = EnsureAssets();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildScene(scene, assets);
        EditorSceneManager.SaveScene(scene, HomeScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!Application.isBatchMode)
        {
            EditorSceneManager.OpenScene(HomeScenePath);
        }
    }

    private static void BuildScene(Scene scene, HomeUiAssets assets)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            UnityEngine.Object.DestroyImmediate(rootObject);
        }

        CreateMainCamera();
        CreateEventSystem();

        Canvas canvas = CreateCanvas("HomeCanvas");
        CanvasGroup canvasGroup = canvas.gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (assets.CanvasAnimator != null)
        {
            Animator animator = canvas.gameObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = canvas.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = assets.CanvasAnimator;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);
        }

        GameObject controllerObject = new GameObject("PremiumHomeScreen");
        PremiumHomeUIController controller = controllerObject.AddComponent<PremiumHomeUIController>();
        controller.gradientBackground = assets.Gradient;
        controller.vignetteSprite = assets.Vignette;
        controller.ambientBloomSprite = assets.Glow;
        controller.shrineSilhouette = assets.ShrineBlur;
        controller.roundedCardSprite = assets.RoundedCard;
        controller.roundedPillSprite = assets.RoundedPill;
        controller.roundedBoxSprite = assets.RoundedBox;
        controller.ctaGradientSprite = assets.CtaGradient;
        controller.accentLineSprite = assets.AccentLine;
        controller.glowRadialSprite = assets.Glow;
        controller.iconNavigation = assets.NavigationIcon;
        controller.iconScan = assets.ScanIcon;
        controller.iconMap = assets.MapIcon;
        controller.iconChevron = assets.ChevronIcon;
        controller.titleFont = assets.TitleFont;
        controller.bodyFont = assets.BodyFont;
        controller.arSceneName = "AguinaldoShrineARTour";
        controller.buildOnStart = true;
        controller.BuildAll();

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void CreateMainCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(0x04, 0x08, 0x12, 0xFF);
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Canvas CreateCanvas(string name)
    {
        GameObject canvasObject = new GameObject(
            name,
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2340f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static HomeUiAssets EnsureAssets()
    {
        WriteSpriteTexture(
            GradientPath,
            CreateVerticalGradient(1080, 2340, PrimaryBlue, DeepBlack),
            Vector4.zero,
            FilterMode.Bilinear);

        WriteSpriteTexture(
            VignettePath,
            CreateVignetteTexture(1080, 2340),
            Vector4.zero,
            FilterMode.Bilinear);

        WriteSpriteTexture(
            GlowPath,
            CreateGlowTexture(1024),
            Vector4.zero,
            FilterMode.Bilinear);

        WriteSpriteTexture(
            RoundedCardPath,
            CreateRoundedRectTexture(256, 256, 42f, 2.25f),
            new Vector4(42f, 42f, 42f, 42f),
            FilterMode.Bilinear);

        WriteSpriteTexture(
            RoundedPillPath,
            CreateRoundedRectTexture(512, 256, 110f, 2.25f),
            new Vector4(110f, 110f, 110f, 110f),
            FilterMode.Bilinear);

        WriteSpriteTexture(
            RoundedBoxPath,
            CreateRoundedRectTexture(256, 128, 18f, 2.25f),
            new Vector4(18f, 18f, 18f, 18f),
            FilterMode.Bilinear);

        WriteSpriteTexture(
            CtaGradientPath,
            CreateHorizontalGradient(1024, 256, new Color32(0x1C, 0x4E, 0xA8, 0xFF), AccentBlue),
            Vector4.zero,
            FilterMode.Bilinear);

        WriteSpriteTexture(
            AccentLinePath,
            CreateAccentLineTexture(512, 12),
            Vector4.zero,
            FilterMode.Bilinear);

        WriteSpriteTexture(
            NavigationIconPath,
            CreateNavigationIconTexture(256),
            Vector4.zero,
            FilterMode.Bilinear);

        WriteSpriteTexture(
            ScanIconPath,
            CreateCameraIconTexture(256),
            Vector4.zero,
            FilterMode.Bilinear);

        WriteSpriteTexture(
            MapIconPath,
            CreateMapIconTexture(256),
            Vector4.zero,
            FilterMode.Bilinear);

        WriteSpriteTexture(
            ChevronIconPath,
            CreateChevronIconTexture(128),
            Vector4.zero,
            FilterMode.Bilinear);

        WriteSpriteTexture(
            ShrineBlurPath,
            CreateShrineOverlayTexture(),
            Vector4.zero,
            FilterMode.Bilinear,
            maxTextureSize: 2048);

        Font titleFont = EnsureFontAsset(
            TitleFontPath,
            new[]
            {
                @"C:\Windows\Fonts\seguisb.ttf",
                @"C:\Windows\Fonts\segoeuib.ttf",
                @"C:\Windows\Fonts\arialbd.ttf"
            });

        Font bodyFont = EnsureFontAsset(
            BodyFontPath,
            new[]
            {
                @"C:\Windows\Fonts\segoeui.ttf",
                @"C:\Windows\Fonts\arial.ttf"
            });

        RuntimeAnimatorController canvasAnimator = EnsureCanvasAnimator();

        AssetDatabase.Refresh();

        return new HomeUiAssets
        {
            Gradient = AssetDatabase.LoadAssetAtPath<Sprite>(GradientPath),
            Vignette = AssetDatabase.LoadAssetAtPath<Sprite>(VignettePath),
            Glow = AssetDatabase.LoadAssetAtPath<Sprite>(GlowPath),
            RoundedCard = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedCardPath),
            RoundedPill = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPillPath),
            RoundedBox = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedBoxPath),
            CtaGradient = AssetDatabase.LoadAssetAtPath<Sprite>(CtaGradientPath),
            AccentLine = AssetDatabase.LoadAssetAtPath<Sprite>(AccentLinePath),
            NavigationIcon = AssetDatabase.LoadAssetAtPath<Sprite>(NavigationIconPath),
            ScanIcon = AssetDatabase.LoadAssetAtPath<Sprite>(ScanIconPath),
            MapIcon = AssetDatabase.LoadAssetAtPath<Sprite>(MapIconPath),
            ChevronIcon = AssetDatabase.LoadAssetAtPath<Sprite>(ChevronIconPath),
            ShrineBlur = AssetDatabase.LoadAssetAtPath<Sprite>(ShrineBlurPath),
            TitleFont = titleFont,
            BodyFont = bodyFont,
            CanvasAnimator = canvasAnimator
        };
    }

    private static RuntimeAnimatorController EnsureCanvasAnimator()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CanvasFadeClipPath);
        if (clip == null)
        {
            clip = new AnimationClip
            {
                name = "HomeCanvasFadeIn"
            };

            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.7f, 1f));

            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(string.Empty, typeof(CanvasGroup), "m_Alpha");
            AnimationUtility.SetEditorCurve(clip, binding, curve);
            AssetDatabase.CreateAsset(clip, CanvasFadeClipPath);
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState visibleState = stateMachine.AddState("Visible");
            visibleState.motion = clip;
            stateMachine.defaultState = visibleState;
        }

        return controller;
    }

    private static Font EnsureFontAsset(string assetPath, string[] fontFileCandidates)
    {
        Font existing = AssetDatabase.LoadAssetAtPath<Font>(assetPath);
        if (existing != null)
        {
            return existing;
        }

        string fontFilePath = fontFileCandidates.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(fontFilePath))
        {
            Font fallbackFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (fallbackFont == null)
            {
                throw new FileNotFoundException("No usable font file was found for home screen font import.", assetPath);
            }

            return fallbackFont;
        }

        string absoluteAssetPath = GetAbsoluteProjectPath(assetPath);
        string assetDirectory = Path.GetDirectoryName(absoluteAssetPath);
        if (!string.IsNullOrWhiteSpace(assetDirectory))
        {
            Directory.CreateDirectory(assetDirectory);
        }

        File.Copy(fontFilePath, absoluteAssetPath, true);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        Font importedFont = AssetDatabase.LoadAssetAtPath<Font>(assetPath);
        if (importedFont != null)
        {
            return importedFont;
        }

        Font builtInFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (builtInFont == null)
        {
            throw new InvalidOperationException("Unable to load the imported font asset or the built-in Arial fallback.");
        }

        return builtInFont;
    }

    private static void WriteSpriteTexture(
        string assetPath,
        Texture2D texture,
        Vector4 border,
        FilterMode filterMode,
        int maxTextureSize = 2048)
    {
        byte[] pngBytes = texture.EncodeToPNG();
        File.WriteAllBytes(assetPath, pngBytes);
        UnityEngine.Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = border;
        importer.filterMode = filterMode;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = maxTextureSize;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void EnsureFolder(string path)
    {
        string[] segments = path.Split('/');
        string current = segments[0];

        for (int i = 1; i < segments.Length; i++)
        {
            string next = current + "/" + segments[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[i]);
            }

            current = next;
        }
    }

    private static string GetAbsoluteProjectPath(string assetPath)
    {
        return Path.Combine(
            Directory.GetCurrentDirectory(),
            assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
    }

    private static Texture2D CreateVerticalGradient(int width, int height, Color topColor, Color bottomColor)
    {
        Texture2D texture = CreateTransparentTexture(width, height);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float t = y / (float)(height - 1);
            float curve = Mathf.SmoothStep(0f, 1f, t);
            Color rowColor = Color.Lerp(bottomColor, topColor, curve);
            rowColor = Color.Lerp(rowColor, DeepBlack, Mathf.Pow(1f - t, 2f) * 0.1f);

            int rowStart = y * width;
            for (int x = 0; x < width; x++)
            {
                float vignette = Mathf.Abs((x / (float)(width - 1)) - 0.5f) * 0.08f;
                pixels[rowStart + x] = Color.Lerp(rowColor, DeepBlack, vignette);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateHorizontalGradient(int width, int height, Color leftColor, Color rightColor)
    {
        Texture2D texture = CreateTransparentTexture(width, height);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float sheen = Mathf.SmoothStep(0.95f, 0.1f, y / (float)(height - 1)) * 0.18f;
            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                Color color = Color.Lerp(leftColor, rightColor, Mathf.SmoothStep(0f, 1f, t));
                color = Color.Lerp(color, Color.white, sheen);
                pixels[(y * width) + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateAccentLineTexture(int width, int height)
    {
        Texture2D texture = CreateTransparentTexture(width, height);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float lineMask = 1f - Mathf.Abs(((y / (float)(height - 1)) - 0.5f) * 2f);
            lineMask = Mathf.Pow(Mathf.Clamp01(lineMask), 1.5f);

            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                float sideFade = 1f - Mathf.Abs((t - 0.5f) * 2f);
                float alpha = Mathf.Clamp01(Mathf.Pow(sideFade, 1.2f) * lineMask);
                pixels[(y * width) + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateVignetteTexture(int width, int height)
    {
        Texture2D texture = CreateTransparentTexture(width, height);
        Color[] pixels = new Color[width * height];
        Vector2 center = new Vector2(width * 0.5f, height * 0.52f);
        float radius = Mathf.Max(width, height) * 0.56f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(Mathf.InverseLerp(0.38f, 1.05f, distance));
                alpha = Mathf.Pow(alpha, 1.35f) * 0.96f;
                pixels[(y * width) + x] = new Color(0f, 0f, 0f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateGlowTexture(int size)
    {
        Texture2D texture = CreateTransparentTexture(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - Mathf.Pow(distance, 1.8f));
                alpha = Mathf.Pow(alpha, 2.2f);
                pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateRoundedRectTexture(int width, int height, float radius, float feather)
    {
        Texture2D texture = CreateTransparentTexture(width, height);
        Color[] pixels = new Color[width * height];
        Vector2 halfSize = new Vector2(width * 0.5f, height * 0.5f);
        Vector2 boxExtents = new Vector2((width * 0.5f) - 1f, (height * 0.5f) - 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 point = new Vector2((x + 0.5f) - halfSize.x, (y + 0.5f) - halfSize.y);
                Vector2 q = Abs(point) - boxExtents + new Vector2(radius, radius);
                Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
                float signedDistance = outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                float alpha = Mathf.Clamp01((feather - signedDistance) / (feather * 2f));
                pixels[(y * width) + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateNavigationIconTexture(int size)
    {
        Texture2D texture = CreateTransparentTexture(size, size);
        FillTriangle(
            texture,
            new Vector2(size * 0.5f, size * 0.92f),
            new Vector2(size * 0.22f, size * 0.18f),
            new Vector2(size * 0.78f, size * 0.34f),
            Color.white);

        FillTriangle(
            texture,
            new Vector2(size * 0.5f, size * 0.72f),
            new Vector2(size * 0.42f, size * 0.4f),
            new Vector2(size * 0.62f, size * 0.46f),
            new Color(0f, 0f, 0f, 1f));

        texture.Apply();
        return texture;
    }

    private static Texture2D CreateCameraIconTexture(int size)
    {
        Texture2D texture = CreateTransparentTexture(size, size);
        FillRoundedRect(texture, new Rect(size * 0.14f, size * 0.22f, size * 0.72f, size * 0.5f), size * 0.08f, Color.white);
        FillRoundedRect(texture, new Rect(size * 0.24f, size * 0.62f, size * 0.18f, size * 0.12f), size * 0.04f, Color.white);
        FillCircle(texture, new Vector2(size * 0.5f, size * 0.47f), size * 0.16f, new Color(0f, 0f, 0f, 1f));
        StrokeCircle(texture, new Vector2(size * 0.5f, size * 0.47f), size * 0.18f, size * 0.035f, Color.white);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateMapIconTexture(int size)
    {
        Texture2D texture = CreateTransparentTexture(size, size);
        FillQuad(
            texture,
            new Vector2(size * 0.15f, size * 0.18f),
            new Vector2(size * 0.42f, size * 0.26f),
            new Vector2(size * 0.42f, size * 0.82f),
            new Vector2(size * 0.15f, size * 0.74f),
            Color.white);

        FillQuad(
            texture,
            new Vector2(size * 0.42f, size * 0.26f),
            new Vector2(size * 0.64f, size * 0.18f),
            new Vector2(size * 0.64f, size * 0.74f),
            new Vector2(size * 0.42f, size * 0.82f),
            Color.white);

        FillQuad(
            texture,
            new Vector2(size * 0.64f, size * 0.18f),
            new Vector2(size * 0.85f, size * 0.26f),
            new Vector2(size * 0.85f, size * 0.82f),
            new Vector2(size * 0.64f, size * 0.74f),
            Color.white);

        StrokeCircle(texture, new Vector2(size * 0.34f, size * 0.38f), size * 0.06f, size * 0.028f, new Color(0f, 0f, 0f, 1f));
        DrawThickLine(texture, new Vector2(size * 0.34f, size * 0.38f), new Vector2(size * 0.55f, size * 0.52f), size * 0.03f, new Color(0f, 0f, 0f, 1f));
        DrawThickLine(texture, new Vector2(size * 0.55f, size * 0.52f), new Vector2(size * 0.73f, size * 0.36f), size * 0.03f, new Color(0f, 0f, 0f, 1f));

        texture.Apply();
        return texture;
    }

    private static Texture2D CreateChevronIconTexture(int size)
    {
        Texture2D texture = CreateTransparentTexture(size, size);
        DrawThickLine(texture, new Vector2(size * 0.28f, size * 0.18f), new Vector2(size * 0.72f, size * 0.5f), size * 0.12f, Color.white);
        DrawThickLine(texture, new Vector2(size * 0.28f, size * 0.82f), new Vector2(size * 0.72f, size * 0.5f), size * 0.12f, Color.white);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateShrineOverlayTexture()
    {
        string sourceAssetPath = FindFirstGalleryImageAssetPath();
        if (string.IsNullOrWhiteSpace(sourceAssetPath))
        {
            return CreateFallbackShrineTexture(1200, 1200);
        }

        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourceAssetPath);
        if (source == null)
        {
            return CreateFallbackShrineTexture(1200, 1200);
        }

        int lowWidth = 96;
        int lowHeight = Mathf.Clamp(Mathf.RoundToInt(lowWidth * (source.height / Mathf.Max(1f, (float)source.width))), 96, 160);
        Color[] lowRes = SampleTexture(source, lowWidth, lowHeight);
        int targetWidth = 1200;
        int targetHeight = Mathf.Clamp(Mathf.RoundToInt(targetWidth * (source.height / Mathf.Max(1f, (float)source.width))), 900, 1800);
        Texture2D output = CreateTransparentTexture(targetWidth, targetHeight);
        Color[] pixels = new Color[targetWidth * targetHeight];

        for (int y = 0; y < targetHeight; y++)
        {
            float v = y / (float)(targetHeight - 1);
            float verticalFade = Mathf.SmoothStep(0f, 0.45f, 1f - Mathf.Abs((v - 0.54f) * 1.5f));

            for (int x = 0; x < targetWidth; x++)
            {
                float u = x / (float)(targetWidth - 1);
                Color sampled = BilinearSample(lowRes, lowWidth, lowHeight, u, v);
                float luminance = (sampled.r * 0.299f) + (sampled.g * 0.587f) + (sampled.b * 0.114f);
                Color desaturated = Color.Lerp(new Color(luminance, luminance, luminance, 1f), sampled, 0.18f);
                Color graded = Color.Lerp(new Color(0.06f, 0.10f, 0.16f, 1f), desaturated, 0.48f);
                graded *= 0.8f;
                graded.a = verticalFade;
                pixels[(y * targetWidth) + x] = graded;
            }
        }

        output.SetPixels(pixels);
        output.Apply();
        return output;
    }

    private static string FindFirstGalleryImageAssetPath()
    {
        if (!AssetDatabase.IsValidFolder(GalleryPhotoFolder))
        {
            return string.Empty;
        }

        string absoluteFolderPath = Path.Combine(Directory.GetCurrentDirectory(), GalleryPhotoFolder.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (!Directory.Exists(absoluteFolderPath))
        {
            return string.Empty;
        }

        string filePath = Directory
            .GetFiles(absoluteFolderPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(path =>
            {
                string extension = Path.GetExtension(path);
                return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return string.Empty;
        }

        return GalleryPhotoFolder + "/" + Path.GetFileName(filePath);
    }

    private static Texture2D CreateFallbackShrineTexture(int width, int height)
    {
        Texture2D texture = CreateTransparentTexture(width, height);
        Color baseColor = new Color(0.11f, 0.16f, 0.22f, 1f);
        FillRoundedRect(texture, new Rect(width * 0.18f, height * 0.3f, width * 0.64f, height * 0.28f), 48f, baseColor);
        FillRoundedRect(texture, new Rect(width * 0.12f, height * 0.22f, width * 0.76f, height * 0.08f), 28f, baseColor * 0.9f);
        FillRoundedRect(texture, new Rect(width * 0.3f, height * 0.58f, width * 0.4f, height * 0.18f), 38f, baseColor * 1.05f);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateTransparentTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = Enumerable.Repeat(new Color(0f, 0f, 0f, 0f), width * height).ToArray();
        texture.SetPixels(pixels);
        return texture;
    }

    private static Vector2 Abs(Vector2 value)
    {
        return new Vector2(Mathf.Abs(value.x), Mathf.Abs(value.y));
    }

    private static Color[] SampleTexture(Texture2D source, int width, int height)
    {
        Color[] samples = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                samples[(y * width) + x] = source.GetPixelBilinear(u, v);
            }
        }

        return samples;
    }

    private static Color BilinearSample(Color[] pixels, int width, int height, float u, float v)
    {
        float x = Mathf.Clamp01(u) * (width - 1);
        float y = Mathf.Clamp01(v) * (height - 1);

        int xMin = Mathf.FloorToInt(x);
        int yMin = Mathf.FloorToInt(y);
        int xMax = Mathf.Min(xMin + 1, width - 1);
        int yMax = Mathf.Min(yMin + 1, height - 1);

        float tx = x - xMin;
        float ty = y - yMin;

        Color bottom = Color.Lerp(pixels[(yMin * width) + xMin], pixels[(yMin * width) + xMax], tx);
        Color top = Color.Lerp(pixels[(yMax * width) + xMin], pixels[(yMax * width) + xMax], tx);
        return Color.Lerp(bottom, top, ty);
    }

    private static void FillRoundedRect(Texture2D texture, Rect rect, float radius, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(rect.xMin));
        int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(rect.xMax));
        int minY = Mathf.Max(0, Mathf.FloorToInt(rect.yMin));
        int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(rect.yMax));

        Vector2 center = rect.center;
        Vector2 half = rect.size * 0.5f;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                Vector2 q = Abs(p) - half + new Vector2(radius, radius);
                Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
                float distance = outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                if (distance <= 0f)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void FillCircle(Texture2D texture, Vector2 center, float radius, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(center.x + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(center.y + radius));
        float radiusSquared = radius * radius;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                if ((point - center).sqrMagnitude <= radiusSquared)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void StrokeCircle(Texture2D texture, Vector2 center, float radius, float thickness, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(center.x + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(center.y + radius));
        float outer = radius * radius;
        float inner = Mathf.Max(0f, radius - thickness);
        float innerSquared = inner * inner;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                float distanceSquared = (point - center).sqrMagnitude;
                if (distanceSquared <= outer && distanceSquared >= innerSquared)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void DrawThickLine(Texture2D texture, Vector2 from, Vector2 to, float thickness, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(from.x, to.x) - thickness));
        int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(Mathf.Max(from.x, to.x) + thickness));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(from.y, to.y) - thickness));
        int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(Mathf.Max(from.y, to.y) + thickness));
        Vector2 direction = to - from;
        float lengthSquared = direction.sqrMagnitude;

        if (lengthSquared < 0.0001f)
        {
            FillCircle(texture, from, thickness * 0.5f, color);
            return;
        }

        float radiusSquared = (thickness * 0.5f) * (thickness * 0.5f);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                float t = Mathf.Clamp01(Vector2.Dot(point - from, direction) / lengthSquared);
                Vector2 projection = from + (direction * t);
                if ((point - projection).sqrMagnitude <= radiusSquared)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void FillTriangle(Texture2D texture, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
        int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
        int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));

        float denominator = ((b.y - c.y) * (a.x - c.x)) + ((c.x - b.x) * (a.y - c.y));
        if (Mathf.Abs(denominator) < 0.0001f)
        {
            return;
        }

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                float alpha = (((b.y - c.y) * (point.x - c.x)) + ((c.x - b.x) * (point.y - c.y))) / denominator;
                float beta = (((c.y - a.y) * (point.x - c.x)) + ((a.x - c.x) * (point.y - c.y))) / denominator;
                float gamma = 1f - alpha - beta;

                if (alpha >= 0f && beta >= 0f && gamma >= 0f)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void FillQuad(Texture2D texture, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
    {
        FillTriangle(texture, a, b, c, color);
        FillTriangle(texture, a, c, d, color);
    }
}
#endif
