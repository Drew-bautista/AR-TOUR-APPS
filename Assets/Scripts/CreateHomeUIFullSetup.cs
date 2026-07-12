#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

// Editor utility: Create full Home UI prefab with animator and simple animations.
// Menu: Tools -> Create Full Home UI Prefab

public static class CreateHomeUIFullSetup
{
    [MenuItem("Tools/Create Full Home UI Prefab")]
    public static void CreatePrefab()
    {
        // Ensure placeholder UI sprites exist
        GenerateHomeUIAssets.GenerateAssets();

        // Find or create HomeScreenUIController in the scene
        HomeScreenUIController controller = GameObject.FindObjectOfType<HomeScreenUIController>();
        GameObject go;
        if (controller == null)
        {
            go = new GameObject("HomeScreenUI");
            controller = go.AddComponent<HomeScreenUIController>();
        }
        else go = controller.gameObject;

        string basePath = "Assets/Art/UI/";
        controller.backgroundGradientSprite = AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "gradient_bg.png");
        controller.glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "glow_radial.png");
        controller.iconArrow = AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "icon_arrow.png");
        controller.iconCamera = AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "icon_camera.png");
        controller.iconMap = AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "icon_map.png");

        // Assign first available TMP font asset if present
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (guids != null && guids.Length > 0)
        {
            string fontPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            controller.tmpFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(fontPath);
        }

        EditorUtility.SetDirty(controller);

        // Build UI now (will delete previous generated children first)
        controller.BuildNow();

        // Locate the Canvas used by the controller
        Canvas canvas = controller.parentCanvas != null ? controller.parentCanvas : GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found after BuildNow(). Aborting animator/prefab creation.");
            return;
        }

        // Create simple AnimatorController and a Canvas fade-in clip
        string animControllerPath = basePath + "HomeUI_Animator.controller";
        AnimatorController animatorController = AnimatorController.CreateAnimatorControllerAtPath(animControllerPath);

        AnimationClip fadeClip = new AnimationClip();
        fadeClip.name = "CanvasFadeIn";
        // Animate CanvasGroup.m_Alpha from 0 to 1 over 0.6s
        var curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.6f, 1f));
        var binding = EditorCurveBinding.FloatCurve("", typeof(CanvasGroup), "m_Alpha");
        AnimationUtility.SetEditorCurve(fadeClip, binding, curve);

        AssetDatabase.CreateAsset(fadeClip, basePath + "CanvasFadeIn.anim");

        // Add clip as default state
        var rootSM = animatorController.layers[0].stateMachine;
        var visibleState = rootSM.AddState("Visible");
        visibleState.motion = fadeClip;

        // Attach Animator to the Canvas and assign controller
        Animator animator = canvas.gameObject.GetComponent<Animator>();
        if (animator == null) animator = canvas.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = animatorController;

        // Save assets
        AssetDatabase.SaveAssets();

        // Create prefab of the Home UI root (prefer saving the HomeScreenUI GameObject if it contains the UI)
        string prefabPath = basePath + "HomeUI.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.UserAction);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Create Home UI Prefab", "Home UI prefab created at: " + prefabPath, "OK");
    }
}
#endif
