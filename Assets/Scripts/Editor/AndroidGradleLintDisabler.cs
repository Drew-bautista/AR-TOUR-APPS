using System.IO;
using UnityEditor.Android;
using UnityEngine;

public sealed class AndroidGradleLintDisabler : IPostGenerateGradleAndroidProject
{
    private const string Marker = "// Aguinaldo Shrine AR Tour: disable Android lint tasks for release APK builds.";

    public int callbackOrder => 999;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        PatchAndroidModuleBuildGradle(Path.Combine(path, "build.gradle"));

        DirectoryInfo rootDirectory = Directory.GetParent(path);
        if (rootDirectory == null)
        {
            return;
        }

        PatchAndroidModuleBuildGradle(Path.Combine(rootDirectory.FullName, "launcher", "build.gradle"));
    }

    private static void PatchAndroidModuleBuildGradle(string buildGradlePath)
    {
        if (!File.Exists(buildGradlePath))
        {
            Debug.LogWarning($"Unable to disable Android lint tasks. Missing Gradle file: {buildGradlePath}");
            return;
        }

        string buildGradle = File.ReadAllText(buildGradlePath);
        if (buildGradle.Contains(Marker))
        {
            return;
        }

        File.AppendAllText(buildGradlePath, @"

" + Marker + @"
android {
    lint {
        checkReleaseBuilds false
        abortOnError false
    }
}

tasks.configureEach { task ->
    if (task.name.toLowerCase().contains('lint')) {
        task.enabled = false
    }
}
");

        Debug.Log($"Android lint tasks disabled in generated Gradle file: {buildGradlePath}");
    }
}
