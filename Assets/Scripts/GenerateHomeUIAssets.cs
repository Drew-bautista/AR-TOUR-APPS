using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Editor-capable utility: Generate placeholder UI assets (gradient, radial glow, simple icons)
// Output: Assets/Art/UI/
// Usage (in Unity Editor): Tools -> Generate Home UI Assets

public static class GenerateHomeUIAssets
{
#if UNITY_EDITOR
    [MenuItem("Tools/Generate Home UI Assets")]
    public static void GenerateAssetsMenu()
    {
        GenerateAssets();
    }

    public static void GenerateAssets()
    {
        string folder = "Assets/Art/UI";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        GenerateGradient(PathCombine(folder, "gradient_bg.png"));
        GenerateRadialGlow(PathCombine(folder, "glow_radial.png"));
        GenerateIconArrow(PathCombine(folder, "icon_arrow.png"));
        GenerateIconCamera(PathCombine(folder, "icon_camera.png"));
        GenerateIconMap(PathCombine(folder, "icon_map.png"));

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Generate Home UI Assets", "Placeholder assets created in Assets/Art/UI.\n\nThen assign them to HomeScreenUIController or use Tools->Wire Home UI Assets.", "OK");
    }

    static string PathCombine(string a, string b) { return a + "/" + b; }

