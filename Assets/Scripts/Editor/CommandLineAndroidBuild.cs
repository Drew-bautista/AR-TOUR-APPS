using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Generates the starter scenes, switches to Android, and builds an APK.
/// Example:
/// Unity.exe -batchmode -quit -projectPath "<path>" -executeMethod CommandLineAndroidBuild.BuildAndroidApk -customBuildPath "Builds/Android/AguinaldoShrineARTour.apk"
/// </summary>
public static class CommandLineAndroidBuild
{
    private const string DefaultOutputPath = "Builds/Android/AguinaldoShrineARTour.apk";
    private const string RequiredAndroidNdkVersion = "27.2.12479018";
    private const int RequiredJavaMajorVersion = 17;

    [MenuItem("Tools/Aguinaldo Shrine AR Tour/Build Android APK")]
    public static void BuildAndroidApk()
    {
        ConfigureAndroidExternalTools();

        bool regenerateScenes = HasCommandLineFlag("-regenerateScenes");
        bool missingStarterScenes =
            !File.Exists("Assets/Scenes/HomeScene.unity") ||
            !File.Exists("Assets/Scenes/AguinaldoShrineARTour.unity");

        if (regenerateScenes || missingStarterScenes)
        {
            Debug.Log("Generating starter scenes before build.");
            AguinaldoShrineProjectBootstrapper.GenerateStarterScenes();
        }
        else
        {
            Debug.Log("Preserving the current HomeScene and AguinaldoShrineARTour scene changes for this build.");
        }

        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            throw new BuildFailedException("Failed to switch the active build target to Android.");
        }

