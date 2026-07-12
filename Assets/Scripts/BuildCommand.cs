#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

// BuildCommand.PerformAndroidBuild
// Static method callable via Unity CLI: -executeMethod BuildCommand.PerformAndroidBuild
// Reads environment variables for configuration (see CI README)

public static class BuildCommand
{
    public static void PerformAndroidBuild()
    {
        try
        {
            Debug.Log("CI Build: Starting PerformAndroidBuild...");

            string outFolderRel = Environment.GetEnvironmentVariable("APK_OUTPUT_PATH") ?? "Builds/Android";
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputFolder = Path.IsPathRooted(outFolderRel) ? outFolderRel : Path.Combine(projectRoot, outFolderRel);
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            string apkName = Environment.GetEnvironmentVariable("APK_NAME") ?? "AguinaldoShrine_AR.apk";
            string fullPath = Path.Combine(outputFolder, apkName);

            // Scenes
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes == null || scenes.Length == 0)
            {
                Debug.LogError("No enabled scenes in Build Settings. Add Home and AR scenes to Build Settings.");
                EditorApplication.Exit(1);
                return;
            }

            // Keystore (optional) - provide base64 via KEYSTORE_BASE64
            string keystoreBase64 = Environment.GetEnvironmentVariable("KEYSTORE_BASE64");
            if (!string.IsNullOrEmpty(keystoreBase64))
            {
                try
                {
                    byte[] keystoreBytes = Convert.FromBase64String(keystoreBase64);
                    string keystorePath = Path.Combine(Path.GetTempPath(), "ci_keystore.jks");
                    File.WriteAllBytes(keystorePath, keystoreBytes);

                    PlayerSettings.Android.useCustomKeystore = true;
                    PlayerSettings.Android.keystoreName = keystorePath;
                    PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("KEYSTORE_PASS") ?? "";
                    PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("KEY_ALIAS") ?? "";
                    PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("KEY_ALIAS_PASS") ?? "";

                    Debug.Log($"Keystore written to: {keystorePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to decode/write keystore: " + e);
                }
            }

            // Bundle Identifier override (optional)
            string bundleId = Environment.GetEnvironmentVariable("BUNDLE_ID");
            if (!string.IsNullOrEmpty(bundleId))
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, bundleId);
                Debug.Log("Set bundle id: " + bundleId);
            }

            // Build options
            BuildOptions options = BuildOptions.None;
            string dev = Environment.GetEnvironmentVariable("DEVELOPMENT_BUILD") ?? "0";
            bool isDev = (dev == "1" || dev.ToLower().Contains("true"));
            if (isDev) options |= BuildOptions.Development;

            Debug.Log($"Building APK to: {fullPath} (Development: {isDev})");

            var report = BuildPipeline.BuildPlayer(scenes, fullPath, BuildTarget.Android, options);
            if (report == null)
            {
                Debug.LogError("BuildPipeline returned null report");
                EditorApplication.Exit(1);
            }

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded: " + fullPath);
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("Build failed: " + report.summary.result);
                EditorApplication.Exit(1);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }
}
#endif
