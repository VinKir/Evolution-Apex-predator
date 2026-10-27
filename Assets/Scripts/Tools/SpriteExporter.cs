#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class SpriteExporter
{
    [MenuItem("Tools/Export Selected Sprites")]
    public static void Export()
    {
        Object[] objects = Selection.objects;

        foreach (Object obj in objects)
        {
            if (obj is not Sprite sprite)
                continue;

            Texture2D source = sprite.texture;
            Rect rect = sprite.textureRect;

            Texture2D result = new Texture2D(
                (int)rect.width,
                (int)rect.height,
                TextureFormat.RGBA32,
                false
            );

            Color[] pixels = source.GetPixels(
                (int)rect.x,
                (int)rect.y,
                (int)rect.width,
                (int)rect.height
            );

            result.SetPixels(pixels);
            result.Apply();

            byte[] png = result.EncodeToPNG();

            string path = EditorUtility.SaveFilePanel(
                "Save Sprite",
                "Assets",
                sprite.name + ".png",
                "png"
            );

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllBytes(path, png);
                AssetDatabase.Refresh();
            }

            Object.DestroyImmediate(result);
        }
    }
}
#endif