        string outputPath = GetCommandLineArgument("-customBuildPath");
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = DefaultOutputPath;
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new BuildFailedException("No enabled scenes were found in Build Settings.");
        }

        string fullOutputPath = Path.GetFullPath(outputPath);
        string outputDirectory = Path.GetDirectoryName(fullOutputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        BuildOptions buildOptions = HasCommandLineFlag("-developmentBuild")
            ? BuildOptions.Development
            : BuildOptions.None;

        BuildPlayerOptions playerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = fullOutputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = buildOptions
        };

        BuildReport report = BuildPipeline.BuildPlayer(playerOptions);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException($"Android build failed with result: {report.summary.result}.");
        }

        Debug.Log($"Android APK created at: {fullOutputPath}");
    }

    private static string GetCommandLineArgument(string key)
    {
        string[] arguments = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i] == key)
            {
                return arguments[i + 1];
            }
        }

        return string.Empty;
    }

    private static bool HasCommandLineFlag(string key)
    {
        return System.Environment.GetCommandLineArgs().Contains(key);
    }

    private static void ConfigureAndroidExternalTools()
    {
        string sdkRoot = ResolveValidDirectory(
            IsValidAndroidSdkPath,
            GetCommandLineArgument("-androidSdkRoot"),
            GetEnvironmentVariable("ANDROID_SDK_ROOT", "ANDROID_HOME"),
            GetDefaultAndroidSdkPath());

        string ndkRoot = ResolveValidDirectory(
            IsValidAndroidNdkPath,
            GetCommandLineArgument("-androidNdkRoot"),
            GetEnvironmentVariable("ANDROID_NDK_ROOT", "ANDROID_NDK_HOME"),
            GetPreferredAndroidNdkPath(sdkRoot));

        string jdkRoot = ResolveValidDirectory(
            IsValidJdkPath,
            GetCommandLineArgument("-androidJdkRoot"),
            GetEnvironmentVariable("JAVA_HOME"),
            GetLatestMatchingDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft"), "jdk-*"));

        string unitySdkRoot = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "SDK");
        string unityNdkRoot = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "NDK");
        string unityJdkRoot = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "OpenJDK");

        EnsureAndroidDependencyAvailable("SDK", sdkRoot, unitySdkRoot, "-androidSdkRoot", IsValidAndroidSdkPath);
        EnsureAndroidDependencyAvailable("NDK", ndkRoot, unityNdkRoot, "-androidNdkRoot", IsValidAndroidNdkPath);
        EnsureAndroidDependencyAvailable("JDK", jdkRoot, unityJdkRoot, "-androidJdkRoot", IsValidJdkPath);

        string resolvedSdkRoot = IsValidAndroidSdkPath(sdkRoot) ? sdkRoot : unitySdkRoot;
        string resolvedNdkRoot = IsValidAndroidNdkPath(unityNdkRoot) ? unityNdkRoot : ndkRoot;
        string resolvedJdkRoot = IsValidJdkPath(unityJdkRoot) ? unityJdkRoot : jdkRoot;

        AndroidExternalToolsSettings.sdkRootPath = resolvedSdkRoot;
        AndroidExternalToolsSettings.ndkRootPath = resolvedNdkRoot;
        AndroidExternalToolsSettings.jdkRootPath = resolvedJdkRoot;

        Debug.Log(
            "Configured Android external tools:\n" +
            $"SDK: {resolvedSdkRoot}\n" +
            $"NDK: {resolvedNdkRoot}\n" +
            $"JDK: {resolvedJdkRoot}");
    }

    private static void EnsureAndroidDependencyAvailable(string dependencyName, string customPath, string unityPath, string commandLineArgument, Func<string, bool> validator)
    {
        if (validator(customPath) || validator(unityPath))
        {
            return;
        }

        throw new BuildFailedException(
            $"Android {dependencyName} was not found. " +
            $"Install the Unity-provided dependency or pass {commandLineArgument} <path>.");
    }

    private static string GetEnvironmentVariable(params string[] keys)
    {
        foreach (string key in keys)
        {
            string value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static string ResolveValidDirectory(Func<string, bool> validator, params string[] candidates)
    {
        foreach (string candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            string fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(candidate));
            if (validator(fullPath))
            {
                return fullPath;
            }
        }

        return string.Empty;
    }

    private static bool IsValidAndroidSdkPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        bool hasPlatformTools = File.Exists(Path.Combine(path, "platform-tools", "adb.exe"));
        bool hasPlatforms = Directory.Exists(Path.Combine(path, "platforms"));
        bool hasSdkManager = File.Exists(Path.Combine(path, "cmdline-tools", "latest", "bin", "sdkmanager.bat"));
        return (hasPlatformTools && hasPlatforms) || hasSdkManager;
    }

    private static bool IsValidAndroidNdkPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        bool hasNdkFiles =
            File.Exists(Path.Combine(path, "ndk-build.cmd")) ||
            File.Exists(Path.Combine(path, "source.properties"));

        if (!hasNdkFiles)
        {
            return false;
        }

        string version = GetAndroidNdkVersion(path);
        return string.Equals(version, RequiredAndroidNdkVersion, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidJdkPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        bool hasJavaTools =
            File.Exists(Path.Combine(path, "bin", "javac.exe")) ||
            File.Exists(Path.Combine(path, "bin", "java.exe"));

        if (!hasJavaTools)
        {
            return false;
        }

        return GetJdkMajorVersion(path) == RequiredJavaMajorVersion;
    }

    private static string GetDefaultAndroidSdkPath()
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, "Android", "Sdk");
    }

    private static string GetLatestSubdirectory(string parentPath)
    {
        if (string.IsNullOrWhiteSpace(parentPath) || !Directory.Exists(parentPath))
        {
            return string.Empty;
        }

        return Directory.GetDirectories(parentPath)
            .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault() ?? string.Empty;
    }

    private static string GetPreferredAndroidNdkPath(string sdkRoot)
    {
        if (string.IsNullOrWhiteSpace(sdkRoot))
        {
            return string.Empty;
        }

        string ndkParentPath = Path.Combine(sdkRoot, "ndk");
        if (!Directory.Exists(ndkParentPath))
        {
            return string.Empty;
        }

        string exactMatchPath = Path.Combine(ndkParentPath, RequiredAndroidNdkVersion);
        if (IsValidAndroidNdkPath(exactMatchPath))
        {
            return exactMatchPath;
        }

        return Directory.GetDirectories(ndkParentPath)
            .Where(IsValidAndroidNdkPath)
            .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault() ?? string.Empty;
    }

    private static string GetAndroidNdkVersion(string path)
    {
        string sourcePropertiesPath = Path.Combine(path, "source.properties");
        if (!File.Exists(sourcePropertiesPath))
        {
            return string.Empty;
        }

        foreach (string line in File.ReadLines(sourcePropertiesPath))
        {
            if (!line.StartsWith("Pkg.Revision", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string[] parts = line.Split(new[] { '=' }, 2);
            if (parts.Length == 2)
            {
                return parts[1].Trim();
            }
        }

        return string.Empty;
    }

    private static int GetJdkMajorVersion(string path)
    {
        string releaseFilePath = Path.Combine(path, "release");
        if (!File.Exists(releaseFilePath))
        {
            return 0;
        }

        foreach (string line in File.ReadLines(releaseFilePath))
        {
            if (!line.StartsWith("JAVA_VERSION=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string version = line.Split(new[] { '=' }, 2)[1].Trim().Trim('"');
            string majorSegment = version.Split('.')[0];
            if (int.TryParse(majorSegment, out int majorVersion))
            {
                return majorVersion;
            }
        }

        return 0;
    }

    private static string GetLatestMatchingDirectory(string parentPath, string pattern)
    {
        if (string.IsNullOrWhiteSpace(parentPath) || !Directory.Exists(parentPath))
        {
            return string.Empty;
        }

        return Directory.GetDirectories(parentPath, pattern)
            .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault() ?? string.Empty;
    }
}