    static void SavePNG(Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        string assetPath = path.Replace("\\", "/");
        AssetDatabase.ImportAsset(assetPath);
        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = FilterMode.Bilinear;
            ti.SaveAndReimport();
        }
    }

    static void GenerateGradient(string path)
    {
        int w = 1080; int h = 2340;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color top = HexToColor("071630"); // deep navy
        Color bottom = HexToColor("000000");
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            Color c = Color.Lerp(bottom, top, t);
            for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        SavePNG(tex, path);
    }

    static void GenerateRadialGlow(string path)
    {
        int size = 1024;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Clear(tex);
        Color accent = HexToColor("3B82F6");
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / maxR;
                float a = Mathf.Clamp01(1f - Mathf.Pow(d, 1.8f));
                Color c = accent; c.a = a * 0.95f;
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        SavePNG(tex, path);
    }

    static void GenerateIconArrow(string path)
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Clear(tex);
        Color white = Color.white;
        int shaftW = Mathf.Max(4, (int)(size * 0.06f));
        int shaftH = (int)(size * 0.38f);
        int centerX = size / 2;
        int shaftY = (int)(size * 0.22f);
        FillRect(tex, centerX - shaftW / 2, shaftY, shaftW, shaftH, white);
        Vector2 v0 = new Vector2(centerX, size - (int)(size * 0.10f));
        Vector2 v1 = new Vector2(centerX - (int)(size * 0.18f), shaftY + shaftH);
        Vector2 v2 = new Vector2(centerX + (int)(size * 0.18f), shaftY + shaftH);
        FillTriangle(tex, v0, v1, v2, white);
        tex.Apply();
        SavePNG(tex, path);
    }

    static void GenerateIconCamera(string path)
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Clear(tex);
        Color white = Color.white;
        int bodyW = (int)(size * 0.64f);
        int bodyH = (int)(size * 0.46f);
        int x = (size - bodyW) / 2;
        int y = (size - bodyH) / 2;
        FillRect(tex, x, y, bodyW, bodyH, white);
        int lensR = (int)(size * 0.16f);
        int cx = size / 2; int cy = size / 2;
        FillCircle(tex, cx, cy, lensR, new Color(0, 0, 0, 1f));
        FillCircleOutline(tex, cx, cy, lensR + 8, 6, white);
        FillRect(tex, x + (int)(bodyW * 0.06f), y + bodyH - (int)(size * 0.06f), (int)(size * 0.22f), (int)(size * 0.06f), white);
        tex.Apply();
        SavePNG(tex, path);
    }

    static void GenerateIconMap(string path)
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Clear(tex);
        Color white = Color.white;
        int margin = (int)(size * 0.10f);
        DrawThickLine(tex, new Vector2(margin, size - margin), new Vector2(size * 0.45f, margin), white, 8);
        DrawThickLine(tex, new Vector2(size * 0.45f, size - margin), new Vector2(size - margin, margin), white, 8);
        DrawThickLine(tex, new Vector2(margin, size - margin - (int)(size * 0.08f)), new Vector2(size - margin, margin + (int)(size * 0.08f)), white, 6);
        tex.Apply();
        SavePNG(tex, path);
    }

    static void Clear(Texture2D tex)
    {
        Color clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < tex.width; x++) tex.SetPixel(x, y, clear);
    }

    static void FillRect(Texture2D tex, int x, int y, int w, int h, Color col)
    {
        int x0 = Mathf.Clamp(x, 0, tex.width - 1);
        int y0 = Mathf.Clamp(y, 0, tex.height - 1);
        int x1 = Mathf.Clamp(x + w, 0, tex.width);
        int y1 = Mathf.Clamp(y + h, 0, tex.height);
        for (int yy = y0; yy < y1; yy++)
            for (int xx = x0; xx < x1; xx++) tex.SetPixel(xx, yy, col);
    }

    static void FillCircle(Texture2D tex, int cx, int cy, int r, Color col)
    {
        int x0 = Mathf.Clamp(cx - r, 0, tex.width - 1);
        int x1 = Mathf.Clamp(cx + r, 0, tex.width - 1);
        int y0 = Mathf.Clamp(cy - r, 0, tex.height - 1);
        int y1 = Mathf.Clamp(cy + r, 0, tex.height - 1);
        int rsq = r * r;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int dx = x - cx; int dy = y - cy;
                if (dx * dx + dy * dy <= rsq) tex.SetPixel(x, y, col);
            }
    }

    static void FillCircleOutline(Texture2D tex, int cx, int cy, int r, int thickness, Color col)
    {
        int rOut = r; int rIn = Mathf.Max(0, r - thickness);
        int rOutSq = rOut * rOut; int rInSq = rIn * rIn;
        int x0 = Mathf.Clamp(cx - rOut, 0, tex.width - 1);
        int x1 = Mathf.Clamp(cx + rOut, 0, tex.width - 1);
        int y0 = Mathf.Clamp(cy - rOut, 0, tex.height - 1);
        int y1 = Mathf.Clamp(cy + rOut, 0, tex.height - 1);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int dx = x - cx; int dy = y - cy;
                int d2 = dx * dx + dy * dy;
                if (d2 <= rOutSq && d2 >= rInSq) tex.SetPixel(x, y, col);
            }
    }

    static void FillTriangle(Texture2D tex, Vector2 v0, Vector2 v1, Vector2 v2, Color col)
    {
        int minX = Mathf.Clamp((int)Mathf.Min(v0.x, Mathf.Min(v1.x, v2.x)), 0, tex.width - 1);
        int maxX = Mathf.Clamp((int)Mathf.Max(v0.x, Mathf.Max(v1.x, v2.x)), 0, tex.width - 1);
        int minY = Mathf.Clamp((int)Mathf.Min(v0.y, Mathf.Min(v1.y, v2.y)), 0, tex.height - 1);
        int maxY = Mathf.Clamp((int)Mathf.Max(v0.y, Mathf.Max(v1.y, v2.y)), 0, tex.height - 1);
        float denom = (v1.y - v2.y) * (v0.x - v2.x) + (v2.x - v1.x) * (v0.y - v2.y);
        if (Mathf.Abs(denom) < 1e-6f) return;
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float a = ((v1.y - v2.y) * (p.x - v2.x) + (v2.x - v1.x) * (p.y - v2.y)) / denom;
                float b = ((v2.y - v0.y) * (p.x - v2.x) + (v0.x - v2.x) * (p.y - v2.y)) / denom;
                float c = 1f - a - b;
                if (a >= 0f && b >= 0f && c >= 0f) tex.SetPixel(x, y, col);
            }
    }

    static void DrawThickLine(Texture2D tex, Vector2 p0, Vector2 p1, Color col, int thickness)
    {
        int minX = Mathf.Clamp((int)Mathf.Min(p0.x, p1.x) - thickness, 0, tex.width - 1);
        int maxX = Mathf.Clamp((int)Mathf.Max(p0.x, p1.x) + thickness, 0, tex.width - 1);
        int minY = Mathf.Clamp((int)Mathf.Min(p0.y, p1.y) - thickness, 0, tex.height - 1);
        int maxY = Mathf.Clamp((int)Mathf.Max(p0.y, p1.y) + thickness, 0, tex.height - 1);
        float dx = p1.x - p0.x; float dy = p1.y - p0.y; float len2 = dx * dx + dy * dy;
        if (len2 < 1e-6f) return;
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float t = ((x - p0.x) * dx + (y - p0.y) * dy) / len2;
                t = Mathf.Clamp01(t);
                float projX = p0.x + t * dx; float projY = p0.y + t * dy;
                float dist2 = (projX - x) * (projX - x) + (projY - y) * (projY - y);
                if (dist2 <= (thickness * thickness * 0.25f)) tex.SetPixel(x, y, col);
            }
    }

    static Color HexToColor(string hex)
    {
        if (hex.StartsWith("#")) hex = hex.Substring(1);
        if (hex.Length != 6) return Color.white;
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color32(r, g, b, 255);
    }
#endif
}
