using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Opens the user's gallery and returns a readable Texture2D for matching.
/// </summary>
public class GalleryPicker : MonoBehaviour
{
    [SerializeField] private int maxLoadedTextureSize = 1024;

    /// <summary>
    /// Opens the image picker and returns the chosen texture and source path.
    /// </summary>
    public void PickImage(Action<Texture2D, string> onImagePicked, Action<string> onFailed)
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Choose a shrine image", string.Empty, "png,jpg,jpeg");
        if (string.IsNullOrWhiteSpace(path))
        {
            onFailed?.Invoke("Image selection was cancelled.");
            return;
        }

        Texture2D texture = LoadEditorTexture(path);
        if (texture == null)
        {
            onFailed?.Invoke("The selected image could not be loaded.");
            return;
        }

        onImagePicked?.Invoke(texture, path);
#elif UNITY_ANDROID || UNITY_IOS
        if (NativeGallery.IsMediaPickerBusy())
        {
            onFailed?.Invoke("The gallery picker is already open.");
            return;
        }

        NativeGallery.GetImageFromGallery(path =>
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                onFailed?.Invoke("Image selection was cancelled.");
                return;
            }

            Texture2D texture = NativeGallery.LoadImageAtPath(path, maxLoadedTextureSize, false, false);
            if (texture == null)
            {
                onFailed?.Invoke("The selected image could not be loaded.");
                return;
            }

            onImagePicked?.Invoke(texture, path);
        }, "Select a shrine image", "image/*");
#else
        onFailed?.Invoke("Gallery picking is only supported on Android, iOS, or the Unity Editor.");
#endif
    }

#if UNITY_EDITOR
    private Texture2D LoadEditorTexture(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        byte[] imageBytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(imageBytes, false))
        {
            DestroyImmediate(texture);
            return null;
        }

        return texture;
    }
#endif
}
