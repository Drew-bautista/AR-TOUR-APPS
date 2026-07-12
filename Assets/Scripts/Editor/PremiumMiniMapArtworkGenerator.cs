using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PremiumMiniMapArtworkGenerator
{
    public const string MapTexturePath = "Assets/Art/Generated/AguinaldoShrineMiniMap.png";

    private static readonly Vector2 RouteMapMin = new Vector2(-1f, -1f);
    private static readonly Vector2 RouteMapMax = new Vector2(7f, 12f);
    private const int TextureWidth = 1536;
    private const int TextureHeight = 480;
    private const int MapInsetPixels = 50;

    [MenuItem("Tools/Aguinaldo Shrine AR Tour/Regenerate Premium Mini Map Artwork")]
    public static void RegenerateFromMenu()
    {
        GenerateOrUpdateMapSprite();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static Sprite GenerateOrUpdateMapSprite()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(MapTexturePath) ?? "Assets/Art/Generated");

        Texture2D texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear
        };

        DrawBackground(texture);
        DrawOutdoorDetails(texture);
        DrawBuilding(texture);
        DrawRoute(texture);

        texture.Apply(false, false);
        File.WriteAllBytes(MapTexturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(MapTexturePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(MapTexturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(MapTexturePath);
    }

    private static void DrawBackground(Texture2D texture)
    {
        Color grassTop = new Color32(0xCF, 0xEA, 0xC8, 0xFF);
        Color grassBottom = new Color32(0xA9, 0xD2, 0x9F, 0xFF);
        for (int y = 0; y < texture.height; y++)
        {
            Color rowColor = Color.Lerp(grassBottom, grassTop, y / (float)(texture.height - 1));
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, rowColor);
            }
        }

        DrawLine(texture, new Vector2Int(0, 88), new Vector2Int(310, 36), new Color32(0xF0, 0xF3, 0xEC, 0xFF), 24);
        DrawLine(texture, new Vector2Int(0, 88), new Vector2Int(310, 36), new Color32(0xCF, 0xD8, 0xD2, 0xFF), 3);
        DrawLine(texture, new Vector2Int(1260, 20), new Vector2Int(1536, 180), new Color32(0xF0, 0xF3, 0xEC, 0xFF), 28);
        DrawLine(texture, new Vector2Int(1260, 20), new Vector2Int(1536, 180), new Color32(0xCF, 0xD8, 0xD2, 0xFF), 3);
        DrawLine(texture, new Vector2Int(1280, 460), new Vector2Int(1536, 340), new Color32(0xF0, 0xF3, 0xEC, 0xFF), 30);
        DrawLine(texture, new Vector2Int(1280, 460), new Vector2Int(1536, 340), new Color32(0xCF, 0xD8, 0xD2, 0xFF), 3);
    }

    private static void DrawOutdoorDetails(Texture2D texture)
    {
        Vector2Int[] treeCenters =
        {
            new Vector2Int(48, 390), new Vector2Int(72, 66), new Vector2Int(146, 32),
            new Vector2Int(1292, 48), new Vector2Int(1350, 82), new Vector2Int(1452, 124),
            new Vector2Int(1414, 382), new Vector2Int(1480, 336), new Vector2Int(1318, 430),
            new Vector2Int(1000, 34), new Vector2Int(1092, 54), new Vector2Int(1184, 426)
        };

        for (int i = 0; i < treeCenters.Length; i++)
        {
            DrawTree(texture, treeCenters[i], 17 + (i % 3) * 3);
        }

        DrawCircle(texture, new Vector2Int(1246, 414), 48, new Color32(0xD6, 0xE7, 0xD0, 0xFF));
        DrawCircle(texture, new Vector2Int(1246, 414), 34, new Color32(0xB7, 0xD7, 0xAD, 0xFF));
        DrawCircle(texture, new Vector2Int(1246, 414), 18, new Color32(0x8F, 0xC8, 0x82, 0xFF));
    }

    private static void DrawTree(Texture2D texture, Vector2Int center, int radius)
    {
        DrawLine(texture, center + new Vector2Int(0, -radius), center + new Vector2Int(0, radius / 2), new Color32(0x8D, 0x72, 0x4B, 0xFF), 2);
        DrawCircle(texture, center + new Vector2Int(-radius / 2, 4), radius, new Color32(0x7F, 0xBC, 0x72, 0xFF));
        DrawCircle(texture, center + new Vector2Int(radius / 2, 5), radius - 2, new Color32(0x5D, 0xA9, 0x58, 0xFF));
        DrawCircle(texture, center + new Vector2Int(0, radius / 3), radius, new Color32(0x91, 0xC8, 0x7E, 0xFF));
    }

    private static void DrawBuilding(Texture2D texture)
    {
        Color wall = new Color32(0x1E, 0x24, 0x2B, 0xFF);
        Color floor = new Color32(0xC9, 0x93, 0x5E, 0xFF);
        Color floorLight = new Color32(0xE0, 0xB8, 0x82, 0xFF);
        Color floorDark = new Color32(0xA9, 0x72, 0x4B, 0xFF);
        Color roomBlue = new Color32(0x5D, 0xA8, 0xD8, 0xFF);
        Color roomCream = new Color32(0xF3, 0xDA, 0xAF, 0xFF);
        Color roomRose = new Color32(0xD7, 0x9A, 0x81, 0xFF);
        Color table = new Color32(0x77, 0x5A, 0x44, 0xFF);
        Color furniture = new Color32(0x2F, 0x86, 0xC9, 0xFF);

        Vector2Int[] building =
        {
            new Vector2Int(96, 62),
            new Vector2Int(360, 30),
            new Vector2Int(1218, 62),
            new Vector2Int(1440, 190),
            new Vector2Int(1332, 398),
            new Vector2Int(456, 450),
            new Vector2Int(126, 342),
            new Vector2Int(46, 178)
        };

        Vector2Int[] buildingShadow =
        {
            new Vector2Int(106, 50),
            new Vector2Int(370, 18),
            new Vector2Int(1228, 50),
            new Vector2Int(1450, 178),
            new Vector2Int(1342, 386),
            new Vector2Int(466, 438),
            new Vector2Int(136, 330),
            new Vector2Int(56, 166)
        };

        FillPolygon(texture, buildingShadow, new Color(0f, 0f, 0f, 0.18f), true);
        FillPolygon(texture, building, floor, false);
        DrawPolyline(texture, ClosePolygon(building), wall, 7);

        DrawRoom(texture, 104, 92, 146, 92, roomCream, wall);
        DrawRoom(texture, 104, 206, 146, 104, roomRose, wall);
        DrawRoom(texture, 276, 72, 150, 124, floorLight, wall);
        DrawRoom(texture, 284, 222, 158, 122, roomCream, wall);
        DrawRoom(texture, 472, 82, 280, 310, floorDark, wall);
        DrawRoom(texture, 784, 104, 334, 82, floorLight, wall);
        DrawRoom(texture, 784, 216, 172, 132, roomRose, wall);
        DrawRoom(texture, 982, 214, 208, 138, roomCream, wall);
        DrawRoom(texture, 1148, 104, 178, 96, floorLight, wall);
        DrawRoom(texture, 1210, 248, 132, 94, roomBlue, wall);

        DrawLine(texture, new Vector2Int(434, 72), new Vector2Int(434, 348), wall, 4);
        DrawLine(texture, new Vector2Int(760, 86), new Vector2Int(760, 388), wall, 4);
        DrawLine(texture, new Vector2Int(452, 202), new Vector2Int(1140, 202), wall, 4);
        DrawLine(texture, new Vector2Int(262, 50), new Vector2Int(262, 352), wall, 4);

        DrawFurniture(texture, 134, 120, furniture, table);
        DrawFurniture(texture, 306, 104, furniture, table);
        DrawFurniture(texture, 326, 252, furniture, table);
        DrawDiningSet(texture, 572, 238, table);
        DrawDiningSet(texture, 646, 170, table);
        DrawFurniture(texture, 826, 132, furniture, table);
        DrawFurniture(texture, 826, 250, furniture, table);
        DrawFurniture(texture, 1028, 252, furniture, table);
        DrawFurniture(texture, 1188, 130, furniture, table);
        DrawFurniture(texture, 1240, 276, new Color32(0x1D, 0x76, 0xB8, 0xFF), table);

        DrawWindows(texture);
    }

    private static void DrawRoute(Texture2D texture)
    {
        List<Vector2Int> routePoints = new List<Vector2Int>
        {
            MapRoutePointToTexture(texture, 0f, 0f),
            MapRoutePointToTexture(texture, 1.3f, 1.4f),
            MapRoutePointToTexture(texture, 2.8f, 2.6f),
            MapRoutePointToTexture(texture, 4.2f, 3.8f),
            MapRoutePointToTexture(texture, 5.4f, 5.1f),
            MapRoutePointToTexture(texture, 5.5f, 6.8f),
            MapRoutePointToTexture(texture, 4.4f, 8.2f),
            MapRoutePointToTexture(texture, 2.8f, 9.5f),
            MapRoutePointToTexture(texture, 1.3f, 11.0f)
        };

        DrawPolyline(texture, routePoints, new Color32(0xFF, 0xFF, 0xFF, 0xFF), 17);
        DrawPolyline(texture, routePoints, new Color32(0x0A, 0x79, 0xD7, 0xFF), 10);
        DrawPolyline(texture, routePoints, new Color32(0x51, 0xC7, 0xFF, 0xFF), 4);

        for (int i = 0; i < routePoints.Count; i++)
        {
            Color marker = i == 0
                ? new Color32(0x20, 0xB8, 0xEB, 0xFF)
                : i < 3
                    ? new Color32(0x27, 0xAE, 0x60, 0xFF)
                    : new Color32(0xF2, 0x99, 0x4A, 0xFF);

            DrawCircle(texture, routePoints[i], i == 0 ? 22 : 17, new Color32(0xFF, 0xFF, 0xFF, 0xFF));
            DrawCircle(texture, routePoints[i], i == 0 ? 14 : 10, marker);
            DrawCircle(texture, routePoints[i], 4, new Color32(0xFF, 0xFF, 0xFF, 0xFF));
        }
    }

    private static void DrawRoom(Texture2D texture, int x, int y, int width, int height, Color fill, Color wall)
    {
        DrawRect(texture, x, y, width, height, fill);
        DrawLine(texture, new Vector2Int(x, y), new Vector2Int(x + width, y), wall, 3);
        DrawLine(texture, new Vector2Int(x + width, y), new Vector2Int(x + width, y + height), wall, 3);
        DrawLine(texture, new Vector2Int(x + width, y + height), new Vector2Int(x, y + height), wall, 3);
        DrawLine(texture, new Vector2Int(x, y + height), new Vector2Int(x, y), wall, 3);
    }

    private static void DrawFurniture(Texture2D texture, int x, int y, Color blue, Color wood)
    {
        DrawRoundedRect(texture, x, y, 54, 28, 7, blue);
        DrawRoundedRect(texture, x + 72, y + 4, 42, 24, 7, blue);
        DrawRoundedRect(texture, x + 24, y + 48, 58, 24, 7, blue);
        DrawCircle(texture, new Vector2Int(x + 112, y + 58), 18, wood);
        DrawCircle(texture, new Vector2Int(x + 112, y + 58), 8, new Color32(0xD9, 0xBD, 0x8F, 0xFF));
    }

    private static void DrawDiningSet(Texture2D texture, int x, int y, Color wood)
    {
        DrawRoundedRect(texture, x, y, 88, 48, 9, wood);
        for (int i = 0; i < 4; i++)
        {
            DrawCircle(texture, new Vector2Int(x - 13, y + 9 + (i * 11)), 7, new Color32(0xEA, 0xD1, 0xAA, 0xFF));
            DrawCircle(texture, new Vector2Int(x + 101, y + 9 + (i * 11)), 7, new Color32(0xEA, 0xD1, 0xAA, 0xFF));
        }
    }

    private static void DrawWindows(Texture2D texture)
    {
        Color window = new Color32(0x85, 0xDA, 0xFF, 0xFF);
        DrawLine(texture, new Vector2Int(138, 74), new Vector2Int(232, 62), window, 4);
        DrawLine(texture, new Vector2Int(308, 54), new Vector2Int(392, 48), window, 4);
        DrawLine(texture, new Vector2Int(812, 78), new Vector2Int(1030, 86), window, 4);
        DrawLine(texture, new Vector2Int(1212, 86), new Vector2Int(1308, 142), window, 4);
        DrawLine(texture, new Vector2Int(126, 328), new Vector2Int(316, 390), window, 4);
        DrawLine(texture, new Vector2Int(720, 422), new Vector2Int(1048, 404), window, 4);
    }

    private static void DrawRoundedRect(Texture2D texture, int x, int y, int width, int height, int radius, Color color)
    {
        int minX = Mathf.Clamp(x, 0, texture.width - 1);
        int minY = Mathf.Clamp(y, 0, texture.height - 1);
        int maxX = Mathf.Clamp(x + width, 0, texture.width);
        int maxY = Mathf.Clamp(y + height, 0, texture.height);

        for (int px = minX; px < maxX; px++)
        {
            for (int py = minY; py < maxY; py++)
            {
                int closestX = Mathf.Clamp(px, x + radius, x + width - radius);
                int closestY = Mathf.Clamp(py, y + radius, y + height - radius);
                int dx = px - closestX;
                int dy = py - closestY;
                if ((dx * dx) + (dy * dy) <= radius * radius)
                {
                    SetPixel(texture, px, py, color, false);
                }
            }
        }
    }

    private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        int minX = Mathf.Clamp(x, 0, texture.width - 1);
        int minY = Mathf.Clamp(y, 0, texture.height - 1);
        int maxX = Mathf.Clamp(x + width, 0, texture.width);
        int maxY = Mathf.Clamp(y + height, 0, texture.height);

        for (int px = minX; px < maxX; px++)
        {
            for (int py = minY; py < maxY; py++)
            {
                SetPixel(texture, px, py, color, color.a < 0.99f);
            }
        }
    }

    private static void DrawPolyline(Texture2D texture, IReadOnlyList<Vector2Int> points, Color color, int thickness)
    {
        for (int i = 0; i < points.Count - 1; i++)
        {
            DrawLine(texture, points[i], points[i + 1], color, thickness);
        }
    }

    private static void DrawLine(Texture2D texture, Vector2Int start, Vector2Int end, Color color, int thickness)
    {
        int radius = Mathf.Max(1, thickness / 2);
        int steps = Mathf.Max(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y));
        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0f : i / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t));
            DrawCircle(texture, new Vector2Int(x, y), radius, color);
        }
    }

    private static void DrawCircle(Texture2D texture, Vector2Int center, int radius, Color color)
    {
        int radiusSquared = radius * radius;
        int minX = Mathf.Max(0, center.x - radius);
        int maxX = Mathf.Min(texture.width - 1, center.x + radius);
        int minY = Mathf.Max(0, center.y - radius);
        int maxY = Mathf.Min(texture.height - 1, center.y + radius);

        for (int x = minX; x <= maxX; x++)
        {
            int dx = x - center.x;
            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - center.y;
                if ((dx * dx) + (dy * dy) <= radiusSquared)
                {
                    SetPixel(texture, x, y, color, color.a < 0.99f);
                }
            }
        }
    }

    private static void FillPolygon(Texture2D texture, IReadOnlyList<Vector2Int> points, Color color, bool blend)
    {
        int minX = texture.width - 1;
        int maxX = 0;
        int minY = texture.height - 1;
        int maxY = 0;
        for (int i = 0; i < points.Count; i++)
        {
            minX = Mathf.Min(minX, points[i].x);
            maxX = Mathf.Max(maxX, points[i].x);
            minY = Mathf.Min(minY, points[i].y);
            maxY = Mathf.Max(maxY, points[i].y);
        }

        minX = Mathf.Clamp(minX, 0, texture.width - 1);
        maxX = Mathf.Clamp(maxX, 0, texture.width - 1);
        minY = Mathf.Clamp(minY, 0, texture.height - 1);
        maxY = Mathf.Clamp(maxY, 0, texture.height - 1);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (PointInPolygon(new Vector2(x + 0.5f, y + 0.5f), points))
                {
                    SetPixel(texture, x, y, color, blend);
                }
            }
        }
    }

    private static bool PointInPolygon(Vector2 point, IReadOnlyList<Vector2Int> polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];
            if (((pi.y > point.y) != (pj.y > point.y)) &&
                (point.x < ((pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y)) + pi.x))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static List<Vector2Int> ClosePolygon(IReadOnlyList<Vector2Int> points)
    {
        List<Vector2Int> closed = new List<Vector2Int>(points);
        if (points.Count > 0)
        {
            closed.Add(points[0]);
        }

        return closed;
    }

    private static Vector2Int MapRoutePointToTexture(Texture2D texture, float localX, float localZ)
    {
        float normalizedX = Mathf.InverseLerp(RouteMapMin.x, RouteMapMax.x, localX);
        float normalizedY = Mathf.InverseLerp(RouteMapMin.y, RouteMapMax.y, localZ);
        return new Vector2Int(
            Mathf.RoundToInt(Mathf.Lerp(MapInsetPixels, texture.width - MapInsetPixels, normalizedX)),
            Mathf.RoundToInt(Mathf.Lerp(MapInsetPixels, texture.height - MapInsetPixels, normalizedY)));
    }

    private static void SetPixel(Texture2D texture, int x, int y, Color color, bool blend)
    {
        if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
        {
            return;
        }

        if (!blend || color.a >= 0.99f)
        {
            texture.SetPixel(x, y, color);
            return;
        }

        Color existing = texture.GetPixel(x, y);
        float inverseAlpha = 1f - color.a;
        Color blended = new Color(
            (color.r * color.a) + (existing.r * inverseAlpha),
            (color.g * color.a) + (existing.g * inverseAlpha),
            (color.b * color.a) + (existing.b * inverseAlpha),
            1f);
        texture.SetPixel(x, y, blended);
    }
}
