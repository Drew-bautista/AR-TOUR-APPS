#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;

public class AndroidBuilderWindow : EditorWindow
{
    string outputFolder = "Builds";
    string apkName = "AguinaldoShrine_AR.apk";
    bool developmentBuild = true;
    bool buildAndRun = false;

    // Signing (optional)
    string keystorePath = "";
    string keystorePass = "";
    string keyAlias = "";
    string keyAliasPass = "";

    string bundleId = "";

    [MenuItem("Tools/Build/Android APK")]
    public static void ShowWindow()
    {
        GetWindow<AndroidBuilderWindow>("Android Builder");
    }

    void OnEnable()
    {
        bundleId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Aguinaldo Shrine — Android APK Builder", EditorStyles.boldLabel);
        outputFolder = EditorGUILayout.TextField("Output folder", outputFolder);
        apkName = EditorGUILayout.TextField("APK name", apkName);
        developmentBuild = EditorGUILayout.Toggle("Development Build", developmentBuild);
        buildAndRun = EditorGUILayout.Toggle("Build And Run (connected device)", buildAndRun);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Signing (optional for release)", EditorStyles.boldLabel);
        keystorePath = EditorGUILayout.TextField("Keystore path", keystorePath);
        keystorePass = EditorGUILayout.PasswordField("Keystore password", keystorePass);
        keyAlias = EditorGUILayout.TextField("Key alias", keyAlias);
        keyAliasPass = EditorGUILayout.PasswordField("Key alias password", keyAliasPass);

        EditorGUILayout.Space();
        bundleId = EditorGUILayout.TextField("Bundle Identifier", bundleId);

        EditorGUILayout.Space();
        if (GUILayout.Button("Build APK", GUILayout.Height(36)))
        {
            BuildApk();
        }

        EditorGUILayout.HelpBox("Make sure your Home + AR scenes are added (Build Settings).\nInstall Android modules via Unity Hub, enable XR/ARCore packages, and set camera permission for AR.", MessageType.Info);
    }

    void BuildApk()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("No scenes", "No enabled scenes in Build Settings. Add Home and AR scenes to Build Settings before building.", "OK");
            return;
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            bool ok = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            if (!ok) Debug.LogWarning("SwitchActiveBuildTarget returned false — ensure Android platform modules are installed.");
        }

        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);
        string fullPath = Path.Combine(outputFolder, apkName);

        if (!string.IsNullOrEmpty(bundleId))
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, bundleId);

        if (!string.IsNullOrEmpty(keystorePath))
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasName = keyAlias;
            PlayerSettings.Android.keyaliasPass = keyAliasPass;
        }
        else
        {
            PlayerSettings.Android.useCustomKeystore = false;
        }

        BuildOptions options = BuildOptions.None;
        if (developmentBuild) options |= BuildOptions.Development;
        if (buildAndRun) options |= BuildOptions.AutoRunPlayer;

        var report = BuildPipeline.BuildPlayer(scenes, fullPath, BuildTarget.Android, options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            EditorUtility.RevealInFinder(Path.GetFullPath(fullPath));
            EditorUtility.DisplayDialog("Build succeeded", "APK created at: " + fullPath, "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Build failed", "See Console for errors. BuildResult: " + report.summary.result, "OK");
        }
    }
}
#endif
