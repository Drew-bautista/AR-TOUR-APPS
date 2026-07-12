using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public static class VideoQuizSceneInstaller
{
    private const string TourScenePath = "Assets/Scenes/AguinaldoShrineARTour.unity";
    private const string VideoFolderPath = "Assets/video";
    private const string StreamingVideoFolderPath = "Assets/StreamingAssets/video";

    [MenuItem("Tools/Aguinaldo Shrine AR Tour/Install Video Quiz Clips")]
    public static void InstallVideoQuizClips()
    {
        AssetDatabase.Refresh();

        Scene activeScene = SceneManager.GetActiveScene();
        bool openedTourScene = !string.Equals(activeScene.path, TourScenePath, StringComparison.OrdinalIgnoreCase);
        if (openedTourScene)
        {
            EditorSceneManager.SaveOpenScenes();
            EditorSceneManager.OpenScene(TourScenePath, OpenSceneMode.Single);
        }

        ScanUIController scanUIController = UnityEngine.Object.FindFirstObjectByType<ScanUIController>();
        if (scanUIController == null)
        {
            throw new InvalidOperationException("ScanUIController was not found in AguinaldoShrineARTour scene.");
        }

        VideoClipBinding[] bindings = FindVideoClipBindings();
        SyncStreamingVideoFiles(bindings);
        scanUIController.SetVideoQuizBindingsForEditor(
            bindings.Select(binding => binding.ImageKey).ToArray(),
            bindings.Select(binding => binding.Clip).ToArray());
        EditorUtility.SetDirty(scanUIController);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Debug.Log($"Installed {bindings.Length} video quiz clip(s) from {VideoFolderPath} and synced {StreamingVideoFolderPath}: {string.Join(", ", bindings.Select(binding => binding.ImageKey))}");
    }

    private static VideoClipBinding[] FindVideoClipBindings()
    {
        if (!AssetDatabase.IsValidFolder(VideoFolderPath))
        {
            Debug.LogWarning($"Video folder not found: {VideoFolderPath}");
            return Array.Empty<VideoClipBinding>();
        }

        List<VideoClipBinding> clips = new List<VideoClipBinding>();
        for (int i = 0; i < ScanUIController.AllowedVideoQuizImageKeys.Length; i++)
        {
            string key = ScanUIController.AllowedVideoQuizImageKeys[i];
            string path = $"{VideoFolderPath}/{key}.mp4";
            VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"Video quiz clip missing or not imported: {path}");
                continue;
            }

            clips.Add(new VideoClipBinding(key, clip, path));
        }

        return clips.ToArray();
    }

    private static void SyncStreamingVideoFiles(VideoClipBinding[] bindings)
    {
        if (!Directory.Exists(StreamingVideoFolderPath))
        {
            Directory.CreateDirectory(StreamingVideoFolderPath);
        }

        HashSet<string> expectedStreamingFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < bindings.Length; i++)
        {
            VideoClipBinding binding = bindings[i];
            string destinationPath = $"{StreamingVideoFolderPath}/{binding.ImageKey}.mp4";
            expectedStreamingFiles.Add(destinationPath);
            File.Copy(binding.SourcePath, destinationPath, true);
            AssetDatabase.ImportAsset(destinationPath);
        }

        string[] existingVideoFiles = Directory.GetFiles(StreamingVideoFolderPath, "*.mp4", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < existingVideoFiles.Length; i++)
        {
            string assetPath = ToProjectAssetPath(existingVideoFiles[i]);
            if (!expectedStreamingFiles.Contains(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }
    }

    private static string ToProjectAssetPath(string path)
    {
        string assetPath = path.Replace('\\', '/');
        int assetsIndex = assetPath.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
        return assetsIndex >= 0 ? assetPath.Substring(assetsIndex) : assetPath;
    }

    private readonly struct VideoClipBinding
    {
        public readonly string ImageKey;
        public readonly VideoClip Clip;
        public readonly string SourcePath;

        public VideoClipBinding(string imageKey, VideoClip clip, string sourcePath)
        {
            ImageKey = imageKey;
            Clip = clip;
            SourcePath = sourcePath;
        }
    }
}
