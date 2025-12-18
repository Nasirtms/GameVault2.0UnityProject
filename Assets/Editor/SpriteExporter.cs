#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class SpriteExporter
{
    [MenuItem("Tools/Export Selected Sprites")]
    public static void ExportSelectedSprites()
    {
        foreach (var obj in Selection.objects)
        {
            if (obj is Sprite sprite)
            {
                Texture2D tex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
                Color[] pixels = sprite.texture.GetPixels(
                    (int)sprite.textureRect.x,
                    (int)sprite.textureRect.y,
                    (int)sprite.textureRect.width,
                    (int)sprite.textureRect.height);
                tex.SetPixels(pixels);
                tex.Apply();

                byte[] pngData = tex.EncodeToPNG();
                if (pngData != null)
                {
                    string path = Path.Combine(Application.dataPath, "ExportedSprites", sprite.name + ".png");
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllBytes(path, pngData);
                    Debug.Log("Exported: " + path);
                }

                Object.DestroyImmediate(tex);
            }
        }
    }
}
#endif
