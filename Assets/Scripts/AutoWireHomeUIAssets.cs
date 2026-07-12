using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// Editor helper: assign generated placeholder sprites to HomeScreenUIController in the current scene.
// Usage (in Unity Editor): Place a GameObject with HomeScreenUIController and run Tools -> Wire Home UI Assets

public static class AutoWireHomeUIAssets
{
#if UNITY_EDITOR
    [MenuItem("Tools/Wire Home UI Assets")]
    public static void Wire()
    {
        string basePath = "Assets/Art/UI/";
        var controller = GameObject.FindObjectOfType<HomeScreenUIController>();
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Wire Home UI Assets", "No HomeScreenUIController found in the scene.\n\nAdd the HomeScreenUIController component to a GameObject and try again.", "OK");
            return;
        }

        controller.backgroundGradientSprite = AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "gradient_bg.png");
        controller.glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "glow_radial.png");
        controller.iconArrow = AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "icon_arrow.png");
        controller.iconCamera = AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "icon_camera.png");
        controller.iconMap = AssetDatabase.LoadAssetAtPath<Sprite>(basePath + "icon_map.png");

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Wire Home UI Assets", "Placeholder sprites assigned to HomeScreenUIController.\nYou may still need to assign tmpFont manually.", "OK");
    }
#endif
}
