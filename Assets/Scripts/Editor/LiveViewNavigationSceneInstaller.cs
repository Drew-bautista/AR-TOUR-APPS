using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

public static class LiveViewNavigationSceneInstaller
{
    private const string TourScenePath = "Assets/Scenes/AguinaldoShrineARTour.unity";
    private const string ArrowMaterialPath = "Assets/Art/Generated/AguinaldoArrow.mat";
    private const string AccentMaterialPath = "Assets/Art/Generated/AguinaldoGuideAccent.mat";

    [MenuItem("Tools/Aguinaldo Shrine AR Tour/Install Live View Navigation")]
    public static void InstallLiveViewNavigation()
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(TourScenePath);

        ARSession arSession = Object.FindFirstObjectByType<ARSession>();
        XROrigin xrOrigin = Object.FindFirstObjectByType<XROrigin>();
        Camera arCamera = xrOrigin != null && xrOrigin.Camera != null ? xrOrigin.Camera : Camera.main;

        if (arSession == null || xrOrigin == null || arCamera == null)
        {
            Debug.LogError("Cannot install Live View navigation because the AR Session, XR Origin, or AR Camera is missing.");
            return;
        }

        ARPlaneManager planeManager = EnsureComponent<ARPlaneManager>(xrOrigin.gameObject);
        ARRaycastManager raycastManager = EnsureComponent<ARRaycastManager>(xrOrigin.gameObject);
        ARAnchorManager anchorManager = EnsureComponent<ARAnchorManager>(xrOrigin.gameObject);
        planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;

        GameObject navigationObject = GameObject.Find("GoogleMapsLiveViewNavigation");
        if (navigationObject == null)
        {
            navigationObject = new GameObject("GoogleMapsLiveViewNavigation");
        }

        AudioSource audioSource = EnsureComponent<AudioSource>(navigationObject);
        audioSource.playOnAwake = false;

        GoogleMapsStyleARNavigation navigation = EnsureComponent<GoogleMapsStyleARNavigation>(navigationObject);
        ConfigureNavigation(
            navigation,
            arSession,
            xrOrigin,
            arCamera,
            anchorManager,
            planeManager,
            raycastManager,
            audioSource,
            AssetDatabase.LoadAssetAtPath<Material>(ArrowMaterialPath),
            AssetDatabase.LoadAssetAtPath<Material>(AccentMaterialPath),
            Object.FindObjectsByType<LocationTrigger>(FindObjectsSortMode.None)
                .OrderBy(location => location.SequenceOrder)
                .ToList());
        DisableLegacyCameraArrows();

        EditorUtility.SetDirty(navigationObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Installed Google Maps-style Live View AR navigation into AguinaldoShrineARTour.");
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void ConfigureNavigation(
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
        IReadOnlyList<LocationTrigger> locations)
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
        waypoints.arraySize = locations.Count;
        for (int i = 0; i < locations.Count; i++)
        {
            SerializedProperty element = waypoints.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("locationName").stringValue = locations[i].LocationName;
            element.FindPropertyRelative("localPosition").vector3Value = locations[i].transform.position;
            element.FindPropertyRelative("reachDistance").floatValue = Mathf.Max(1.1f, locations[i].ReachDistance);
            element.FindPropertyRelative("description").stringValue = locations[i].Description;
        }

        serializedNavigation.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void DisableLegacyCameraArrows()
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

            SerializedProperty arrowAnchor = serializedManager.FindProperty("arrowAnchor");
            if (arrowAnchor != null && arrowAnchor.objectReferenceValue is Transform arrowTransform)
            {
                arrowTransform.gameObject.SetActive(false);
                EditorUtility.SetDirty(arrowTransform.gameObject);
            }

            SerializedProperty floatingLabel = serializedManager.FindProperty("floatingLabel");
            if (floatingLabel != null && floatingLabel.objectReferenceValue is BillboardLabel label)
            {
                label.SetMessage(string.Empty);
                label.gameObject.SetActive(false);
                EditorUtility.SetDirty(label.gameObject);
            }

            SerializedProperty destinationBeacon = serializedManager.FindProperty("destinationBeacon");
            if (destinationBeacon != null && destinationBeacon.objectReferenceValue is Transform beaconTransform)
            {
                beaconTransform.gameObject.SetActive(false);
                EditorUtility.SetDirty(beaconTransform.gameObject);
            }

            SerializedProperty breadcrumbs = serializedManager.FindProperty("breadcrumbDots");
            if (breadcrumbs != null)
            {
                for (int dotIndex = 0; dotIndex < breadcrumbs.arraySize; dotIndex++)
                {
                    if (breadcrumbs.GetArrayElementAtIndex(dotIndex).objectReferenceValue is Transform dotTransform)
                    {
                        dotTransform.gameObject.SetActive(false);
                        EditorUtility.SetDirty(dotTransform.gameObject);
                    }
                }
            }

            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(managers[i]);
        }
    }
}
